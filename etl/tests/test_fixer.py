"""The repair of invalid shapes, core/fixer.py, against NetTopologySuite's own.

tests/golden/fixer holds 679 shapes, most of them invalid, and what NetTopologySuite 2.6, the
library the C# tool used, said of each: why it is invalid, and the shape GeometryFixer made of
it. Each must come out the same here, corner for corner.
"""

import re
from pathlib import Path

import shapely

from tessera_etl.core import fixer, shapes

FOLDER = Path(__file__).parent / "golden" / "fixer"


def test_repairs_every_shape_as_NetTopologySuite_did() -> None:
    inputs = (FOLDER / "shapes.wkt").read_text().splitlines()
    expected = (FOLDER / "nts.tsv").read_text().splitlines()
    wrong: list[int] = []
    for number, (text, answer) in enumerate(zip(inputs, expected, strict=True), start=1):
        shape = shapely.from_wkt(text)
        why = "valid" if shape.is_valid else re.sub(r"\[[^\]]*\]$", "", shapes.invalid_reason(shape))
        if f"{why}\t{shapely.to_wkt(fixer.fix(shape), rounding_precision=-1)}" != answer:
            wrong.append(number)
    assert wrong == []
