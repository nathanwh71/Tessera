"""Writes the SQL that makes and fills one table from its cleaned rows.

***SQL*** is the language a database such as PostgreSQL takes its orders in; ***PostGIS*** is
the PostgreSQL extension that stores and measures shapes.

The SQL takes the form of GDAL's PGDump driver, the part of GDAL that writes a PostgreSQL script
(GDAL has a driver for each format it reads or writes): drop the table, make it with a `fid`
***key*** and a `geom` column in ***SRID*** 4326 (the code of longitude and latitude), one INSERT
for each row in order, a ***spatial index***, and fresh statistics.

It keeps GDAL's choices where the map service depends on them, its typing of dates, times and
date-times included, and departs from them in two places on purpose. A number with a fraction
keeps all its digits, where GDAL keeps fifteen. A column that mixes text with numbers or true
and false becomes text, where GDAL makes it JSON, which the tile query leaves out of every tile.
"""

import math
import re
from collections import Counter
from collections.abc import Iterable, Mapping, Sequence

from geopandas import GeoDataFrame
from shapely.geometry import LineString, MultiLineString, MultiPoint, MultiPolygon, Point, Polygon
from shapely.geometry.base import BaseGeometry

from ..kernel.result import Err, Ok, Result
from .judge import number_text
from .plan import TablePlan
from .rows import Value, columns, numbers, shapes, values
from .seed_script import schema_and_name
from .shapes import ewkb_hex

NAME_BYTES = 63
"""PostgreSQL keeps a name to 63 bytes, and cuts a longer one; GDAL cuts it first, and so does this."""

_INT32 = range(-(2**31), 2**31)
"""The whole numbers PostgreSQL's INTEGER holds, in 32 bits; a column holding a larger one needs
INT8, also called BIGINT, which holds 64."""


def written(frame: GeoDataFrame, absent: Mapping[str, frozenset[int]]) -> list[str]:
    """The columns to write: each column of the table that some kept row holds.

    A filter may add a column or remove one, so the columns come from the cleaned table itself.
    A column every kept row lacks, as a GeoJSON feature may lack a property, is left out; a
    column of nulls stays, since each row still holds it.
    """
    kept = frozenset(numbers(frame))
    return [column for column in columns(frame) if not kept <= absent.get(column, frozenset())]


def statements(table: TablePlan, frame: GeoDataFrame, absent: Mapping[str, frozenset[int]]) -> Result[tuple[str, ...], str]:
    """The table's statements, one after another, ready for the script; or why there are none.

    There are none for no rows, or for two columns whose names become one.
    """
    if frame.empty:
        return Err(f"{table.table}: no rows to write.")
    schema, name = schema_and_name(table)
    kept = written(frame, absent)
    names = [launder(column) for column in kept]
    # Two columns that become one name, such as 'Name' and 'name', or a column named as the key
    # or the shape, would make a script the database refuses; the run stops here instead.
    counts = Counter([*names, "fid", "geom"])
    clash = next((column for column in counts if counts[column] > 1), None)
    if clash is not None:
        return Err(f'{table.table}: two columns become "{clash}"; rename one in the source.')

    cells = [values(frame, column) for column in kept]
    kinds = [type_of(column) for column in cells]
    found = [_kept_shape(shape, table.lines) for shape in shapes(frame)]
    # One kind of shape gives a column of that kind; a mix, or none, gives GEOMETRY.
    shared = {shape.geom_type.upper() for shape in found if shape is not None}
    geometry = next(iter(shared)) if len(shared) == 1 else "GEOMETRY"
    quoted = f"{_quote(schema)}.{_quote(name)}"
    return Ok(
        (
            "SET standard_conforming_strings = ON;",
            f"DROP TABLE IF EXISTS {quoted} CASCADE;",
            f"CREATE TABLE {quoted}();",
            # The key numbers the rows 1, 2, 3 in the order they are inserted, which is the
            # source's, and fixes the order of features in a tile.
            f'ALTER TABLE {quoted} ADD COLUMN "fid" SERIAL CONSTRAINT {_quote(f"{name}_pk")} PRIMARY KEY;',
            f"SELECT AddGeometryColumn({_text(schema)},{_text(name)},'geom',4326,'{geometry}',2);",
            *(f"ALTER TABLE {quoted} ADD COLUMN {_quote(column)} {kind};" for column, kind in zip(names, kinds, strict=True)),
            *_inserts(
                quoted,
                names,
                found,
                [[literal(value, kind) for value in column] for column, kind in zip(cells, kinds, strict=True)],
            ),
            # The spatial index lets the tile query read only the rows inside one tile.
            f'CREATE INDEX {_quote(f"{name}_geom_geom_idx")} ON {quoted} USING GIST ("geom");',
            # Fresh statistics let PostgreSQL plan the tile query well from the first request.
            f"ANALYZE {quoted};",
        )
    )


def _inserts(
    quoted: str, names: Sequence[str], found: Sequence[BaseGeometry | None], literals: Sequence[Sequence[str]]
) -> list[str]:
    """One INSERT for each row, in order, from each column's values already written as SQL.

    Each shape goes in as PostGIS's ***extended well-known binary***, in hexadecimal, the form
    GDAL writes, which PostGIS reads into a geometry column directly. A row with no shape leaves
    the column out, so it holds NULL.
    """
    listed = ", ".join(_quote(column) for column in names)
    rows = zip(*literals, strict=True) if literals else iter([()] * len(found))
    hexes = iter(ewkb_hex([shape for shape in found if shape is not None], 4326))
    return [
        f"INSERT INTO {quoted} (\"geom\", {listed}) VALUES ('{next(hexes)}', {', '.join(row)});"
        if shape is not None
        else f"INSERT INTO {quoted} ({listed}) VALUES ({', '.join(row)});"
        for shape, row in zip(found, rows, strict=True)
    ]


def launder(column: str) -> str:
    """A column's name as GDAL wrote it, which the service and its tests already know.

    Letters A to Z are made lower case, and each ' - # made _, then the name is cut to 63
    bytes. Other characters, such as 'é', stay as they are.
    """
    name = "".join("_" if letter in "'-#" else letter.lower() if ord(letter) <= 127 else letter for letter in column)
    # Cut at a whole character, so no letter is split in two.
    while len(name.encode("utf-8", "surrogatepass")) > NAME_BYTES:
        name = name[:-1]
    return name


def type_of(column: Sequence[Value]) -> str:
    """The column's type, from the values its rows hold, as GDAL chooses it.

    Text that is all dates, times or date-times takes that type (see `time_type_of`); any other
    text, text mixed with other values, and a column of nulls become VARCHAR; any fraction makes
    FLOAT8; whole numbers become INTEGER, or INT8 past 32 bits, with true and false beside them
    as numbers; and true and false alone become BOOLEAN.
    """
    held = [value for value in column if value is not None]
    if not held:
        return "VARCHAR"
    if all(isinstance(value, str) for value in held):
        return time_type_of(value for value in held if isinstance(value, str)) or "VARCHAR"
    if any(isinstance(value, str) for value in held):
        return "VARCHAR"
    if any(isinstance(value, float) for value in held):
        return "FLOAT8"
    wholes = [value for value in held if isinstance(value, int) and not isinstance(value, bool)]
    if wholes:
        return "INTEGER" if all(number in _INT32 for number in wholes) else "INT8"
    return "BOOLEAN"


# A date as GDAL reads one: four digits, two and two, joined by - or /, such as 2024-05-01.
_DAY = r"(?P<year>[0-9]{4})[-/](?P<month>[0-9]{2})[-/](?P<day>[0-9]{2})"

# A time of day: hours, minutes and seconds, with a fraction of a second if given.
_CLOCK = r"(?P<hour>[0-9]{2}):(?P<minute>[0-9]{2}):(?P<second>[0-9]{2})(\.[0-9]+)?"

_DATE_ONLY = re.compile(f"^{_DAY}$")
_TIME_ONLY = re.compile(f"^{_CLOCK}$")

# A date and a time joined by T or a space, with an optional zone: Z, or an offset such as +08:00.
_DATE_AND_TIME = re.compile(f"^{_DAY}[T ]{_CLOCK}(Z|[+-][0-9]{{2}}(:?[0-9]{{2}})?)?$")


def time_type_of(texts: Iterable[str]) -> str | None:
    """The type for a column of text when every value is a date, a time or a date-time.

    It is `date`, `time`, or `timestamp with time zone`, which also takes dates mixed with
    date-times, as GDAL types the column when it reads GeoJSON; None when any value is other
    text, or a date that cannot be, such as 2024-13-45. The values go into the script as
    written, and PostgreSQL reads them as the type says.
    """
    kinds: set[str] = set()
    for text in texts:
        kind = _kind_of(text)
        # One value of plain text makes the column text, so the rest need no look.
        if kind == "text":
            return None
        kinds.add(kind)
    if kinds == {"date"}:
        return "date"
    if kinds == {"time"}:
        return "time"
    return "timestamp with time zone" if kinds and kinds <= {"date", "timestamp"} else None


def _kind_of(value: str) -> str:
    """One value's kind: date, time, timestamp, or text for anything else."""
    # Every date and time starts with a digit, so other text needs no pattern.
    if not value[:1].isdigit():
        return "text"
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
            if kind == "VARCHAR":
                return _text(number_text(value))
            # PostgreSQL reads a number with no limit, such as inf, only as quoted text.
            return repr(value) if math.isfinite(value) else f"'{value}'"
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
