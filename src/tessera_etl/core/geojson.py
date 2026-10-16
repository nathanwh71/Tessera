"""Features, the form every reader hands its places in, and the rows the ***filters*** clean.

The C# tool had GDAL write each table as ***GeoJSON***, the text form of places, and read that
text back. The Python build reads the source directly (shell/extract.py), but builds the same
features GDAL's GeoJSON would have held, so this module turns either into rows the same way.
"""

import json
from collections.abc import Iterable, Mapping, Sequence
from dataclasses import dataclass
from typing import Any

import shapely
from shapely.geometry.base import BaseGeometry

from ..kernel.result import Err, Ok, Result
from . import dotnet, json_text
from .rows import Row, Value, frozen_values


@dataclass(frozen=True, slots=True)
class Some:
    """A value that is there, even when it is null; None stands for no value at all."""

    value: Value


@dataclass(frozen=True, slots=True)
class Feature:
    """One place as GDAL's GeoJSON held it.

    Its own `id`, which GDAL writes outside the properties, when it has one; its properties,
    in the order GDAL wrote them; and its shape, if any.
    """

    id: Some | None
    properties: tuple[tuple[str, Value], ...]
    shape: BaseGeometry | None


@dataclass(frozen=True, slots=True)
class Extract:
    """A table's rows, numbered from 1 in the source's order, and its columns in their order."""

    rows: tuple[Row, ...]
    columns: tuple[str, ...]


def rows_of(features: Iterable[Feature]) -> Extract:
    """The rows of a table, and its columns in the order they first appear.

    GDAL writes a text field named id as the feature's own id, outside its properties; it
    comes back here as the id column, first, as it stood in the source.
    """
    rows: list[Row] = []
    columns: list[str] = []
    seen: set[str] = set()
    for number, feature in enumerate(features, start=1):
        values: dict[str, Value] = {}
        if feature.id is not None:
            if "id" not in seen:
                columns.insert(0, "id")
                seen.add("id")
            values["id"] = feature.id.value
        for name, value in feature.properties:
            if name not in seen:
                columns.append(name)
                seen.add(name)
            values[name] = value
        rows.append(Row(number, feature.shape, frozen_values(values)))
    return Extract(tuple(rows), tuple(columns))


def columns(rows: Sequence[Row], source_columns: Sequence[str]) -> tuple[str, ...]:
    """The columns to write, taken from the kept rows themselves.

    So a filter that adds a column reaches the script, and one that removes a column leaves no
    empty one behind. The source's columns come first, in their order; new ones follow, by the
    first row that holds them, and by name within that row, since a row keeps no order of its
    own among its values. A column no kept row holds is left out; an empty source column stays,
    since each row still holds its key, with null.
    """
    held = {column for row in rows for column in row.values}
    source = set(source_columns)
    added = dict.fromkeys(
        column for row in rows for column in sorted(row.values, key=dotnet.ordinal_key) if column not in source
    )
    return (*(column for column in source_columns if column in held), *added)


def read(text: str) -> Result[Extract, str]:
    """The rows of GeoJSON text, or why it is not a FeatureCollection.

    GDAL may fail part-way and leave a file that is not one; the run then stops with its log
    written, rather than crashing.
    """
    try:
        document: object = json.loads(text, parse_int=_Whole, parse_float=_Fraction, object_pairs_hook=_Pairs)
    except ValueError:
        fault = json_text.parse(text)
        return Err(_reader_message(fault) if isinstance(fault, json_text.SyntaxFault) else "The text is not valid JSON.")
    if not isinstance(document, _Pairs):
        return Err(_wrong_kind("Object", document))
    features = document.get("features", _MISSING)
    if features is _MISSING:
        return Err("The given key was not present in the dictionary.")
    if not isinstance(features, list):
        return Err(_wrong_kind("Array", features))
    read_features: list[Feature] = []
    for item in features:
        if not isinstance(item, _Pairs):
            return Err(_wrong_kind("Object", item))
        geometry = item.get("geometry", _MISSING)
        shape = _shape(geometry) if isinstance(geometry, _Pairs) else None
        own = item.get("id", _MISSING)
        properties = item.get("properties", _MISSING)
        read_features.append(
            Feature(
                None if own is _MISSING else Some(_value(own)),
                tuple((key, _value(value)) for key, value in properties) if isinstance(properties, _Pairs) else (),
                shape,
            )
        )
    return Ok(rows_of(read_features))


class NumberText(str):
    """A JSON number kept as its text, as GDAL's JSON library keeps a number it read."""


class _Whole(NumberText):
    """A JSON number with no fraction, kept as its text."""


class _Fraction(NumberText):
    """A JSON number with a fraction or an exponent, kept as its text."""


class _Pairs(list[tuple[str, Any]]):
    """A JSON object as its keys and values in order, duplicates kept, as .NET reads one."""

    def get(self, key: str, default: object) -> Any:
        """The value of the last pair with the key, or `default`."""
        found: object = default
        for name, value in self:
            if name == key:
                found = value
        return found


_MISSING = object()
_INT64 = (-(2**63), 2**63 - 1)


def _value(value: object) -> Value:
    """A JSON value as the plain value a filter compares.

    A whole number that fits in 64 bits stays whole, as .NET's TryGetInt64 decides; any other
    number has a fraction. A list or an object stays as its JSON text, as GDAL wrote it.
    """
    match value:
        case None | bool():
            return value
        case _Whole():
            number = int(value)
            return number if _INT64[0] <= number <= _INT64[1] else float(value)
        case _Fraction():
            return float(value)
        case str():
            return str(value)
        case _:
            return jsonc_text(value)


def jsonc_text(value: object) -> str:
    """A list or an object as GDAL's JSON library, json-c, writes it: '{ "a": 1 }'."""
    match value:
        case None:
            return "null"
        case bool():
            return "true" if value else "false"
        case NumberText():
            return str(value)
        case int() | float():
            return repr(value)
        case str():
            return _jsonc_string(value)
        case _Pairs():
            if not value:
                return "{ }"
            return "{ " + ", ".join(f"{_jsonc_string(key)}: {jsonc_text(item)}" for key, item in value) + " }"
        case Mapping():
            pairs = _Pairs(value.items())  # pyright: ignore[reportUnknownArgumentType, reportUnknownMemberType]
            return jsonc_text(pairs)
        case list():
            items: list[object] = value  # pyright: ignore[reportUnknownVariableType]
            return "[ ]" if not items else "[ " + ", ".join(jsonc_text(item) for item in items) + " ]"
        case _:
            return str(value)


def _jsonc_string(text: str) -> str:
    out = ['"']
    for letter in text:
        if letter == '"':
            out.append('\\"')
        elif letter == "\\":
            out.append("\\\\")
        elif letter in "\b\f\n\r\t":
            out.append({"\b": "\\b", "\f": "\\f", "\n": "\\n", "\r": "\\r", "\t": "\\t"}[letter])
        elif ord(letter) < 32:
            out.append(f"\\u{ord(letter):04x}")
        else:
            out.append(letter)
    out.append('"')
    return "".join(out)


def _shape(geometry: "_Pairs") -> BaseGeometry | None:
    """A GeoJSON geometry as a shape, or None when it cannot be one."""
    plain = _plain(geometry)
    try:
        return shapely.from_geojson(json.dumps(plain))
    except (shapely.errors.GEOSException, ValueError, TypeError):
        return None


def _plain(value: object) -> object:
    match value:
        case _Whole() | _Fraction():
            return float(value)
        case _Pairs():
            return {key: _plain(item) for key, item in value}
        case list():
            items: list[object] = value  # pyright: ignore[reportUnknownVariableType]
            return [_plain(item) for item in items]
        case _:
            return value


def _wrong_kind(wanted: str, found: object) -> str:
    """.NET's words when a JSON value is not of the kind the reader wanted."""
    if isinstance(found, _Pairs):
        kind = "Object"
    elif isinstance(found, list):
        kind = "Array"
    elif isinstance(found, bool):
        kind = "True" if found else "False"
    elif isinstance(found, (_Whole, _Fraction)):
        kind = "Number"
    elif isinstance(found, str):
        kind = "String"
    else:
        kind = "Null"
    return f"The requested operation requires an element of type '{wanted}', but the target element has type '{kind}'."


def _reader_message(fault: json_text.SyntaxFault) -> str:
    """.NET's words for text that is not JSON, which name no path when reading a whole document."""
    return f"{fault.message} LineNumber: {fault.place.line} | BytePositionInLine: {fault.place.byte}."
