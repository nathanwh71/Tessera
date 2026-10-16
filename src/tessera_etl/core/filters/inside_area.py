"""insideArea: a shape that does not touch the area's box fails.

The map shows one area alone, so a place outside it is a mistake in the data, such as a wrong
sign on a coordinate, and no ***tile*** of the map would ever show it.
"""

from typing import ClassVar

from ...kernel.pipes import Handler
from .. import judge as measure
from ..filter_kind import FilterKind, needs_area, settings_name
from ..plan import Area, Model, OnFail, OnFailSetting
from ..rows import Cleaning, Fail, Keep, Row, Verdict
from ..steps import each_row, filter_step

NAME = "insideArea"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """Only what to do with a failing row; the box comes from the area, not from these."""

    dotnet_name: ClassVar[str] = settings_name(NAME)
    on_fail: OnFailSetting = OnFail.DROP


def create(area: Area, on_fail: OnFail = OnFail.DROP) -> Handler[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file."""
    area_box = measure.box(area)

    def judge(row: Row) -> Verdict:
        return Keep(row) if measure.inside(row.shape, area_box) else Fail(row, "its shape lies outside the area")

    return filter_step(NAME, on_fail, each_row(judge))


def _create(settings: Settings, area: Area | None) -> Handler[Cleaning]:
    # needs_area refuses the filter when there is no area, so this never meets None.
    assert area is not None
    return create(area, settings.on_fail)


KIND = FilterKind(NAME, "a shape that does not touch the area's box fails", Settings, _create, needs_area)
