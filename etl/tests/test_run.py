"""Whole runs of the ***pipeline***, from a file ***geodatabase***.

The geodatabase holds seven cafés, six with one fault each and one clean, and two parks, one an
area whose edge crosses itself; tests/fixtures/make_fixtures.py builds it with GDAL.
"""

import json
import math
import re
import struct
import subprocess
import sys
from pathlib import Path

AREA = {"minLongitude": 113.52, "minLatitude": 22.10, "maxLongitude": 113.61, "maxLatitude": 22.22}


def pipeline(cafes: list[dict[str, object]], parks: list[dict[str, object]] | None = None) -> str:
    """A pipeline file for the fixtures, with the cafés' filters a test gives.

    It allows 60 % loss, since four of the seven cafés carry a fault that drops them (57 %),
    well above the default 10 %. The area stands in the file, as no tessera.json sits beside it.
    """
    return json.dumps(
        {
            "source": "places.gdb",
            "output": "seed/0002-test-places.sql",
            "area": AREA,
            "stopWhen": {"droppedOverPercent": 60},
            "tables": [
                {"table": "food.cafes", "layer": "Cafes", "filters": cafes},
                {"table": "outdoors.parks", "layer": "Parks", "lines": True, "filters": parks or [{"filter": "repairShapes"}]},
            ],
        }
    )


def etl(folder: Path, text: str) -> int:
    """Runs the tool in `folder`, as `tessera-etl pipeline.json` would, and returns its exit code."""
    (folder / "pipeline.json").write_text(text, encoding="utf-8")
    return subprocess.run(
        [sys.executable, "-m", "tessera_etl", "pipeline.json"], cwd=folder, capture_output=True, check=False
    ).returncode


def test_cleans_a_geodatabase_into_one_script_and_logs_each_row(geodatabase: Path) -> None:
    code = etl(
        geodatabase,
        pipeline(
            [
                {"filter": "dropEmptyShapes"},
                {"filter": "swapCoordinates"},
                {"filter": "insideArea"},
                {"filter": "tidyText"},
                {"filter": "requireFields", "fields": ["name"]},
                {"filter": "dropDuplicateIds", "field": "id"},
                {"filter": "dropNearDuplicates", "field": "name", "metres": 25},
            ]
        ),
    )

    assert code == 0
    script = (geodatabase / "seed/0002-test-places.sql").read_text(encoding="utf-8")
    # Of the seven cafés, the swapped one is mended and kept; four are dropped.
    assert len([line for line in script.split("\n") if line.startswith('INSERT INTO "food"."cafes"')]) == 3
    assert "'Café Uno'" in script
    assert "'Swapped Café'" in script
    assert "Far Away" not in script
    assert "'MULTIPOLYGON',2" in script
    assert "BEGIN;" not in script

    log = (geodatabase / "seed-logs/0002-test-places.log").read_text(encoding="utf-8")
    assert "Row 2: fixed by swapCoordinates: swapped longitude and latitude" in log
    assert "Row 5: dropped by dropNearDuplicates: its name matches row 1, 15 m away" in log
    assert "Row 1: fixed by repairShapes" in log
    assert "Result: wrote seed/0002-test-places.sql: 2 tables, 5 rows." in log


def test_stops_at_a_bad_row_writes_no_script_and_logs_why(geodatabase: Path) -> None:
    code = etl(geodatabase, pipeline([{"filter": "insideArea", "onFail": "stop"}]))

    assert code == 1
    assert not (geodatabase / "seed/0002-test-places.sql").exists()
    log = (geodatabase / "seed-logs/0002-test-places.log").read_text(encoding="utf-8")
    # Row 2 has its longitude and latitude swapped, and nothing here swaps it back.
    assert "Row 2: stopped by insideArea: its shape lies outside the area" in log
    assert "Result: stopped. insideArea stopped the run at row 2" in log


def test_stores_a_source_in_web_map_metres_in_longitude_and_latitude(tmp_path: Path) -> None:
    """FR-11 and US-24 c2: a source in EPSG:3857, the web map's metres, reaches the script in
    longitude and latitude, SRID 4326. The one place stands on Macau's home point, 113.55 E and
    22.19 N, written in metres with the projection's own formula, so the test does not lean on the
    code it checks."""
    radius = 6378137.0  # The earth's radius at the equator, in metres, as EPSG:3857 takes it.
    x = math.radians(113.55) * radius
    y = math.log(math.tan(math.pi / 4 + math.radians(22.19) / 2)) * radius
    source = {
        "type": "FeatureCollection",
        # GeoJSON's older 'crs' member names the system; GDAL reads it.
        "crs": {"type": "name", "properties": {"name": "urn:ogc:def:crs:EPSG::3857"}},
        "features": [{"type": "Feature", "properties": {"name": "Home"}, "geometry": {"type": "Point", "coordinates": [x, y]}}],
    }
    (tmp_path / "metres.geojson").write_text(json.dumps(source), encoding="utf-8")
    text = json.dumps(
        {
            "source": "metres.geojson",
            "output": "seed/0002-metres.sql",
            "area": AREA,
            # insideArea refuses a point left in metres, millions of 'degrees' from Macau.
            "tables": [{"table": "food.cafes", "filters": [{"filter": "insideArea", "onFail": "stop"}]}],
        }
    )

    assert etl(tmp_path, text) == 0
    script = (tmp_path / "seed/0002-metres.sql").read_text(encoding="utf-8")
    assert "'geom',4326,'POINT',2" in script
    [hexed] = re.findall(r"VALUES \('([0-9A-F]+)'", script)
    # The shape is a point in extended WKB, read here field by field: the byte order (1, least
    # significant byte first), the type with its 'has an SRID' flag, the SRID, then x and y.
    order, kind, srid, longitude, latitude = struct.unpack("<BIIdd", bytes.fromhex(hexed))
    assert (order, kind, srid) == (1, 0x20000001, 4326)
    assert (round(longitude, 6), round(latitude, 6)) == (113.55, 22.19)
