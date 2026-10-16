"""repairShapes: an invalid shape is rebuilt into a valid one.

An example is an area whose edge crosses itself, which becomes two areas. PostGIS may refuse or
misdraw an invalid shape when it cuts a ***tile***, and its inside and outside are unclear. A
shape that comes out empty fails, since nothing is left to draw.
"""

import re

import pandas as pd
import shapely
from geopandas import GeoDataFrame, GeoSeries

from ...kernel.pipes import Step
from ..filter_kind import FilterKind
from ..plan import Area, Model, OnFail, OnFailSetting
from ..rows import Cleaning, Judged
from ..steps import failures, filter_step, fixes, in_row_order

NAME = "repairShapes"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """Only what to do with a shape beyond repair, which is dropped unless the file says stop."""

    on_fail: OnFailSetting = OnFail.DROP


# GEOS, the geometry library behind shapely, adds the place of the fault in brackets, such as
# 'Self-intersection[113.545 22.185]'; the log names the fault alone, so its lines stay short.
_PLACE = re.compile(r"\[[^\]]*\]$")


def create(on_fail: OnFail = OnFail.DROP) -> Step[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file.

    The repair is GEOS's 'structure' method: it rebuilds each area from its rings, so an area
    whose edge crosses itself becomes the parts it encloses, and it leaves out parts that
    collapse to lines or points.
    """

    def judge(frame: GeoDataFrame) -> Judged:
        shapes = frame.geometry.to_numpy()
        broken = ~shapely.is_missing(shapes) & ~shapely.is_empty(shapes) & ~shapely.is_valid(shapes)
        mended = shapes.copy()
        mended[broken] = shapely.make_valid(shapes[broken], method="structure", keep_collapsed=False)
        why = pd.Series(shapely.is_valid_reason(shapes), index=frame.index).str.replace(_PLACE, "", regex=True)
        lost = pd.Series(broken & shapely.is_empty(mended), index=frame.index)
        repaired = pd.Series(broken, index=frame.index) & ~lost
        # A shape that comes out empty fails, so it obeys onFail; the row keeps its shape.
        mended[lost.to_numpy()] = shapes[lost.to_numpy()]
        return Judged(
            frame.set_geometry(GeoSeries(mended, index=frame.index, crs=frame.crs)),
            in_row_order(
                # Series.replace swaps whole values, so a reason left empty reads 'invalid'.
                fixes(repaired, "repaired a shape that was invalid (" + why.replace("", "invalid") + ")"),
                failures(lost, "its shape is invalid and cannot be repaired"),
            ),
        )

    return filter_step(NAME, on_fail, judge)


def _create(settings: Settings, area: Area | None) -> Step[Cleaning]:
    return create(settings.on_fail)


KIND = FilterKind(
    NAME,
    "an invalid shape, such as an area whose edge crosses itself, is repaired; one that cannot be repaired fails",
    Settings,
    _create,
)
