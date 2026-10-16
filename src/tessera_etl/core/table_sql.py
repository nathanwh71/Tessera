"""Writes the SQL that makes and fills one table from its cleaned rows.

The SQL takes the form GDAL's PGDump driver wrote before: drop the table, make it with a `fid`
***key*** and a `geom` column in ***SRID*** 4326 (the code of longitude and latitude), one
INSERT for each row in order, a ***spatial index***, and fresh statistics.

It keeps GDAL's choices where the map service depends on them, its typing of dates, times and
date-times included, and departs from them in two places on purpose, as the C# tool did. A
number with a fraction keeps all its digits, where GDAL kept fifteen. A column that mixes text
with numbers or true and false becomes text, where GDAL made it JSON, which the tile query
leaves out of every tile.
"""

import re
from collections import Counter
from collections.abc import Iterable, Sequence

from shapely.geometry import LineString, MultiLineString, MultiPoint, MultiPolygon, Point, Polygon
from shapely.geometry.base import BaseGeometry

from ..kernel.result import Err, Ok, Result
from . import dotnet, geojson
from .plan import TablePlan
from .rows import Row, Value
from .seed_script import schema_and_name
from .shapes import ewkb_hex

# PostgreSQL keeps a name to 63 bytes, and cuts a longer one; GDAL cuts it first, and so does this.
_NAME_BYTES = 63


def statements(table: TablePlan, rows: Sequence[Row], source_columns: Sequence[str]) -> Result[tuple[str, ...], str]:
    """The table's statements, one after another, ready for the script; or why there are none.

    There are none for no rows, or for two columns whose names become one.
    """
    # A table with no rows is refused.
    if not rows:
        return Err(f"{table.table}: no rows to write.")

    schema, name = schema_and_name(table)
    columns = geojson.columns(rows, source_columns)
    names = [launder(column) for column in columns]
    # Two columns that become one name, such as 'Name' and 'name', or a column named as the key
    # or the shape, would make a script the database refuses; the run stops here instead.
    counts = Counter([*names, "fid", "geom"])
    clash = next((column for column in dict.fromkeys([*names, "fid", "geom"]) if counts[column] > 1), None)
    if clash is not None:
        return Err(f'{table.table}: two columns become "{clash}"; rename one in the source.')

    types = [type_of(row.value(column) for row in rows) for column in columns]
    shapes = [_kept_shape(row.shape, table.lines) for row in rows]
    kinds = list(dict.fromkeys(shape.geom_type.upper() for shape in shapes if shape is not None))
    quoted = f"{_quote(schema)}.{_quote(name)}"

    out: list[str] = [
        "SET standard_conforming_strings = ON;",
        f"DROP TABLE IF EXISTS {quoted} CASCADE;",
        f"CREATE TABLE {quoted}();",
        # The key numbers the rows 1, 2, 3 in the order they are inserted, which is the
        # source's, and fixes the order of features in a tile.
        f'ALTER TABLE {quoted} ADD COLUMN "fid" SERIAL CONSTRAINT {_quote(f"{name}_pk")} PRIMARY KEY;',
        # One kind of shape gives a column of that kind; a mix, or none, gives GEOMETRY.
        f"SELECT AddGeometryColumn({_text(schema)},{_text(name)},'geom',4326,'{kinds[0] if len(kinds) == 1 else 'GEOMETRY'}',2);",
    ]
    out.extend(f"ALTER TABLE {quoted} ADD COLUMN {_quote(column)} {kind};" for column, kind in zip(names, types, strict=True))

    listed = ", ".join(_quote(column) for column in names)
    for row, shape in zip(rows, shapes, strict=True):
        values = ", ".join(literal(row.value(column), kind) for column, kind in zip(columns, types, strict=True))
        # A row with no shape leaves the column out, so it holds NULL, as GDAL wrote it.
        if shape is not None:
            out.append(f"INSERT INTO {quoted} (\"geom\", {listed}) VALUES ('{_ewkb(shape)}', {values});")
        else:
            out.append(f"INSERT INTO {quoted} ({listed}) VALUES ({values});")

    # The spatial index lets the tile query read only the rows inside one tile.
    out.append(f'CREATE INDEX {_quote(f"{name}_geom_geom_idx")} ON {quoted} USING GIST ("geom");')
    # Fresh statistics let PostgreSQL plan the tile query well from the first request.
    out.append(f"ANALYZE {quoted};")
    return Ok(tuple(out))


def launder(column: str) -> str:
    """A column's name as GDAL wrote it, which the service and its tests already know.

    Letters A to Z are made lower case, and each ' - # made _, then the name is cut to 63
    bytes. Other characters, such as 'é', stay as they are.
    """
    name = "".join("_" if letter in "'-#" else letter.lower() if ord(letter) <= 127 else letter for letter in column)
    # Cut at a whole character, so no letter is split in two.
    while dotnet.utf8_length(name) > _NAME_BYTES:
        name = name[:-1]
    return name


def type_of(values: Iterable[Value]) -> str:
    """The column's type, from the values its rows hold, as GDAL chose it.

    Whole numbers become INTEGER, or INT8 past 32 bits; any fraction makes FLOAT8; true and
    false become BOOLEAN, or a number beside other numbers; text that is all dates, times or
    date-times takes that type (see `time_type_of`); anything mixed with text, and a column of
    nulls, becomes text.
    """
    held = [value for value in values if value is not None]
    texts = [value for value in held if isinstance(value, str)]
    if held and len(texts) == len(held):
        time = time_type_of(texts)
        if time is not None:
            return time
    if texts or not held:
        return "VARCHAR"
    flags = [value for value in held if isinstance(value, bool)]
    wholes = [value for value in held if isinstance(value, int) and not isinstance(value, bool)]
    if any(isinstance(value, float) for value in held):
        return "FLOAT8"
    if wholes:
        return "INTEGER" if all(-(2**31) <= number <= 2**31 - 1 for number in wholes) else "INT8"
    return "BOOLEAN" if flags else "VARCHAR"


# A date as GDAL reads one: four digits, two and two, joined by - or /, such as 2024-05-01.
_DAY = r"(?P<year>[0-9]{4})[-/](?P<month>[0-9]{2})[-/](?P<day>[0-9]{2})"

# A time of day: hours, minutes and seconds, with a fraction of a second if given.
_CLOCK = r"(?P<hour>[0-9]{2}):(?P<minute>[0-9]{2}):(?P<second>[0-9]{2})(\.[0-9]+)?"

_DATE_ONLY = re.compile(f"^{_DAY}$")
_TIME_ONLY = re.compile(f"^{_CLOCK}$")

# A date and a time joined by T or a space, with an optional zone: Z, or an offset such as +08:00.
_DATE_AND_TIME = re.compile(f"^{_DAY}[T ]{_CLOCK}(Z|[+-][0-9]{{2}}(:?[0-9]{{2}})?)?$")


def time_type_of(values: Iterable[str]) -> str | None:
    """The type for a column of text when every value is a date, a time or a date-time.

    It is `date`, `time`, or `timestamp with time zone`, which also takes dates mixed with
    date-times, as GDAL typed the column when it read the GeoJSON; None when any value is
    other text, or a date that cannot be, such as 2024-13-45. The values go into the script as
    written, and PostgreSQL reads them as the type says.
    """
    kinds = list(dict.fromkeys(_kind_of(value) for value in values))
    if kinds == ["date"]:
        return "date"
    if kinds == ["time"]:
        return "time"
    if kinds and all(kind in ("date", "timestamp") for kind in kinds):
        return "timestamp with time zone"
    return None


def _kind_of(value: str) -> str:
    """One value's kind: date, time, timestamp, or text for anything else."""
    for pattern, kind in ((_DATE_ONLY, "date"), (_TIME_ONLY, "time"), (_DATE_AND_TIME, "timestamp")):
        found = pattern.match(value)
        if found is not None and _fits(found):
            return kind
    return "text"


def _fits(found: re.Match[str]) -> bool:
    """True when each part found lies within its range, as GDAL checks.

    Month 1 to 12, day 1 to 31, hour 0 to 23, minute 0 to 59, second 0 to 60, which allows a
    ***leap second***, the extra second clocks sometimes add.
    """
    limits = {"month": (1, 12), "day": (1, 31), "hour": (0, 23), "minute": (0, 59), "second": (0, 60)}
    parts = found.groupdict()
    return all(parts.get(part) is None or least <= int(parts[part] or "0") <= most for part, (least, most) in limits.items())


def literal(value: Value, kind: str) -> str:
    """A value as SQL for a column of the given type.

    NULL for none; text in single quotes, each quote inside doubled; numbers as digits, a
    fraction with every digit it holds; true and false as 't' and 'f', or as 1 and 0 in a
    column of numbers.
    """
    match value:
        case None:
            return "NULL"
        case bool():
            if kind == "BOOLEAN":
                return "'t'" if value else "'f'"
            if kind != "VARCHAR":
                return "1" if value else "0"
            return "'true'" if value else "'false'"
        case int():
            return str(value) if kind != "VARCHAR" else _text(str(value))
        case float():
            return dotnet.number_text(value) if kind != "VARCHAR" else _text(dotnet.number_text(value))
        case str():
            return _text(value)


def _text(text: str) -> str:
    """Text in single quotes, each quote inside doubled.

    With standard_conforming_strings on, a backslash is an ordinary character, so nothing else
    needs escaping.
    """
    return "'" + text.replace("'", "''") + "'"


def _quote(name: str) -> str:
    """A name in double quotes, each double quote inside doubled, as the service quotes names."""
    return '"' + name.replace('"', '""') + '"'


def _kept_shape(shape: BaseGeometry | None, lines: bool) -> BaseGeometry | None:
    """The shape to write: none for an empty one, and one part made a set of one for lines.

    A table of lines or areas keeps one kind of shape in its column, so one part becomes a set
    of one, as GDAL's PROMOTE_TO_MULTI made it.
    """
    if shape is None or shape.is_empty:
        return None
    if not lines:
        return shape
    match shape:
        case Polygon():
            return MultiPolygon([shape])
        case LineString():
            return MultiLineString([shape])
        case Point():
            return MultiPoint([shape])
        case _:
            return shape


def _ewkb(shape: BaseGeometry) -> str:
    """The shape as PostGIS's ***extended well-known binary***, in hexadecimal.

    That is the form GDAL wrote, which PostGIS reads into a geometry column directly: least
    significant byte first, x and y alone, with SRID 4326 inside, since the extract put every
    shape in longitude and latitude.
    """
    return ewkb_hex(shape, 4326)
