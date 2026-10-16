"""requireFields: a row missing any of the fields, or holding only spaces there, fails.

A place with no name has no label on the map and cannot be told from its neighbours.
"""

from collections.abc import Sequence

import pandas as pd
from geopandas import GeoDataFrame

from ...kernel.pipes import Step
from .. import judge as measure
from ..filter_kind import FilterKind
from ..plan import Area, Model, OnFail, OnFailSetting, Text
from ..rows import Cleaning, Judged
from ..steps import failures, filter_step

NAME = "requireFields"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """`fields` has no default, since only the publisher knows which columns a place cannot
    lack; a failing row is dropped unless the file says stop. .NET's reader let a null stand in
    the list, which this filter then reads as a field that is always missing."""

    fields: tuple[Text | None, ...]
    on_fail: OnFailSetting = OnFail.DROP


def create(fields: Sequence[str | None], on_fail: OnFail = OnFail.DROP) -> Step[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file.

    A row names the first field it lacks, in the order given.
    """
    names = tuple(fields)

    def judge(frame: GeoDataFrame) -> Judged:
        reason = pd.Series(None, index=frame.index, dtype=object)
        for field in reversed(names):
            missing = measure.blank(frame[field]) if field is not None and field in frame.columns else reason.isna() | True
            reason = reason.mask(missing, f"{field or ''} is missing")
        return Judged(frame, failures(reason.notna(), reason))

    return filter_step(NAME, on_fail, judge)


def _check(settings: Settings, area: Area | None) -> tuple[str, ...]:
    return ('needs fields, such as ["name"].',) if not settings.fields else ()


def _create(settings: Settings, area: Area | None) -> Step[Cleaning]:
    return create(settings.fields, settings.on_fail)


KIND = FilterKind(NAME, "a row missing any of `fields`, or holding only spaces there, fails", Settings, _create, _check)
