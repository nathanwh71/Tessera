"""Features, the form every reader hands its places in, and the table the ***filters*** clean.

***GeoJSON*** is the text form of places that GDAL's ogr2ogr writes. The pipeline reads most
sources with pyogrio (shell/extract.py), and builds the same features ogr2ogr's GeoJSON would
hold, so this module turns either into a table the same way.
"""

import json
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import Any, cast

from geopandas import GeoDataFrame
from shapely.geometry.base import BaseGeometry

from ..kernel.result import Err, Ok, Result
from . import shapes
from .rows import Value, table_of


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
    """A table read from its source: its rows, numbered from 1 in the source's order, and the
    rows that lack each column.

    A GeoJSON feature may leave a property out, where a table of fields holds every column in
    every row. A column no kept row holds then stays out of the script, as GDAL's PGDump driver
    leaves it out; `absent` names, for each column some rows lack, the rows that lack it.
    """

    frame: GeoDataFrame
    absent: Mapping[str, frozenset[int]]


def rows_of(features: Sequence[Feature]) -> Extract:
    """The table of the features, with its columns in the order they first appear.

    GDAL writes a text field named id as the feature's own id, outside its properties; it
    comes back here as the id column, first, as it stood in the source.
    """
    cells = [{**({"id": feature.id.value} if feature.id is not None else {}), **dict(feature.properties)} for feature in features]
    order = dict.fromkeys(["id"] if any(feature.id is not None for feature in features) else [])
    order |= dict.fromkeys(name for feature in features for name, _ in feature.properties)
    columns = {name: [cell.get(name) for cell in cells] for name in order}
    lacking = {name: frozenset(number for number, cell in enumerate(cells, start=1) if name not in cell) for name in order}
    return Extract(
        table_of([feature.shape for feature in features], columns, range(1, len(features) + 1)),
        {name: rows for name, rows in lacking.items() if rows},
    )


def read(text: str) -> Result[Extract, str]:
    """The rows of GeoJSON text, or why it is not a FeatureCollection.

    GDAL may fail part-way and leave a file that is not one; the run then stops with its log
    written, rather than crashing.
    """
    try:
        document: object = json.loads(text, parse_int=_Whole, parse_float=_Fraction, object_pairs_hook=_Pairs)
    except ValueError as error:
        return Err(f"It is not JSON: {error}.")
    features = document.get("features", None) if isinstance(document, _Pairs) else None
    # An object is held as a list of pairs, so it is ruled out first.
    if isinstance(features, _Pairs) or not isinstance(features, list):
        return Err("It is not a FeatureCollection: it needs a list of features.")
    read_features: list[Feature] = []
    for item in cast(list[object], features):
        if not isinstance(item, _Pairs):
            return Err("It is not a FeatureCollection: each feature must be an object.")
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
    """A list or an object as GDAL's JSON library, json-c, writes it, such as `{ "a": 1 }`."""
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
        return shapes.from_geojson(json.dumps(plain))
    except (shapes.GEOSException, ValueError, TypeError):
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
