"""The Extract edge: reads each table from the source with pyogrio, as features.

pyogrio is a Python library that reads a ***layer*** of a source, such as a feature class of a
***geodatabase***, through GDAL, straight into ***Arrow*** columns (a compact table format held
in memory). The C# tool instead had ogr2ogr write each table as GeoJSON text and read that
back. The features built here hold what that text held: each value goes through the same round
trip GDAL's writer gave it (core/gdal_text.py), so the cleaned rows, the script and the log come
out the same.

Two cases still run ogr2ogr, as the C# tool did:

- a table holding true curves, such as a geodatabase's arcs, since the straight lines GDAL makes
  of them come from its own drawing rules, which shapely does not share;
- a read pyogrio refuses, such as a misspelt layer or a bad `where`, since ogr2ogr's own message
  is the one the C# tool printed.
"""

import json
import os
import warnings
from collections.abc import Callable, Sequence
from dataclasses import dataclass, field
from typing import Any, cast

import geopandas
import numpy as np
import pyarrow as pa
import pyogrio  # pyright: ignore[reportMissingTypeStubs]
import shapely
from numpy.typing import NDArray
from pyproj import CRS
from shapely.geometry.base import BaseGeometry

from ..core import gdal_text, geojson, seed_script
from ..core.geojson import Extract, Feature, Some
from ..core.plan import TablePlan
from ..core.rows import Value
from ..kernel.result import Err, Ok, Result
from . import gdal

_ABSENT = object()
"""Marks a value GDAL's writer left out of a feature, such as a number that is not finite."""

# Shape types GeoJSON holds: point, line, area, their many-part forms, and collections. Any other
# type, such as a circular arc, needs GDAL to straighten it.
_PLAIN_TYPES = frozenset(range(8))


@dataclass(slots=True)
class Reader:
    """Where each table is read from, and what the reads share.

    `copy` is the GeoPackage copy of a GeoJSON source of several tables; `native` says GDAL's
    GeoJSON driver reads the source itself, which keeps each feature's own id and leaves out a
    property the feature lacks, as ogr2ogr's GeoJSON did.
    """

    source: str
    work: str
    tables: int
    copy: str | None
    native: bool
    json_columns: frozenset[str]
    folders: tuple[str, ...]
    gdal_source: str | None = None
    native_features: list[tuple[Any, frozenset[str], dict[str, Any]]] | None = field(default=None)


def _read_arrow(path: str, **options: Any) -> tuple[dict[str, Any], pa.Table]:
    """pyogrio's read, typed for the rest of this module."""
    meta, table = pyogrio.read_arrow(path, **options)  # pyright: ignore[reportUnknownMemberType, reportUnknownVariableType]
    return cast(dict[str, Any], meta), cast(pa.Table, table)


def _read_info(path: str, layer: str | None) -> dict[str, Any]:
    # pyogrio warns when it picks the first of several layers; the reads here ask for a layer
    # by name whenever the choice matters, so the warning would only clutter the console.
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        info = pyogrio.read_info(path, layer=layer)  # pyright: ignore[reportUnknownMemberType, reportUnknownVariableType]
    return cast(dict[str, Any], info)


def _layers(path: str) -> int:
    found = pyogrio.list_layers(path)  # pyright: ignore[reportUnknownMemberType, reportUnknownVariableType]
    return len(cast(Sequence[Any], found))


def open_source(source: str, tables: int, work: str) -> Result[Reader, str]:
    """Gets a source ready to read, copying it into a GeoPackage when that saves time.

    The copy is made for a GeoJSON source of several tables, as the C# tool made it
    (core/seed_script.py, copy_first): GeoJSON has no index, so each table's read would scan
    the whole file again.
    """
    folders = (os.path.dirname(source), work)
    if not seed_script.copy_first(source, tables):
        native = False
        try:
            native = _read_info(source, None).get("driver") == "GeoJSON"
        except Exception:  # noqa: BLE001 - an unreadable source fails later, with ogr2ogr's words
            native = False
        return Ok(Reader(source, work, tables, None, native, frozenset(), folders))
    copy = os.path.join(work, "source.gpkg")
    try:
        json_columns = _copy(source, copy)
    except Exception:  # noqa: BLE001 - ogr2ogr then names the fault, as in the C# tool
        made = gdal.run(seed_script.copy_arguments(source, copy), folders)
        if isinstance(made, Err):
            return made
        return Ok(Reader(source, work, tables, None, False, frozenset(), folders, gdal_source=copy))
    return Ok(Reader(source, work, tables, copy, False, json_columns, folders))


def _copy(source: str, copy: str) -> frozenset[str]:
    """Copies a GeoJSON source into a GeoPackage, and names its columns of JSON.

    Dates and times go in as the text GDAL's writer gives them, so a table's `where` compares
    the same text, and lists and JSON values go in as their JSON text.
    """
    info = _read_info(source, None)
    meta, table = _read_arrow(source, datetime_as_string=True)
    json_columns: set[str] = set()
    columns: dict[str, pa.Array | pa.ChunkedArray] = {}
    for index, name in enumerate(cast(Sequence[str], meta["fields"])):
        kind, subtype = info["ogr_types"][index], info["ogr_subtypes"][index]
        column = table.column(name)
        if kind in ("OFTDateTime", "OFTTime") or kind.endswith("List") or subtype == "OFSTJSON":
            values = [_copied(kind, subtype, item) for item in column.to_pylist()]
            columns[name] = pa.array([None if item is _ABSENT else item for item in values], type=pa.string())
            if kind.endswith("List") or subtype == "OFSTJSON":
                json_columns.add(name)
        else:
            columns[name] = column
    geometry_name = meta.get("geometry_name") or "wkb_geometry"
    if geometry_name in table.column_names:
        columns["geom"] = table.column(geometry_name)
    copied = pa.table(columns)
    pyogrio.write_arrow(  # pyright: ignore[reportUnknownMemberType]
        copied,
        copy,
        layer=info.get("layer_name"),
        driver="GPKG",
        geometry_name="geom" if "geom" in columns else None,
        geometry_type="Unknown" if "geom" in columns else None,
        crs=meta.get("crs"),
    )
    return frozenset(json_columns)


def _copied(kind: str, subtype: str, item: Any) -> Any:
    """A value as ogr2ogr's GeoPackage copy held it.

    A GeoPackage has no lists, so GDAL keeps a list as JSON text; its numbers with a fraction
    keep 17 significant digits, as GDAL's JSON library writes them.
    """
    if item is None or kind != "OFTRealList":
        return _value(kind, subtype, item)
    return "[ " + ", ".join(_jsonc_real(float(one)) for one in item) + " ]"


def _jsonc_real(number: float) -> str:
    text = f"{number:.17g}"
    return text if any(mark in text for mark in ".en") else text + ".0"


def read_table(reader: Reader, table: TablePlan) -> Result[Result[Extract, str], str]:
    """One table's rows, as ogr2ogr's GeoJSON held them, or why the table could not be read.

    The outer result fails when the table could not be extracted at all; the inner one fails
    when ogr2ogr, run for a table pyogrio could not match, wrote GeoJSON that cannot be read.
    """
    if reader.gdal_source is not None:
        return _with_ogr2ogr(reader, table)
    path = reader.copy or reader.source
    try:
        if table.layer is None and _layers(path) > 1:
            return _with_ogr2ogr(reader, table)
        info = _read_info(path, table.layer)
        meta, arrow = _read_arrow(path, layer=table.layer, where=table.where, datetime_as_string=True, return_fids=reader.native)
    except Exception:  # noqa: BLE001 - ogr2ogr names the fault, in the C# tool's words
        return _with_ogr2ogr(reader, table)
    shapes = _shapes(meta, arrow)
    if shapes is None:
        return _with_ogr2ogr(reader, table)
    match _flat_lonlat(shapes, meta.get("crs")):
        case None:
            return _with_ogr2ogr(reader, table)
        case flat:
            pass
    if reader.native:
        return Ok(Ok(geojson.rows_of(_native_features(reader, table, info, meta, arrow, flat))))
    return Ok(Ok(geojson.rows_of(_features(info, meta, arrow, flat, reader.json_columns))))


def _shapes(meta: dict[str, Any], arrow: pa.Table) -> list[BaseGeometry | None] | None:
    """The shapes of the rows, or None when one is a type only GDAL can straighten."""
    name = meta.get("geometry_name") or "wkb_geometry"
    if name not in arrow.column_names:
        return [None] * arrow.num_rows
    blobs = np.array(arrow.column(name).to_pylist(), dtype=object)
    try:
        shapes = cast(NDArray[np.object_], shapely.from_wkb(blobs))
    except (shapely.errors.GEOSException, NotImplementedError):
        return None
    found: list[BaseGeometry | None] = list(shapes)
    if any(shape is not None and not _plain(shape) for shape in found):
        return None
    return found


def _plain(shape: BaseGeometry) -> bool:
    """True when the shape and every part of it is a type GeoJSON can hold as it is."""
    if int(shapely.get_type_id(shape)) not in _PLAIN_TYPES:
        return False
    if isinstance(shape, shapely.GeometryCollection):
        return all(_plain(part) for part in shape.geoms)
    return True


def _flat_lonlat(shapes: list[BaseGeometry | None], crs_text: str | None) -> list[BaseGeometry | None] | None:
    """The shapes flat, with each number as GDAL's writer left it, when they are in longitude
    and latitude already.

    None when the source uses another coordinate system, or names none: ogr2ogr then reads the
    table, since its PROJ, the library that moves points between systems, may differ from
    pyproj's in the last digit of a number, and since it refuses a table with no system in its
    own words.
    """
    present = [shape for shape in shapes if shape is not None]
    if not present:
        return shapes
    if crs_text is None:
        return None
    crs = CRS.from_user_input(crs_text)
    flat_crs = crs.to_2d() if crs.is_geographic else crs
    if not flat_crs.equals(CRS.from_epsg(4326), ignore_axis_order=True):
        return None
    series = geopandas.GeoSeries(shapes, crs=crs)
    flat = shapely.force_2d(np.asarray(series.values, dtype=object))
    rounded = shapely.transform(flat, _round_points)
    return [None if shape is None else cast(BaseGeometry, shape) for shape in rounded]


_COORDINATE: Callable[[NDArray[np.float64]], NDArray[np.float64]] = np.vectorize(gdal_text.coordinate, otypes=[np.float64])


def _round_points(points: NDArray[np.float64]) -> NDArray[np.float64]:
    """Each coordinate as GDAL's writer wrote it and read back: 15 decimals, noise trimmed."""
    if points.size == 0:
        return points
    return _COORDINATE(points)


def _value(kind: str, subtype: str, item: Any) -> Any:
    """One field's value as ogr2ogr's GeoJSON held it, for the field's GDAL type."""
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
            items = [bool(one) if subtype == "OFSTBoolean" else int(one) for one in item]
            return geojson.jsonc_text(items)
        case "OFTRealList":
            reals = [_list_real(float(one), subtype) for one in item]
            return "[ ]" if not reals else "[ " + ", ".join(reals) + " ]"
        case "OFTStringList":
            return geojson.jsonc_text([str(one) for one in item])
        case _:
            return str(item)


def _list_real(number: float, subtype: str) -> str:
    if subtype == "OFSTFloat32":
        return repr(gdal_text.single(number))
    return gdal_text.real_text(number)


def _json_value(text: str) -> Value:
    """A JSON field's value: GDAL writes text that looks like a list or an object as JSON."""
    if len(text) >= 2 and ((text[0] == "{" and text[-1] == "}") or (text[0] == "[" and text[-1] == "]")):
        try:
            parsed = json.loads(text, parse_int=geojson.NumberText, parse_float=geojson.NumberText, object_pairs_hook=_pairs)
        except ValueError:
            return text
        return geojson.jsonc_text(parsed)
    return text


def _pairs(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    return dict(pairs)


@dataclass(frozen=True, slots=True)
class _Column:
    """One field's values for every row, and, for a coded field, the descriptions beside them."""

    name: str
    values: list[Any]
    resolved: list[Any] | None


def _columns(info: dict[str, Any], meta: dict[str, Any], arrow: pa.Table, json_columns: frozenset[str]) -> list[_Column]:
    """Every field's values, as ogr2ogr's GeoJSON held them.

    A field with a ***coded domain***, a list of codes and the words they stand for, comes from
    pyogrio as a dictionary column: its codes, and the words by code. ogr2ogr's -resolveDomains
    kept the code, and wrote the words in a new field named after it with '_resolved' added.
    """
    fields = cast(Sequence[str], meta["fields"])
    columns: list[_Column] = []
    for index, name in enumerate(fields):
        kind = info["ogr_types"][index]
        subtype = "OFSTJSON" if name in json_columns else info["ogr_subtypes"][index]
        column = arrow.column(name).combine_chunks()
        if isinstance(column, pa.DictionaryArray):
            codes = cast(list[Any], column.indices.to_pylist())
            words = cast(list[Any], column.dictionary.to_pylist())
            values = [_value(kind, subtype, code) for code in codes]
            resolved = [
                words[code] if code is not None and 0 <= code < len(words) and words[code] is not None else _ABSENT for code in codes
            ]
            columns.append(_Column(name, values, resolved))
        else:
            columns.append(_Column(name, [_value(kind, subtype, item) for item in column.to_pylist()], None))
    return columns


def _features(
    info: dict[str, Any], meta: dict[str, Any], arrow: pa.Table, shapes: list[BaseGeometry | None], json_columns: frozenset[str]
) -> list[Feature]:
    """Features from a source GDAL reads with no GeoJSON of its own: every field, in order."""
    columns = _columns(info, meta, arrow, json_columns)
    features: list[Feature] = []
    for row, shape in enumerate(shapes):
        properties: list[tuple[str, Value]] = []
        for column in columns:
            value = column.values[row]
            if value is not _ABSENT:
                properties.append((column.name, value))
            if column.resolved is not None and column.resolved[row] is not _ABSENT:
                properties.append((f"{column.name}_resolved", column.resolved[row]))
        features.append(Feature(None, tuple(properties), shape))
    return features


def _native_features(
    reader: Reader, table: TablePlan, info: dict[str, Any], meta: dict[str, Any], arrow: pa.Table, shapes: list[BaseGeometry | None]
) -> list[Feature]:
    """Features from a GeoJSON source GDAL reads itself, as ogr2ogr's GeoJSON held them.

    GDAL keeps each source feature's own JSON beside its fields, and its GeoJSON writer used
    it: the feature's own id came first, outside the properties, and a property the feature
    lacked stayed out. pyogrio keeps neither, so the source's JSON is read here too, and each
    row found by its ***feature id***, the number GDAL gives each feature.
    """
    sources = _source_features(reader)
    id_column = meta.get("fid_column") or "OGC_FID"
    _, all_ids = _read_arrow(reader.source, layer=table.layer, columns=[], read_geometry=False, return_fids=True)
    position = {fid: index for index, fid in enumerate(cast(list[int], all_ids.column(id_column).to_pylist()))}
    fids = cast(list[int], arrow.column(id_column).to_pylist())
    columns = _columns(info, meta, arrow, frozenset())
    kinds = dict(zip(cast(Sequence[str], meta["fields"]), cast(Sequence[str], info["ogr_types"]), strict=True))
    features: list[Feature] = []
    for row, (fid, shape) in enumerate(zip(fids, shapes, strict=True)):
        own, keys, raw = sources[position[fid]]
        properties: list[tuple[str, Value]] = []
        for column in columns:
            # GDAL fills a field named id from the feature's own id when the properties lack one.
            is_set = column.name in keys or (column.name == "id" and own is not _ABSENT)
            value = column.values[row]
            if column.name == "id" and "id" not in keys and own is not _ABSENT:
                value = _id_as_field(own, kinds.get("id"))
            if not is_set or value is _ABSENT:
                continue
            if column.name in raw and info["ogr_subtypes"][list(kinds).index(column.name)] == "OFSTJSON":
                value = _native_json(raw[column.name], value)
            properties.append((column.name, value))
        found_id: Some | None = None
        if own is not _ABSENT:
            found_id = Some(_own_id(own))
            properties = [(name, value) for name, value in properties if not (name == "id" and _same_id(own, value, kinds.get("id")))]
        features.append(Feature(found_id, tuple(properties), shape))
    return features


def _id_as_field(own: Any, kind: str | None) -> Value:
    """The id field's value GDAL 3.8 made from the feature's own id, for a feature whose
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
    """True when the id field holds the feature's own id, so GDAL wrote it once, outside."""
    if isinstance(own, str) and not isinstance(own, geojson.NumberText):
        return kind == "OFTString" and value == own
    if isinstance(own, geojson.NumberText) and own.lstrip("-").isdigit():
        return kind in ("OFTInteger", "OFTInteger64") and value == int(own)
    return False


def _own_id(own: Any) -> Value:
    """A feature's own id as the C# tool read it from ogr2ogr's GeoJSON."""
    if isinstance(own, geojson.NumberText):
        text = str(own)
        if text.lstrip("-").isdigit() and -(2**63) <= int(text) <= 2**63 - 1:
            return int(text)
        return float(text)
    if own is None or isinstance(own, (bool, str)):
        return own
    return geojson.jsonc_text(own)


def _native_json(raw: Any, value: Value) -> Value:
    """A JSON field's value as GDAL wrote it from the feature's own JSON."""
    if isinstance(raw, (dict, list)):
        return geojson.jsonc_text(raw)
    return value


def _source_features(reader: Reader) -> list[tuple[Any, frozenset[str], dict[str, Any]]]:
    """Each source feature's own id, the keys of its properties, and its raw JSON values."""
    if reader.native_features is None:
        with open(reader.source, "rb") as file:
            document = json.loads(file.read(), parse_int=geojson.NumberText, parse_float=geojson.NumberText)
        found: list[tuple[Any, frozenset[str], dict[str, Any]]] = []
        shared: dict[frozenset[str], frozenset[str]] = {}
        for item in cast(list[dict[str, Any]], document.get("features", [])):
            properties = item.get("properties")
            keys = frozenset(properties) if isinstance(properties, dict) else frozenset()
            raw = {key: value for key, value in cast(dict[str, Any], properties or {}).items() if isinstance(value, (dict, list))}
            found.append((item.get("id", _ABSENT), shared.setdefault(keys, keys), raw))
        reader.native_features = found
    return reader.native_features


def _with_ogr2ogr(reader: Reader, table: TablePlan) -> Result[Result[Extract, str], str]:
    """Reads one table as the C# tool did: ogr2ogr writes it as GeoJSON, and that is read back."""
    source = reader.gdal_source or reader.source
    if seed_script.copy_first(reader.source, reader.tables) and reader.gdal_source is None:
        copy = os.path.join(reader.work, "source-gdal.gpkg")
        made = gdal.run(seed_script.copy_arguments(reader.source, copy), reader.folders)
        if isinstance(made, Err):
            return Err(made.error)
        reader.gdal_source = source = copy
    extracted = os.path.join(reader.work, f"{table.table}.extracted.geojson")
    match gdal.run(seed_script.extract_arguments(source, extracted, table), reader.folders):
        case Err(why):
            return Err(why)
        case Ok():
            pass
    with open(extracted, encoding="utf-8", errors="replace") as file:
        return Ok(geojson.read(file.read()))
