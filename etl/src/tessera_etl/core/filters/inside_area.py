"""insideArea: a shape that does not touch the area's box fails.

The map shows one area alone, so a place outside it is a mistake in the data, such as a wrong
sign on a coordinate, and no ***tile*** of the map would ever show it.
"""

import pandas as pd
from geopandas import GeoDataFrame

from ...kernel.pipes import Step
from .. import judge as measure
from ..filter_kind import FilterKind, needs_area
from ..plan import Area, Model, OnFail, OnFailSetting
from ..rows import Cleaning, Judged
from ..steps import failures, filter_step

NAME = "insideArea"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """Only what to do with a failing row; the box comes from the area, not from these."""

    on_fail: OnFailSetting = OnFail.DROP


def create(area: Area, on_fail: OnFail = OnFail.DROP) -> Step[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file."""
    area_box = measure.box(area)

    def judge(frame: GeoDataFrame) -> Judged:
        outside = pd.Series(~measure.inside(frame.geometry.to_numpy(), area_box), index=frame.index)
        return Judged(frame, failures(outside, "its shape lies outside the area"))

    return filter_step(NAME, on_fail, judge)


def _create(settings: Settings, area: Area | None) -> Step[Cleaning]:
    # needs_area refuses the filter when there is no area, so this never meets None.
    assert area is not None
    return create(area, settings.on_fail)


KIND = FilterKind(NAME, "a shape that does not touch the area's box fails", Settings, _create, needs_area)
