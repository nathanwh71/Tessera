"""repairShapes: an invalid shape is rebuilt into a valid one.

An example is an area whose edge crosses itself, which becomes two areas. PostGIS may refuse or
misdraw an invalid shape when it cuts a ***tile***, and its inside and outside are unclear. A
shape that comes out empty fails, since nothing is left to draw.
"""

import re
from dataclasses import replace
from typing import ClassVar

import shapely

from ...kernel.pipes import Handler
from .. import fixer
from ..filter_kind import FilterKind, settings_name
from ..plan import Area, Model, OnFail, OnFailSetting
from ..rows import Cleaning, Fail, Fix, Keep, Row, Verdict
from ..steps import each_row, filter_step

NAME = "repairShapes"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """Only what to do with a shape beyond repair, which is dropped unless the file says stop."""

    dotnet_name: ClassVar[str] = settings_name(NAME)
    on_fail: OnFailSetting = OnFail.DROP


# GEOS, the geometry library behind shapely, adds the place of the fault in brackets, such as
# 'Self-intersection[113.545 22.185]'; NetTopologySuite, which the C# tool used, names the
# fault alone.
_PLACE = re.compile(r"\[[^\]]*\]$")


def create(on_fail: OnFail = OnFail.DROP) -> Handler[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file.

    The repair follows NetTopologySuite's GeometryFixer, which the C# tool used (core/fixer.py),
    and leaves out parts that collapse to lines or points.
    """

    def judge(row: Row) -> Verdict:
        shape = row.shape
        if shape is None or shape.is_empty or shape.is_valid:
            return Keep(row)
        why = _PLACE.sub("", shapely.is_valid_reason(shape)) or "invalid"
        mended = fixer.fix(shape)
        # A repair is a fix; a shape that comes out empty fails, so it obeys onFail.
        if mended.is_empty:
            return Fail(row, "its shape is invalid and cannot be repaired")
        return Fix(replace(row, shape=mended), f"repaired a shape that was invalid ({why})")

    return filter_step(NAME, on_fail, each_row(judge))


def _create(settings: Settings, area: Area | None) -> Handler[Cleaning]:
    return create(settings.on_fail)


KIND = FilterKind(
    NAME,
    "an invalid shape, such as an area whose edge crosses itself, is repaired; one that cannot be repaired fails",
    Settings,
    _create,
)
