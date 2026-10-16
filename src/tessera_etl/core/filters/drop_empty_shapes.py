"""dropEmptyShapes: a row with no shape, or an empty one, fails.

A place with no shape has no position, so no ***tile*** can draw it, and the other shape
***filters*** have nothing to test.
"""

from typing import ClassVar

from ...kernel.pipes import Handler
from ..filter_kind import FilterKind, settings_name
from ..plan import Area, Model, OnFail, OnFailSetting
from ..rows import Cleaning, Fail, Keep, Row, Verdict
from ..steps import each_row, filter_step

NAME = "dropEmptyShapes"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """Only what to do with a failing row, which is dropped unless the file says stop."""

    dotnet_name: ClassVar[str] = settings_name(NAME)
    on_fail: OnFailSetting = OnFail.DROP


def create(on_fail: OnFail = OnFail.DROP) -> Handler[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file."""

    def judge(row: Row) -> Verdict:
        return Fail(row, "it has no shape") if row.shape is None or row.shape.is_empty else Keep(row)

    return filter_step(NAME, on_fail, each_row(judge))


def _create(settings: Settings, area: Area | None) -> Handler[Cleaning]:
    return create(settings.on_fail)


KIND = FilterKind(NAME, "a row with no shape, or an empty one, fails", Settings, _create)
