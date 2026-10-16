"""dropNearDuplicates: two rows with one name, close together, are one place entered twice.

The name is compared ignoring case and spaces, and the shapes' centres must lie within the
given metres; the first row is kept. Neither test alone is enough: a name repeats across a
city, and a position repeats in a busy building.
"""

from ...kernel.pipes import Handler
from .. import judge as measure
from ..filter_kind import FilterKind
from ..plan import Area, Fraction, Model, OnFail, OnFailSetting, Text
from ..rows import Cleaning, Fail, Keep, Row, Verdict
from ..steps import filter_step

NAME = "dropNearDuplicates"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """`field` and `metres` have no default, since only the publisher knows which column names
    a place and how close counts as the same place."""

    field: Text
    metres: Fraction
    on_fail: OnFailSetting = OnFail.DROP


def create(field: str, metres: float, on_fail: OnFail = OnFail.DROP) -> Handler[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file."""

    def judge(rows: tuple[Row, ...]) -> tuple[Verdict, ...]:
        # Each name keeps a list of earlier places, not one: two cafés of one chain may share a
        # name far apart, and both are real, so a third must be measured against each.
        seen: dict[str, list[tuple[int, tuple[float, float]]]] = {}
        verdicts: list[Verdict] = []
        for row in rows:
            found = measure.text(row.value(field))
            name = None if found is None else measure.tidy_text(found)
            shape = row.shape
            # A row with no name or no shape passes: requireFields and dropEmptyShapes judge
            # those. A name that tidies to nothing counts as no name.
            if name is None or shape is None or shape.is_empty:
                verdicts.append(Keep(row))
                continue
            centroid = shape.centroid
            centre = (float(centroid.x), float(centroid.y))
            earlier = seen.setdefault(measure.folded(name), [])
            verdict: Verdict = Keep(row)
            for number, other in earlier:
                distance = measure.metres(centre, other)
                if distance <= metres:
                    verdict = Fail(row, f"its {field} matches row {number}, {distance:.0f} m away")
                    break
            if isinstance(verdict, Keep):
                earlier.append((row.number, centre))
            verdicts.append(verdict)
        return tuple(verdicts)

    return filter_step(NAME, on_fail, judge)


def _check(settings: Settings, area: Area | None) -> tuple[str, ...]:
    problems: list[str] = []
    if measure.is_blank(settings.field):
        problems.append('needs a field, such as "name".')
    if not settings.metres > 0:
        problems.append("needs metres above 0, such as 25.")
    return tuple(problems)


def _create(settings: Settings, area: Area | None) -> Handler[Cleaning]:
    return create(settings.field, settings.metres, settings.on_fail)


KIND = FilterKind(NAME, "a row whose `field` matches an earlier row's, within `metres` of it, fails", Settings, _create, _check)
