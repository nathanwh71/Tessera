"""Whole runs of the ***pipeline***, from a file ***geodatabase***.

The geodatabase holds seven cafés with one fault each, and two parks, one an area whose edge
crosses itself; tests/fixtures/make_fixtures.py builds it with GDAL.
"""

import json
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
