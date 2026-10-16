"""Builds the source files the golden tests read, with GDAL's own Python bindings.

    /usr/bin/python3.12 tests/fixtures/make_fixtures.py

pyogrio reads files but cannot write a geodatabase's coded domains or curves, so this script
uses the osgeo package that comes with GDAL itself (Ubuntu's python3-gdal, GDAL 3.8.4). The
files it makes are committed, so the tests need neither it nor GDAL's bindings. Five sources
were made before this script and are kept as they are: dates.geojson and t.gdb,
macau-pois.geojson, a copy of data/macau-pois.geojson, and cafes.geojson and parks.geojson.
"""

import json
import os
import shutil

from osgeo import gdal, ogr, osr

gdal.UseExceptions()
HERE = os.path.dirname(os.path.abspath(__file__))


def srs(code: int) -> osr.SpatialReference:
    found = osr.SpatialReference()
    found.ImportFromEPSG(code)
    found.SetAxisMappingStrategy(osr.OAMS_TRADITIONAL_GIS_ORDER)
    return found


def field(layer, name, kind, subtype=ogr.OFSTNone, domain=None):
    definition = ogr.FieldDefn(name, kind)
    definition.SetSubType(subtype)
    if domain:
        definition.SetDomainName(domain)
    layer.CreateField(definition)


def add(layer, values, wkt=None):
    feature = ogr.Feature(layer.GetLayerDefn())
    for name, value in values.items():
        if value is None:
            feature.SetFieldNull(name)
        elif isinstance(value, tuple):
            feature.SetField(name, *value)
        else:
            feature.SetField(name, value)
    if wkt is not None:
        feature.SetGeometry(ogr.CreateGeometryFromWkt(wkt))
    layer.CreateFeature(feature)


def sink_gdb(folder: str) -> None:
    """A geodatabase with a coded domain, 3D points, curves, a projected layer and a table."""
    path = os.path.join(folder, "sink.gdb")
    shutil.rmtree(path, ignore_errors=True)
    ds = gdal.GetDriverByName("OpenFileGDB").Create(path, 0, 0, 0, gdal.GDT_Unknown)
    ds.AddFieldDomain(ogr.CreateCodedFieldDomain("KindCodes", "kinds", ogr.OFTInteger, ogr.OFSTNone, {1: "Museum", 2: "Library", 3: None}))
    kinds = ds.CreateLayer("Kinds", srs(4326), ogr.wkbPoint25D)
    field(kinds, "name", ogr.OFTString)
    field(kinds, "kind", ogr.OFTInteger, domain="KindCodes")
    field(kinds, "count", ogr.OFTInteger)
    field(kinds, "big", ogr.OFTInteger64)
    field(kinds, "ratio", ogr.OFTReal)
    field(kinds, "small", ogr.OFTReal, ogr.OFSTFloat32)
    field(kinds, "seen", ogr.OFTDateTime)
    rows = [
        ({"name": "  Café \t Uno ", "kind": 1, "count": 5, "big": 5_000_000_000, "ratio": 0.1 + 0.2, "small": 0.1, "seen": (2024, 5, 1, 10, 30, 0.0, 100)}, "POINT Z (113.55 22.19 4)"),
        ({"name": "Museu", "kind": 2, "count": None, "big": 7, "ratio": 1 / 3, "small": 3.14159, "seen": None}, "POINT Z (113.5501 22.1901 0)"),
        ({"name": "", "kind": 3, "count": 7, "big": None, "ratio": 3.0, "small": None, "seen": (2023, 1, 2, 8, 0, 12.5, 100)}, "POINT Z (113.56 22.2 1.5)"),
        ({"name": "Far", "kind": None, "count": 1, "big": -1, "ratio": 1e20, "small": 1e-7, "seen": (2022, 12, 31, 23, 59, 59.0, 100)}, "POINT Z (0.5 51.5 0)"),
        ({"name": "Odd code", "kind": 9, "count": 2, "big": 0, "ratio": None, "small": 2.5, "seen": None}, "POINT Z (22.2 113.56 0)"),
        ({"name": "No shape", "kind": 1, "count": 3, "big": 1, "ratio": 12.345678901234567, "small": 1.0, "seen": None}, None),
    ]
    for values, wkt in rows:
        add(kinds, values, wkt)
    arcs = ds.CreateLayer("Arcs", srs(4326), ogr.wkbMultiSurface)
    field(arcs, "name", ogr.OFTString)
    add(arcs, {"name": "round"}, "CURVEPOLYGON (CIRCULARSTRING (113.55 22.19, 113.56 22.20, 113.57 22.19, 113.56 22.18, 113.55 22.19))")
    add(arcs, {"name": "plain"}, "POLYGON ((113.55 22.19, 113.56 22.19, 113.56 22.2, 113.55 22.19))")
    bends = ds.CreateLayer("Bends", srs(4326), ogr.wkbMultiCurve)
    field(bends, "name", ogr.OFTString)
    add(bends, {"name": "bend"}, "COMPOUNDCURVE ((113.54 22.18, 113.55 22.19), CIRCULARSTRING (113.55 22.19, 113.56 22.2, 113.57 22.19))")
    lines = ds.CreateLayer("Lines", srs(4326), ogr.wkbMultiLineString25D)
    field(lines, "name", ogr.OFTString)
    add(lines, {"name": "road"}, "MULTILINESTRING Z ((113.60 22.15 1, 113.70 22.15 2))")
    add(lines, {"name": "two"}, "MULTILINESTRING Z ((113.55 22.19 0, 113.56 22.2 0), (113.57 22.19 0, 113.58 22.2 0))")
    metres = ds.CreateLayer("Metres", srs(3857), ogr.wkbPoint)
    field(metres, "name", ogr.OFTString)
    add(metres, {"name": "projected"}, "POINT (12640826.4 2534520.3)")
    add(metres, {"name": "second"}, "POINT (12641826.123456 2535520.987654)")
    ds.AddFieldDomain(ogr.CreateCodedFieldDomain("Sizes", "sizes", ogr.OFTString, ogr.OFSTNone, {"S": "Small", "L": "Large"}))
    labels = ds.CreateLayer("Labels", srs(4326), ogr.wkbPoint)
    field(labels, "size", ogr.OFTString, domain="Sizes")
    field(labels, "kind", ogr.OFTInteger, domain="KindCodes")
    add(labels, {"size": "S", "kind": 2}, "POINT (113.55 22.19)")
    add(labels, {"size": "X", "kind": None}, "POINT (113.56 22.2)")
    table = ds.CreateLayer("Notes", None, ogr.wkbNone)
    field(table, "code", ogr.OFTString)
    field(table, "label", ogr.OFTString)
    add(table, {"code": "1", "label": "Museum"})
    add(table, {"code": "2", "label": None})
    ds = None


def kinds_gpkg(folder: str) -> None:
    """A GeoPackage with true and false, dates, times with offsets, and 3D lines."""
    path = os.path.join(folder, "kinds.gpkg")
    if os.path.exists(path):
        os.remove(path)
    ds = gdal.GetDriverByName("GPKG").Create(path, 0, 0, 0, gdal.GDT_Unknown)
    layer = ds.CreateLayer("kinds", srs(4326), ogr.wkbLineString25D)
    field(layer, "name", ogr.OFTString)
    field(layer, "open", ogr.OFTInteger, ogr.OFSTBoolean)
    field(layer, "day", ogr.OFTDate)
    field(layer, "moment", ogr.OFTDateTime)
    field(layer, "floors", ogr.OFTInteger)
    field(layer, "ID", ogr.OFTString)
    field(layer, "doc", ogr.OFTString, ogr.OFSTJSON)
    add(layer, {"name": "a", "open": 1, "day": (2024, 5, 1, 0, 0, 0, 0), "moment": (2024, 5, 1, 10, 30, 0.25, 132), "floors": 3, "ID": "x1", "doc": '{"a": [1, 2.50], "b": "c/d"}'}, "LINESTRING Z (113.55 22.19 1, 113.56 22.2 2)")
    add(layer, {"name": "b", "open": 0, "day": None, "moment": (2023, 1, 2, 8, 0, 0, 0), "floors": None, "ID": "x2", "doc": "[]"}, "LINESTRING Z (113.57 22.19 0, 113.58 22.2 0)")
    add(layer, {"name": "c", "open": None, "day": (2020, 2, 29, 0, 0, 0, 0), "moment": (2021, 6, 1, 0, 0, 0, 1), "floors": 2_000_000_000, "ID": None, "doc": None}, "LINESTRING EMPTY")
    ds = None


def props_geojson(folder: str) -> None:
    """GeoJSON with ids outside the properties, missing properties, lists and objects."""
    features = [
        {"type": "Feature", "id": "a-1", "properties": {"name": "One", "n": 1, "mixed": 1, "real": 0.30000000000000004, "tags": [1, 2], "obj": {"k": [1.50, "x"]}, "flag": True, "big": 9007199254740993}, "geometry": {"type": "Point", "coordinates": [113.55, 22.19]}},
        {"type": "Feature", "id": "a-2", "properties": {"id": "a-2", "name": "Two", "mixed": "two", "real": 3.0, "words": ["x", "y"], "flag": False}, "geometry": {"type": "Point", "coordinates": [113.551, 22.191, 7]}},
        {"type": "Feature", "properties": {"name": None, "n": 3, "real": 1e-7, "late": "only here", "obj": "plain"}, "geometry": None},
        {"type": "Feature", "id": "a-4", "properties": {"id": "other", "name": "Four", "n": None, "real": 12.5, "reals": [1.25, 0.1]}, "geometry": {"type": "LineString", "coordinates": [[113.55, 22.19], [113.56, 22.2]]}},
        {"type": "Feature", "id": "a-5", "properties": {"name": "Five", "layer": "b", "n": 5}, "geometry": {"type": "Polygon", "coordinates": [[[113.54, 22.18], [113.55, 22.19], [113.55, 22.18], [113.54, 22.19], [113.54, 22.18]]]}},
    ]
    for feature in features:
        feature["properties"].setdefault("layer", "a")
    with open(os.path.join(folder, "props.geojson"), "w", encoding="utf-8") as file:
        json.dump({"type": "FeatureCollection", "features": features}, file, ensure_ascii=False)
    numbered = [dict(feature, id=index * 10) for index, feature in enumerate(features, start=1)]
    with open(os.path.join(folder, "numbered.geojson"), "w", encoding="utf-8") as file:
        json.dump({"type": "FeatureCollection", "features": numbered}, file, ensure_ascii=False)


def places_gdb(folder: str) -> None:
    """The test geodatabase: seven cafés, six with one fault each and one clean, and two
    parks."""
    path = os.path.join(folder, "places.gdb")
    shutil.rmtree(path, ignore_errors=True)
    gdal.VectorTranslate(path, os.path.join(folder, "cafes.geojson"), format="OpenFileGDB", layerName="Cafes")
    gdal.VectorTranslate(path, os.path.join(folder, "parks.geojson"), format="OpenFileGDB", accessMode="update", layerName="Parks", geometryType="MULTIPOLYGON")


def clash_geojson(folder: str) -> None:
    """Two columns whose names become one once GDAL lowers their capitals."""
    features = [{"type": "Feature", "properties": {"Name": "A", "name": "a"}, "geometry": {"type": "Point", "coordinates": [113.55, 22.19]}}]
    with open(os.path.join(folder, "clash.geojson"), "w", encoding="utf-8") as file:
        json.dump({"type": "FeatureCollection", "features": features}, file)


if __name__ == "__main__":
    sources = os.path.join(HERE, "sources")
    os.makedirs(sources, exist_ok=True)
    sink_gdb(sources)
    kinds_gpkg(sources)
    props_geojson(sources)
    places_gdb(sources)
    clash_geojson(sources)
    print("made", sorted(os.listdir(sources)))
