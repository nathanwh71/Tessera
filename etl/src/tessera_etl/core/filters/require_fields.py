"""requireFields: a row missing any of the fields, or holding only spaces there, fails.

A place with no name has no label on the map and cannot be told from its neighbours.
"""

from collections.abc import Sequence

from ...kernel.pipes import Handler
from .. import judge as measure
from ..filter_kind import FilterKind
from ..plan import Area, Model, OnFail, OnFailSetting, Text
from ..rows import Cleaning, Fail, Keep, Row, Verdict
from ..steps import each_row, filter_step

NAME = "requireFields"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """`fields` has no default, since only the publisher knows which columns a place cannot
    lack; a failing row is dropped unless the file says stop. .NET's reader let a null stand in
    the list, which this filter then reads as a field that is always missing."""

    fields: tuple[Text | None, ...]
    on_fail: OnFailSetting = OnFail.DROP


def create(fields: Sequence[str | None], on_fail: OnFail = OnFail.DROP) -> Handler[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file."""
    names = tuple(fields)

    def judge(row: Row) -> Verdict:
        for field in names:
            if measure.is_blank(None if field is None else row.value(field)):
                return Fail(row, f"{field or ''} is missing")
        return Keep(row)

    return filter_step(NAME, on_fail, each_row(judge))


def _check(settings: Settings, area: Area | None) -> tuple[str, ...]:
    return ('needs fields, such as ["name"].',) if not settings.fields else ()


def _create(settings: Settings, area: Area | None) -> Handler[Cleaning]:
    return create(settings.fields, settings.on_fail)


KIND = FilterKind(NAME, "a row missing any of `fields`, or holding only spaces there, fails", Settings, _create, _check)
