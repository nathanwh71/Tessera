"""The Extract edge: reads each table of the source with pyogrio, into a table of rows.

pyogrio is a Python library that reads a ***layer*** of a source, such as a feature class of a
***geodatabase***, through GDAL, straight into ***Arrow*** columns (a compact table format held
in memory). Each value then goes through the rounding GDAL's GeoJSON writer gives it
(core/gdal_text.py), so a table reads the same whichever way it is read.

Three cases need ogr2ogr, GDAL's own program, which writes the table into a GeoPackage that
pyogrio then reads:

- a table holding true curves, such as a geodatabase's arcs, since the straight lines GDAL makes
  of them come from its own drawing rules, which shapely does not share;
- a table in another coordinate system, since pyproj, the Python library that moves points
  between systems, gives other last digits than GDAL's own: on 6,000 points moved from Web
  Mercator, the flat projection most web maps draw on, 3,732 differed;
- a field whose domain is coded by text rather than whole numbers, since pyogrio hands over the
  words of whole-number codes alone.
"""

import json
import warnings
from collections.abc import Callable, Sequence
from dataclasses import dataclass
from pathlib import Path
from typing import Any, assert_never, cast

import numpy as np
import pyarrow as pa
import pyogrio  # pyright: ignore[reportMissingTypeStubs]
from numpy.typing import NDArray
from pyproj import CRS
from shapely.geometry.base import BaseGeometry

from ..core import gdal_text, geojson, seed_script, shapes
from ..core.geojson import Extract, Feature, Some
from ..core.plan import TablePlan
from ..core.rows import Value, table_of
from ..kernel.result import Err, Ok, Result
from . import gdal

REFUSALS = (RuntimeError, ValueError, OSError)
"""The errors a read that fails raises: pyogrio's own, such as DataLayerError for a missing
layer, are RuntimeErrors; pyogrio and Arrow, the format it reads into, raise a ValueError or an
OSError for a `where` GDAL cannot run, as soon as the query starts or as the rows arrive."""

_ABSENT = object()
"""Marks a value GDAL's writer leaves out of a feature, such as a number that is not finite."""


@dataclass(frozen=True, slots=True)
class Source:
    """Where each table is read from, and what the reads share.

    `path` is the source, or its GeoPackage copy for a GeoJSON source of several tables;
    `native` says GDAL's GeoJSON driver, its part for that format, reads the source itself,
    which keeps each feature's own id and leaves out a property the feature lacks;
    `json_columns` names the copy's columns that hold lists or JSON as text.
    """

    path: Path
    work: Path
    native: bool
    json_columns: frozenset[str]


@dataclass(frozen=True, slots=True)
class NeedsGdal:
    """The table holds what pyogrio cannot read as needed, so ogr2ogr reads it."""

    why: str


type Read = Ok[Extract] | Err[str] | NeedsGdal
"""What one read with pyogrio gives: the table, why it cannot be read, or a turn for ogr2ogr."""


def _info(path: Path, layer: str | None) -> dict[str, Any]:
    """pyogrio's account of a layer: its driver, and each field's GDAL type and subtype."""
    # pyogrio warns when it picks the first of several layers; read_table asks for a layer by
    # name whenever the choice matters, so the warning would only clutter the console.
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        info = pyogrio.read_info(path, layer=layer)  # pyright: ignore[reportUnknownMemberType, reportUnknownVariableType]
    return cast(dict[str, Any], info)


def _arrow(path: Path, **options: Any) -> tuple[dict[str, Any], pa.Table]:
    """pyogrio's read, typed for the rest of this module."""
    meta, table = pyogrio.read_arrow(path, **options)  # pyright: ignore[reportUnknownMemberType, reportUnknownVariableType]
    return cast(dict[str, Any], meta), cast(pa.Table, table)


def _layers(path: Path) -> list[str]:
    found = pyogrio.list_layers(path)  # pyright: ignore[reportUnknownMemberType, reportUnknownVariableType]
    return [str(name) for name, _ in cast(Sequence[Sequence[Any]], found)]


def open_source(source: Path, tables: int, work: Path) -> Result[Source, str]:
    """Gets a source ready to read, copying it into a GeoPackage when that saves time.

    The copy is made for a GeoJSON source of several tables (core/seed_script.py, copy_first):
    GeoJSON has no index, so each table's read would scan the whole file again.
    """
    if not seed_script.copy_first(str(source), tables):
        try:
            native = _info(source, None).get("driver") == "GeoJSON"
        except REFUSALS:
            # A source pyogrio cannot open fails at its first table, with pyogrio's words.
            native = False
        return Ok(Source(source, work, native, frozenset()))
    copy = work / "source.gpkg"
    try:
        json_columns = _copy(source, copy)
    except REFUSALS as error:
        return Err(f"{error}.")
    return Ok(Source(copy, work, False, json_columns))


def _copy(source: Path, copy: Path) -> frozenset[str]:
    """Copies a GeoJSON source into a GeoPackage, and names its columns of JSON.

    Dates and times go in as the text GDAL's writer gives them, so a table's `where` compares
    that text, and lists and JSON values go in as their JSON text, since a GeoPackage has no
    lists.
    """
    info = _info(source, None)
    meta, table = _arrow(source, datetime_as_string=True)
    json_columns: set[str] = set()
    columns: dict[str, Any] = {}
    for name, kind, subtype in zip(cast(Sequence[str], meta["fields"]), info["ogr_types"], info["ogr_subtypes"], strict=True):
        column = table.column(name)
        if kind in ("OFTDateTime", "OFTTime") or kind.endswith("List") or subtype == "OFSTJSON":
            values = [_copied(kind, subtype, item) for item in column.to_pylist()]
            columns[name] = pa.array([None if item is _ABSENT else item for item in values], type=pa.string())
            if kind.endswith("List") or subtype == "OFSTJSON":
                json_columns.add(name)
        else:
            columns[name] = column
    geometry = meta.get("geometry_name") or "wkb_geometry"
    if geometry in table.column_names:
        columns["geom"] = table.column(geometry)
    pyogrio.write_arrow(  # pyright: ignore[reportUnknownMemberType]
        pa.table(columns),  # pyright: ignore[reportUnknownMemberType]
        copy,
        layer=info.get("layer_name"),
        driver="GPKG",
        geometry_name="geom" if "geom" in columns else None,
        geometry_type="Unknown" if "geom" in columns else None,
        crs=meta.get("crs"),
    )
    return frozenset(json_columns)


def _copied(kind: str, subtype: str, item: Any) -> Any:
    """A value as the GeoPackage copy holds it: a list of fractions as JSON with 17 digits each,
    as GDAL's JSON library writes them; any other value as GDAL's GeoJSON writer gives it."""
    if item is None or kind != "OFTRealList":
        return _value(kind, subtype, item)
    return "[ " + ", ".join(_json_real(float(one)) for one in item) + " ]"


def _json_real(number: float) -> str:
    text = f"{number:.17g}"
    return text if any(mark in text for mark in ".en") else text + ".0"


def read_table(source: Source, table: TablePlan) -> Result[Extract, str]:
    """One table's rows, or why the table could not be read.

    pyogrio reads it; a table that needs ogr2ogr goes through it into a GeoPackage of its own,
    which pyogrio then reads, with no layer or `where` to apply, and its codes resolved.
    """
    match _read(source.path, table, source.native, source.json_columns, resolved=False):
        case Ok() | Err() as done:
            return done
        case NeedsGdal():
            pass
        case _ as never:
            assert_never(never)
    extracted = source.work / f"{table.table}.gpkg"
    match gdal.run(
        seed_script.extract_arguments(str(source.path), str(extracted), table), (str(source.path.parent), str(source.work))
    ):
        case Err(why):
            return Err(why)
        case Ok():
            pass
    match _read(extracted, table.model_copy(update={"layer": None, "where": None}), False, frozenset(), resolved=True):
        case Ok() | Err() as done:
            return done
        case NeedsGdal(why):
            return Err(f"ogr2ogr's copy still needs GDAL: {why}.")
        case _ as never:
            assert_never(never)


def _read(path: Path, table: TablePlan, native: bool, json_columns: frozenset[str], resolved: bool) -> Read:
    """Reads one table with pyogrio, or says why ogr2ogr must read it.

    `resolved` marks ogr2ogr's own copy, whose coded fields already have their words beside
    them.
    """
    try:
        layers = _layers(path)
        if table.layer is None and len(layers) > 1:
            return Err(f"the source holds {len(layers)} layers, {', '.join(layers)}; name one as the table's layer.")
        info = _info(path, table.layer)
    except REFUSALS as error:
        return Err(f"{error}.")
    try:
        meta, arrow = _arrow(path, layer=table.layer, where=table.where, datetime_as_string=True, return_fids=native)
    except REFUSALS as error:
        # GDAL itself prints why it refused the `where`, such as a field it does not know.
        return Err(f"GDAL could not apply the where, {table.where}." if table.where is not None else f"{error}.")
    found = _shapes(meta, arrow)
    if found is None:
        return NeedsGdal("a shape holds curves")
    if any(shape is not None for shape in found):
        crs = None if meta.get("crs") is None else CRS(meta["crs"])
        if crs is None:
            return Err("the table names no coordinate system, so its places cannot be put in longitude and latitude.")
        flat = crs.to_2d() if crs.is_geographic else crs
        if not flat.equals(CRS.from_epsg(4326), ignore_axis_order=True):
            return NeedsGdal("the table is in another coordinate system")
    if not resolved and _text_coded(arrow):
        return NeedsGdal("a field's domain is coded by text")
    flat_shapes = shapes.transform_all(shapes.force_2d(found), _round_points)
    if native:
        return Ok(geojson.rows_of(_native_features(path, table, info, meta, arrow, flat_shapes)))
    return Ok(_extract(_columns(info, meta, arrow, json_columns, resolved), flat_shapes))


def _text_coded(arrow: pa.Table) -> bool:
    """True when a field has a domain whose words pyogrio does not hand over.

    pyogrio gives the words of a domain coded by whole numbers, as a dictionary column, which
    holds each word once and a code for each row; a domain coded by text, such as 'S' for
    'Small', comes as plain text.
    """
    schema: Any = arrow.schema
    fields = [schema.field(name) for name in cast(list[str], schema.names)]
    return any(
        b"GDAL:OGR:domain_name" in cast(dict[bytes, bytes], field.metadata or {}) and not bool(pa.types.is_dictionary(field.type))
        for field in fields
    )


def _shapes(meta: dict[str, Any], arrow: pa.Table) -> list[BaseGeometry | None] | None:
    """The rows' shapes, or None when one is a type only GDAL can straighten, such as an arc."""
    name = meta.get("geometry_name") or "wkb_geometry"
    if name not in arrow.column_names:
        return [cast(BaseGeometry | None, None)] * arrow.num_rows
    try:
        found = shapes.from_wkb(cast(list[bytes | None], arrow.column(name).to_pylist()))
    except (shapes.GEOSException, NotImplementedError):
        return None
    return found if all(shape is None or shapes.plain(shape) for shape in found) else None


_COORDINATE: Callable[[NDArray[np.float64]], NDArray[np.float64]] = np.vectorize(gdal_text.coordinate, otypes=[np.float64])


def _round_points(points: NDArray[np.float64]) -> NDArray[np.float64]:
    """Each coordinate as GDAL's writer writes it and reads it back: 15 decimals, noise trimmed."""
    return points if points.size == 0 else _COORDINATE(points)


def _value(kind: str, subtype: str, item: Any) -> Any:
    """One field's value as GDAL's GeoJSON writer gives it, for the field's GDAL type."""
    if item is None:
        return None
    if subtype == "OFSTJSON":
        return _json_value(str(item))
    match kind:
        case "OFTInteger" | "OFTInteger64":
            return bool(item) if subtype == "OFSTBoolean" else int(item)
        case "OFTReal":
            number = float(item)
            found = gdal_text.single(number) if subtype == "OFSTFloat32" else gdal_text.real(number)
            return _ABSENT if found is None else found
        case "OFTDate":
            return item.isoformat()
        case "OFTDateTime":
            return gdal_text.date_time(str(item))
        case "OFTTime":
            return gdal_text.time_of_day(item.hour, item.minute, item.second, item.microsecond // 1000)
        case "OFTBinary":
            return bytes(item).hex().upper()
        case "OFTIntegerList" | "OFTInteger64List":
            return geojson.jsonc_text([bool(one) if subtype == "OFSTBoolean" else int(one) for one in item])
        case "OFTRealList":
            reals = [
                repr(gdal_text.single(float(one))) if subtype == "OFSTFloat32" else gdal_text.real_text(float(one))
                for one in item
            ]
            return "[ ]" if not reals else "[ " + ", ".join(reals) + " ]"
        case "OFTStringList":
            return geojson.jsonc_text([str(one) for one in item])
        case _:
            return str(item)


def _json_value(text: str) -> Value:
    """A JSON field's value: GDAL's writer writes text that looks like a list or an object as JSON."""
    if len(text) >= 2 and text[0] + text[-1] in ("{}", "[]"):
        try:
            parsed = json.loads(text, parse_int=geojson.NumberText, parse_float=geojson.NumberText)
        except ValueError:
            return text
        return geojson.jsonc_text(parsed)
    return text


@dataclass(frozen=True, slots=True)
class _Column:
    """One field's values for every row, and, for a coded field, the words beside them."""

    name: str
    values: list[Any]
    resolved: list[Any] | None


def _columns(
    info: dict[str, Any], meta: dict[str, Any], arrow: pa.Table, json_columns: frozenset[str], resolved: bool
) -> list[_Column]:
    """Every field's values, as GDAL's GeoJSON writer gives them.

    A field with a ***coded domain***, a list of codes and the words they stand for, comes from
    pyogrio as a dictionary column: its codes, and the words by code. The code stays, and its
    words go in a new field named after it with '_resolved' added, as ogr2ogr's -resolveDomains
    writes them; ogr2ogr's own copy holds that field already.
    """
    columns: list[_Column] = []
    for name, kind, given in zip(cast(Sequence[str], meta["fields"]), info["ogr_types"], info["ogr_subtypes"], strict=True):
        subtype = "OFSTJSON" if name in json_columns else given
        column = arrow.column(name).combine_chunks()
        if isinstance(column, pa.DictionaryArray):
            coded = cast(Any, column)
            codes = cast(list[Any], coded.indices.to_pylist())
            words = cast(list[Any], coded.dictionary.to_pylist())
            found = [
                words[code] if code is not None and 0 <= code < len(words) and words[code] is not None else _ABSENT
                for code in codes
            ]
            columns.append(_Column(name, [_value(kind, subtype, code) for code in codes], None if resolved else found))
        else:
            columns.append(_Column(name, [_value(kind, subtype, item) for item in column.to_pylist()], None))
    return columns


def _extract(columns: list[_Column], found: list[BaseGeometry | None]) -> Extract:
    """The table of a source GDAL reads with no GeoJSON of its own: every field, in order.

    A value GDAL's writer leaves out, such as a number that is not finite, is null, and its row
    lacks the column, as GDAL's GeoJSON feature lacks the property.
    """
    named: dict[str, list[Any]] = {}
    for column in columns:
        named[column.name] = column.values
        if column.resolved is not None:
            named[f"{column.name}_resolved"] = column.resolved
    lacking = {
        name: frozenset(row for row, value in enumerate(found_values, start=1) if value is _ABSENT)
        for name, found_values in named.items()
    }
    held = {name: [None if value is _ABSENT else value for value in found_values] for name, found_values in named.items()}
    return Extract(table_of(found, held, range(1, len(found) + 1)), {name: rows for name, rows in lacking.items() if rows})


def _native_features(
    path: Path, table: TablePlan, info: dict[str, Any], meta: dict[str, Any], arrow: pa.Table, found: list[BaseGeometry | None]
) -> list[Feature]:
    """Features from a GeoJSON source GDAL reads itself, as GDAL's GeoJSON writer gives them.

    GDAL keeps each source feature's own JSON beside its fields, and its GeoJSON writer uses it:
    the feature's own id comes first, outside the properties, and a property the feature lacks
    stays out. pyogrio keeps neither, so the source's JSON is read here too, and each row found
    by its ***feature id***, the number GDAL gives each feature.
    """
    sources = _source_features(path)
    id_column = meta.get("fid_column") or "OGC_FID"
    _, every = _arrow(path, layer=table.layer, columns=[], read_geometry=False, return_fids=True)
    position = {fid: index for index, fid in enumerate(cast(list[int], every.column(id_column).to_pylist()))}
    fids = cast(list[int], arrow.column(id_column).to_pylist())
    columns = _columns(info, meta, arrow, frozenset(), resolved=False)
    kinds = dict(zip(cast(Sequence[str], meta["fields"]), cast(Sequence[str], info["ogr_types"]), strict=True))
    subtypes = dict(zip(cast(Sequence[str], meta["fields"]), cast(Sequence[str], info["ogr_subtypes"]), strict=True))
    features: list[Feature] = []
    for row, (fid, shape) in enumerate(zip(fids, found, strict=True)):
        own, keys, raw = sources[position[fid]]
        properties: list[tuple[str, Value]] = []
        for column in columns:
            # GDAL fills a field named id from the feature's own id when the properties lack one.
            filled = column.name == "id" and "id" not in keys and own is not _ABSENT
            value = _id_as_field(own, kinds.get("id")) if filled else column.values[row]
            if (column.name not in keys and not filled) or value is _ABSENT:
                continue
            if column.name in raw and subtypes[column.name] == "OFSTJSON":
                value = geojson.jsonc_text(raw[column.name])
            properties.append((column.name, value))
        if own is _ABSENT:
            features.append(Feature(None, tuple(properties), shape))
            continue
        # GDAL writes an id field that holds the feature's own id once, outside the properties.
        kept = tuple((name, value) for name, value in properties if not (name == "id" and _same_id(own, value, kinds.get("id"))))
        features.append(Feature(Some(_own_id(own)), kept, shape))
    return features


def _id_as_field(own: Any, kind: str | None) -> Value:
    """The id field's value GDAL makes from the feature's own id, for a feature whose
    properties hold no id: the id's text in a text field, its number in a number field."""
    if own is None or isinstance(own, bool):
        return None
    if kind in ("OFTInteger", "OFTInteger64"):
        try:
            return int(str(own))
        except ValueError:
            return None
    return str(own)


def _same_id(own: Any, value: Value, kind: str | None) -> bool:
    """True when the id field holds the feature's own id, so GDAL writes it once, outside."""
    if isinstance(own, str) and not isinstance(own, geojson.NumberText):
        return kind == "OFTString" and value == own
    if isinstance(own, geojson.NumberText) and own.lstrip("-").isdigit():
        return kind in ("OFTInteger", "OFTInteger64") and value == int(own)
    return False


def _own_id(own: Any) -> Value:
    """A feature's own id as a value: a whole number that fits in 64 bits stays whole."""
    if isinstance(own, geojson.NumberText):
        text = str(own)
        if text.lstrip("-").isdigit() and -(2**63) <= int(text) <= 2**63 - 1:
            return int(text)
        return float(text)
    if own is None or isinstance(own, (bool, str)):
        return own
    return geojson.jsonc_text(own)


def _source_features(path: Path) -> list[tuple[Any, frozenset[str], dict[str, Any]]]:
    """Each source feature's own id, the keys of its properties, and its lists and objects."""
    document = json.loads(path.read_bytes(), parse_int=geojson.NumberText, parse_float=geojson.NumberText)
    found: list[tuple[Any, frozenset[str], dict[str, Any]]] = []
    for item in cast(list[dict[str, Any]], document.get("features", [])):
        properties = item.get("properties")
        given = cast(dict[str, Any], properties) if isinstance(properties, dict) else {}
        raw: dict[str, Any] = {key: value for key, value in given.items() if isinstance(value, (dict, list))}
        found.append((item.get("id", _ABSENT), frozenset(given), raw))
    return found
