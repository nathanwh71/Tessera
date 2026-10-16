"""dropDuplicateIds: the first row with a value keeps it; each later row with the same value fails.

An id must name one place alone, or two places would claim one key. A row with no value
passes, since requireFields is the ***filter*** that asks for one.
"""

import pandas as pd
from geopandas import GeoDataFrame

from ...kernel.pipes import Step
from .. import judge as measure
from ..filter_kind import FilterKind
from ..plan import Area, Model, OnFail, OnFailSetting, Text
from ..rows import Cleaning, Judged, values
from ..steps import failures, filter_step

NAME = "dropDuplicateIds"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """`field` has no default, since only the publisher knows which column holds the id."""

    field: Text
    on_fail: OnFailSetting = OnFail.DROP


def create(field: str, on_fail: OnFail = OnFail.DROP) -> Step[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file.

    Each run compares a table's ids alone, since one table's ids say nothing about another's.
    """

    def judge(frame: GeoDataFrame) -> Judged:
        ids = pd.Series(values(frame, field), index=frame.index, dtype=object).map(measure.text)
        present = ids.notna()
        firsts = present & ~ids.duplicated()
        first_row = dict(zip(ids[firsts], frame.index[firsts], strict=True))
        repeats = present & ~firsts
        reason = ids[repeats].map(lambda found: f"it repeats {field} '{found}' of row {first_row[found]}")
        return Judged(frame, failures(repeats, reason))

    return filter_step(NAME, on_fail, judge)


def _check(settings: Settings, area: Area | None) -> tuple[str, ...]:
    return ('needs a field, such as "id".',) if measure.is_blank(settings.field) else ()


def _create(settings: Settings, area: Area | None) -> Step[Cleaning]:
    return create(settings.field, settings.on_fail)


KIND = FilterKind(NAME, "a row that repeats an earlier row's `field` fails", Settings, _create, _check)
