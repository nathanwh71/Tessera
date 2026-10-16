"""tidyText: text is put into one Unicode form, runs of spaces become one, ends are trimmed.

Empty text becomes null. Two names that look the same then are the same, so the later
***filters*** that compare names, and the map's labels, see one name, not two. With `fields`,
it tidies those columns alone.
"""

from collections.abc import Sequence
from typing import Any

import pandas as pd
from geopandas import GeoDataFrame

from ...kernel.pipes import Step
from .. import judge as measure
from ..filter_kind import FilterKind
from ..plan import Area, Model, OnFail, Text
from ..rows import Cleaning, Judged, columns
from ..steps import filter_step, fixes

NAME = "tidyText"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """`fields` is None by default, meaning every text column, since most data needs tidying
    throughout; no onFail, since it fails no row."""

    fields: tuple[Text | None, ...] | None = None


def create(fields: Sequence[str | None] | None = None) -> Step[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file.

    An empty list of fields counts as none given.
    """
    only = tuple(fields) if fields else None

    def judge(frame: GeoDataFrame) -> Judged:
        tidy: dict[str, pd.Series[Any]] = {}
        # Each row's changed columns, by name in sorted order, so the same row gives the same
        # log line on every run.
        changed: dict[int, list[str]] = {}
        for name in sorted(name for name in columns(frame) if only is None or name in only):
            texts = measure.as_text(frame[name][measure.is_text(frame[name])])
            clean = measure.tidied(texts)
            # Empty text is untidy too: it becomes null, even though tidying leaves it as it was.
            moved = clean[(clean != texts) | (clean == "")]
            if moved.empty:
                continue
            # Text that tidies to nothing becomes null, the value of an empty cell.
            tidy[name] = frame[name].copy()
            tidy[name].loc[moved.index] = moved.astype(object).replace("", None)
            for row in moved.index:
                changed.setdefault(int(row), []).append(name)
        touched = pd.Series(frame.index.isin(list(changed)), index=frame.index).astype(bool)
        did = pd.Series({row: "tidied " + ", ".join(names) for row, names in changed.items()}, dtype=object)
        return Judged(frame.assign(**tidy), fixes(touched, did))

    # It never fails a row, so OnFail.DROP never comes into play.
    return filter_step(NAME, OnFail.DROP, judge)


def _create(settings: Settings, area: Area | None) -> Step[Cleaning]:
    return create(settings.fields)


KIND = FilterKind(
    NAME,
    "text is put into one Unicode form, runs of spaces become one, ends are trimmed, and empty text becomes null; "
    "`fields` limits it to some columns",
    Settings,
    _create,
)
