"""Features, the form every reader hands its places in, and the table the ***filters*** clean.

***GeoJSON*** is the text form of places that GDAL's ogr2ogr writes. The pipeline reads most
sources with pyogrio (shell/extract.py), and builds the same features ogr2ogr's GeoJSON would
hold, so this module turns either into a table the same way.
"""

import json
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import cast

from geopandas import GeoDataFrame
from shapely.geometry.base import BaseGeometry

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


class NumberText(str):
    """A JSON number kept as its text, as GDAL's JSON library keeps a number it read."""


def jsonc_text(value: object) -> str:
    """A list or an object as GDAL's JSON library, json-c, writes it, such as `{ "a": 1 }`.

    Text inside it is escaped as JSON escapes it, which json-c and Python's JSON writer do
    alike, with no escape for letters beyond ASCII.
    """
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
            return json.dumps(value, ensure_ascii=False)
        case Mapping():
            items = cast(Mapping[str, object], value)
            pairs = (f"{json.dumps(key, ensure_ascii=False)}: {jsonc_text(item)}" for key, item in items.items())
            return "{ " + ", ".join(pairs) + " }" if items else "{ }"
        case list():
            listed = cast(list[object], value)
            return "[ " + ", ".join(jsonc_text(item) for item in listed) + " ]" if listed else "[ ]"
        case _:
            return str(value)
