"""tidyText: text is put into one Unicode form, runs of spaces become one, ends are trimmed.

Empty text becomes null. Two names that look the same then are the same, so the later
***filters*** that compare names, and the map's labels, see one name, not two. With `fields`,
it tidies those columns alone.
"""

from collections.abc import Sequence
from dataclasses import replace

from ...kernel.pipes import Step
from .. import judge as measure
from ..filter_kind import FilterKind
from ..plan import Area, Model, OnFail, Text
from ..rows import Cleaning, Fix, Keep, Row, Value, Verdict, frozen_values
from ..steps import each_row, filter_step

NAME = "tidyText"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """`fields` is None by default, meaning every text column, since most data needs tidying
    throughout; no onFail, since it fails no row."""

    fields: tuple[Text | None, ...] | None = None


def create(fields: Sequence[str | None] | None = None) -> Step[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file.

    An empty list of fields counts as none given, as in the C# tool.
    """
    only = tuple(fields) if fields else None

    def judge(row: Row) -> Verdict:
        changed: list[str] = []
        values: dict[str, Value] = dict(row.values)
        for column, value in row.values.items():
            if not isinstance(value, str) or (only is not None and column not in only):
                continue
            clean = measure.tidy_text(value)
            if clean == value:
                continue
            changed.append(column)
            # Text that tidies to nothing becomes null, the value of an empty cell.
            values[column] = clean
        # The names are sorted, so the same row gives the same log line on every run.
        if not changed:
            return Keep(row)
        names = ", ".join(sorted(changed))
        return Fix(replace(row, values=frozen_values(values)), f"tidied {names}")

    # It never fails a row, so OnFail.DROP never comes into play.
    return filter_step(NAME, OnFail.DROP, each_row(judge))


def _create(settings: Settings, area: Area | None) -> Step[Cleaning]:
    return create(settings.fields)


KIND = FilterKind(
    NAME,
    "text is put into one Unicode form, runs of spaces become one, ends are trimmed, and empty text becomes null; "
    "`fields` limits it to some columns",
    Settings,
    _create,
)
