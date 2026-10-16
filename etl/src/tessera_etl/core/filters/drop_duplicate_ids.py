"""dropDuplicateIds: the first row with a value keeps it; each later row with the same value fails.

An id must name one place alone, or two places would claim one key. A row with no value
passes, since requireFields is the ***filter*** that asks for one.
"""

from ...kernel.pipes import Handler
from .. import judge as measure
from ..filter_kind import FilterKind
from ..plan import Area, Model, OnFail, OnFailSetting, Text
from ..rows import Cleaning, Fail, Keep, Row, Verdict
from ..steps import filter_step

NAME = "dropDuplicateIds"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """`field` has no default, since only the publisher knows which column holds the id."""

    field: Text
    on_fail: OnFailSetting = OnFail.DROP


def create(field: str, on_fail: OnFail = OnFail.DROP) -> Handler[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file.

    Each run starts a fresh table of ids, since one table's ids say nothing about another's.
    """

    def judge(rows: tuple[Row, ...]) -> tuple[Verdict, ...]:
        first: dict[str, int] = {}
        verdicts: list[Verdict] = []
        for row in rows:
            found = measure.text(row.value(field))
            if found is None:
                verdicts.append(Keep(row))
            elif found in first:
                verdicts.append(Fail(row, f"it repeats {field} '{found}' of row {first[found]}"))
            else:
                first[found] = row.number
                verdicts.append(Keep(row))
        return tuple(verdicts)

    return filter_step(NAME, on_fail, judge)


def _check(settings: Settings, area: Area | None) -> tuple[str, ...]:
    return ('needs a field, such as "id".',) if measure.is_blank(settings.field) else ()


def _create(settings: Settings, area: Area | None) -> Handler[Cleaning]:
    return create(settings.field, settings.on_fail)


KIND = FilterKind(NAME, "a row that repeats an earlier row's `field` fails", Settings, _create, _check)
