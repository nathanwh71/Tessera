"""dropEmptyShapes: a row with no shape, or an empty one, fails.

A place with no shape has no position, so no ***tile*** can draw it, and the other shape
***filters*** have nothing to test.
"""

from geopandas import GeoDataFrame

from ...kernel.pipes import Step
from ..filter_kind import FilterKind
from ..plan import Area, Model, OnFail, OnFailSetting
from ..rows import Cleaning, Judged
from ..steps import failures, filter_step

NAME = "dropEmptyShapes"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """Only what to do with a failing row, which is dropped unless the file says stop."""

    on_fail: OnFailSetting = OnFail.DROP


def create(on_fail: OnFail = OnFail.DROP) -> Step[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file."""

    def judge(frame: GeoDataFrame) -> Judged:
        empty = frame.geometry.isna() | frame.geometry.is_empty
        return Judged(frame, failures(empty, "it has no shape"))

    return filter_step(NAME, on_fail, judge)


def _create(settings: Settings, area: Area | None) -> Step[Cleaning]:
    return create(settings.on_fail)


KIND = FilterKind(NAME, "a row with no shape, or an empty one, fails", Settings, _create)
