"""Why one table could not become SQL.

Each table ends in a script or in one of these, and any one of them stops the run with no
script written; the shell turns each into its message and its exit code. A plain string could
not say whether the rows were cleaned; these cases can, so a stop or a refusal still puts its
table's counts in the log, and a failed extraction does not.
"""

from dataclasses import dataclass
from typing import assert_never

from .plan import TablePlan
from .rows import Outcome


@dataclass(frozen=True, slots=True)
class NotExtracted:
    """GDAL could not copy the table out of the source."""

    table: TablePlan
    why: str


@dataclass(frozen=True, slots=True)
class Stopped:
    """A filter or the stop rules ended the run; `why` already names the table."""

    outcome: Outcome
    why: str


@dataclass(frozen=True, slots=True)
class Refused:
    """The cleaned rows cannot be written as SQL; `why` already names the table."""

    outcome: Outcome
    why: str


type Failure = NotExtracted | Stopped | Refused
"""A ***union***, as F# would write `NotExtracted of TablePlan * string | Stopped of Outcome *
string | Refused of Outcome * string`."""


def cleaned(failure: Failure) -> Outcome | None:
    """The table's counts and log lines, when its rows were cleaned before it failed."""
    match failure:
        case NotExtracted():
            return None
        case Stopped(outcome, _) | Refused(outcome, _):
            return outcome
        case _:
            assert_never(failure)
