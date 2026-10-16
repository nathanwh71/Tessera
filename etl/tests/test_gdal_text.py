"""Values after GDAL's GeoJSON writer, core/gdal_text.py.

The pipeline reads most sources straight from GDAL's memory, and a few through ogr2ogr's GeoJSON;
each number goes through the rounding GDAL's writer gives it, so both ways give the same values.
When the machine has ogr2ogr, the rounding is checked against it on 3,000 numbers.
"""

import json
import random
import shutil
import subprocess
from pathlib import Path

import pytest
from hypothesis import given
from hypothesis import strategies as st

from tessera_etl.core import gdal_text


@pytest.mark.parametrize(
    ("value", "text"),
    [
        (113.55, "113.55"),
        (113.54999999999995, "113.55"),
        (22.19, "22.19"),
        (113.57621445697644, "113.576214456976444"),
        (0.1, "0.1"),
        (-0.30000000000000004, "-0.3"),
        (1e60, "9.99999999999999949e+59"),
        (12345.000000011, "12345.000000010999429"),
        (3.0, "3.0"),
    ],
)
# Each text below is what ogr2ogr 3.8.4 wrote for the number.
def test_rounds_a_coordinate_as_GDAL_did(value: float, text: str) -> None:
    assert gdal_text.coordinate(value) == float(text)


@pytest.mark.parametrize(
    ("value", "text"),
    [(0.30000000000000004, "0.3"), (3.0, "3.0"), (12.5, "12.5"), (1e60, "9.9999999999999995e+59"), (0.1, "0.1")],
)
def test_writes_an_attribute_as_GDAL_did(value: float, text: str) -> None:
    assert gdal_text.real_text(value) == text


def test_leaves_out_a_number_that_is_not_finite() -> None:
    assert gdal_text.real(float("nan")) is None
    assert gdal_text.real(float("inf")) is None


@pytest.mark.parametrize(
    ("text", "written"),
    [
        ("2024-05-01T10:30:00", "2024-05-01T10:30:00"),
        ("2024-05-01T10:30:00.123+08:00", "2024-05-01T10:30:00.123+08:00"),
        ("2024-05-01T10:30:00.000Z", "2024-05-01T10:30:00Z"),
        ("2024-05-01T10:30:00.25-05:00", "2024-05-01T10:30:00.250-05:00"),
    ],
)
def test_writes_a_date_time_as_GDAL_did(text: str, written: str) -> None:
    assert gdal_text.date_time(text) == written


@given(st.floats(min_value=-180, max_value=180))
def test_moves_a_coordinate_by_far_less_than_a_millimetre(value: float) -> None:
    # The test allows 1e-12 of a degree, about a ten-thousandth of a millimetre on the ground.
    assert abs(gdal_text.coordinate(value) - value) < 1e-12


@pytest.mark.skipif(shutil.which("ogr2ogr") is None, reason="needs GDAL's ogr2ogr")
def test_rounds_as_the_machines_ogr2ogr_does(tmp_path: Path) -> None:
    chance = random.Random(20261016)
    values = [chance.uniform(-180, 180) for _ in range(1000)]
    values += [round(chance.uniform(-90, 90), chance.randint(1, 16)) for _ in range(1000)]
    values += [chance.uniform(-1, 1) * 10 ** chance.randint(-8, 12) for _ in range(1000)]
    values += [sign * 10.0**power for power in range(-30, 61, 3) for sign in (1, -1)]
    features = [
        {
            "type": "Feature",
            "properties": {"v": value},
            "geometry": {"type": "Point", "coordinates": [max(-180.0, min(180.0, value)), 0.5]},
        }
        for value in values
    ]
    source, target = tmp_path / "in.geojson", tmp_path / "out.geojson"
    source.write_text(json.dumps({"type": "FeatureCollection", "features": features}), encoding="utf-8")
    subprocess.run(["ogr2ogr", "-f", "GeoJSON", str(target), str(source)], check=True, capture_output=True)
    written = json.loads(target.read_text(encoding="utf-8"))["features"]

    wrong_coordinates = [
        value
        for value, feature in zip(values, written, strict=True)
        if feature["geometry"]["coordinates"][0] != gdal_text.coordinate(max(-180.0, min(180.0, value)))
    ]
    wrong_reals = [
        value for value, feature in zip(values, written, strict=True) if feature["properties"]["v"] != gdal_text.real(value)
    ]
    assert wrong_coordinates == []
    assert wrong_reals == []
