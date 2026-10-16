"""npm run inspect, this repository's TypeScript script, against the pipeline's reader.

The script prints a starting pipeline file for a source. This test checks the one thing the
script's own tests cannot: the file it prints for a real ***geodatabase*** passes this
pipeline's reader, so the maintainer's first run does not fail. (C#: InspectTests.cs)

It needs Node.js, GDAL's ogrinfo and the scripts' packages, installed with npm ci in the
repository's root; without them the test is skipped.
"""

import re
import shutil
import subprocess
from pathlib import Path

import pytest
from helpers import MACAU
from staging import FIXTURES

from tessera_etl.core import pipeline
from tessera_etl.kernel.result import Ok

# The repository that holds this folder, etl/, and the scripts beside it.
REPO = Path(__file__).resolve().parents[2]


@pytest.mark.skipif(
    shutil.which("node") is None or not (REPO / "scripts" / "inspect.ts").is_file() or shutil.which("ogrinfo") is None,
    reason="needs Node.js, GDAL's ogrinfo and the repository's scripts/inspect.ts",
)
def test_prints_a_starting_file_the_pipeline_accepts(tmp_path: Path) -> None:
    gdb = tmp_path / "places.gdb"
    shutil.copytree(FIXTURES / "sources" / "places.gdb", gdb)

    done = subprocess.run(
        ["node", str(REPO / "scripts" / "inspect.ts"), str(gdb)], cwd=REPO, capture_output=True, text=True, check=False
    )

    assert done.returncode == 0, done.stderr
    printed = done.stdout
    assert "7 rows of PointZ" in printed
    assert "2 rows of MultiPolygon, in EPSG:4326" in printed
    read = pipeline.read(printed[printed.index("{") :], MACAU)
    assert isinstance(read, Ok), read
    assert [plan.table for plan in read.value.pipeline.tables if plan is not None] == ["places.cafes", "places.parks"]
    assert [plan.layer for plan in read.value.pipeline.tables if plan is not None] == ["Cafes", "Parks"]
    assert "repairShapes" in read.value.tables[1].filters
    assert re.fullmatch(r"db/seed/[0-9]{4}-places\.sql", read.value.pipeline.output)
