"""The values a table's chain passes along: rows, verdicts, log lines and the context.

Each is a frozen record: a step never changes one, but returns a changed copy, made with
`dataclasses.replace`. That keeps every step a ***pure function***, one that reads only its
inputs and changes nothing else, so the tests check each rule with values alone.
"""

from collections.abc import Mapping
from dataclasses import dataclass
from enum import Enum
from types import MappingProxyType

from shapely.geometry.base import BaseGeometry

from .plan import TablePlan

type Value = str | int | float | bool | None
"""A value in a row: text, a whole number, a number with a fraction, true or false, or null.

Rows keep null as a value in its own right, the mark of an empty cell, as GeoJSON does."""


def frozen_values(values: Mapping[str, Value]) -> Mapping[str, Value]:
    """A read-only view of a copy of the values, so no step can change a row in place."""
    return MappingProxyType(dict(values))


@dataclass(frozen=True, slots=True)
class Row:
    """One place read from the source.

    Its number in the source, counting from 1, so the log can name it; its shape, in
    longitude and latitude, or none; and its other values, by column name.
    """

    number: int
    shape: BaseGeometry | None
    values: Mapping[str, Value]

    def value(self, column: str) -> Value:
        """The value in `column`, or None when the row has none.

        A missing column and an empty one mean the same to a ***filter***, a step that checks
        or mends rows.
        """
        return self.values.get(column)


class Act(Enum):
    """What a filter did to one row. Kept rows leave no line in the log; the rest do."""

    KEPT = "kept"
    FIXED = "fixed"
    DROPPED = "dropped"
    STOPPED = "stopped"


@dataclass(frozen=True, slots=True)
class Keep:
    """The row passes as it is."""

    row: Row


@dataclass(frozen=True, slots=True)
class Fix:
    """The row passes as the mended `row`, and the log says what changed."""

    row: Row
    did: str


@dataclass(frozen=True, slots=True)
class Fail:
    """The row cannot pass, for the reason given: it is dropped, or the run stops."""

    row: Row
    reason: str


type Verdict = Keep | Fix | Fail
"""A filter's word on one row: keep it, keep a mended copy, or fail it.

The three cases form a ***union***, as F#'s `Keep of Row | Fix of Row * string | Fail of Row *
string` would: a verdict is always exactly one of them, and each carries only what it needs, so
a kept row has no detail to leave empty. A `match` over a verdict ends with `assert_never`, so
the type checker refuses one that misses a case."""


@dataclass(frozen=True, slots=True)
class LogLine:
    """One line of the log: a row's number, what happened to it, and why."""

    row: int
    filter: str
    act: Act
    detail: str

    def __str__(self) -> str:
        """The line as the log file writes it; one fixed form lets a publisher search the log."""
        return f"Row {self.row}: {self.act.value} by {self.filter}: {self.detail}"


@dataclass(frozen=True, slots=True)
class Outcome:
    """What the log needs of one cleaned table: the rows read and kept, and its log lines.

    The rows themselves are left out, so the program can let each table's rows go once its SQL
    is written, rather than hold every table's rows until the run ends.
    """

    table: TablePlan
    read: int
    kept: int
    log: tuple[LogLine, ...]

    @property
    def dropped(self) -> int:
        """The rows lost, counted from the rows read, whichever filter dropped them."""
        return self.read - self.kept


@dataclass(frozen=True, slots=True)
class Cleaning:
    """The ***context*** every step of a table's chain reads and hands on.

    The table; the rows read from the source; the rows still kept; a log line for each row a
    filter fixed, dropped or stopped at; and, once a step stops the run, the reason. `read`
    stays fixed, so the stop rules can count what the filters dropped.
    """

    table: TablePlan
    read: int
    rows: tuple[Row, ...]
    log: tuple[LogLine, ...]
    stop: str | None

    @staticmethod
    def start(table: TablePlan, rows: tuple[Row, ...]) -> "Cleaning":
        """A table's rows as they were read, before any step has run."""
        return Cleaning(table, len(rows), rows, (), None)

    @property
    def dropped(self) -> int:
        """The rows lost, counted from the rows read, whichever filter dropped them."""
        return self.read - len(self.rows)

    @property
    def outcome(self) -> Outcome:
        """What the log needs of a cleaned table, without its rows."""
        return Outcome(self.table, self.read, len(self.rows), self.log)
