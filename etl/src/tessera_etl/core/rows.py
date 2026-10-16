"""The values a table's chain passes along: the table, the filters' verdicts, log lines and the context.

A table is a ***GeoDataFrame***, geopandas' table of rows with a column of shapes. Its index
numbers each row as the source does, counting from 1, so the log can name it; its shapes are in
longitude and latitude, or None; and each other column holds plain values of any kind. A step
never changes a table or a record, but returns a changed copy, so each step stays a ***pure
function***, one that reads only its inputs and changes nothing else.
"""

from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from enum import Enum

import numpy as np
import pandas as pd
from geopandas import GeoDataFrame, GeoSeries
from shapely.geometry.base import BaseGeometry

from .plan import TablePlan

type Value = str | int | float | bool | None
"""A value in a row: text, a whole number, a number with a fraction, true or false, or null."""


def table_of(
    shapes: Sequence[BaseGeometry | None], columns: Mapping[str, Sequence[Value]], numbers: Sequence[int]
) -> GeoDataFrame:
    """A table from its shapes, its columns of values, and each row's number in the source.

    Each column keeps Python's own values, as an 'object' column, so a column may mix text with
    numbers, and a whole number never turns into a fraction, as pandas would make it beside a
    null.
    """
    index = pd.Index(numbers, name="row")
    frame = {name: pd.Series(list(values), index=index, dtype=object) for name, values in columns.items()}
    shapes_array = np.empty(len(shapes), dtype=object)
    shapes_array[:] = list(shapes)
    return GeoDataFrame(frame, geometry=GeoSeries(shapes_array, index=index, crs="EPSG:4326"), index=index)


def columns(frame: GeoDataFrame) -> list[str]:
    """The table's columns of values, in order: every column but the shapes."""
    return [str(name) for name in frame.columns if name != frame.geometry.name]


def shapes(frame: GeoDataFrame) -> list[BaseGeometry | None]:
    """The table's shapes, in row order."""
    return list(frame.geometry.to_numpy())


def values(frame: GeoDataFrame, column: str) -> list[Value]:
    """A column's values, in row order; None in each row for a column the table lacks."""
    return list(frame[column].to_numpy()) if column in frame.columns else [None] * len(frame)


def numbers(frame: GeoDataFrame) -> list[int]:
    """Each row's number in the source, in row order."""
    return [int(number) for number in frame.index]


class Act(Enum):
    """What a filter did to one row it touched. Kept rows leave no line in the log."""

    FIXED = "fixed"
    DROPPED = "dropped"
    STOPPED = "stopped"


@dataclass(frozen=True, slots=True)
class Fixed:
    """The filter mended the row, and the log says what changed."""

    row: int
    did: str


@dataclass(frozen=True, slots=True)
class Failed:
    """The row cannot pass, for the reason given: it is dropped, or the run stops."""

    row: int
    reason: str


type Verdict = Fixed | Failed
"""A filter's word on one row it does not pass as it is: mended, or failed.

The two cases form a ***union***, as F#'s `Fixed of int * string | Failed of int * string`
would: a verdict is always exactly one of them. A row with no verdict passes as it is. A `match`
over a verdict ends with `assert_never`, so the type checker refuses one that misses a case."""


@dataclass(frozen=True, slots=True)
class Judged:
    """What a filter made of a table: the table with each mended row mended, and its verdicts.

    The table still holds every row, failed ones too; the verdicts come in row order.
    """

    frame: GeoDataFrame
    verdicts: tuple[Verdict, ...]


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

    The table's plan; the number of rows read from the source; the rows still kept; a log line
    for each row a filter fixed, dropped or stopped at; and, once a step stops the run, the
    reason. `read` stays fixed, so the stop rules can count what the filters dropped.
    """

    table: TablePlan
    read: int
    frame: GeoDataFrame
    log: tuple[LogLine, ...]
    stop: str | None

    @staticmethod
    def start(table: TablePlan, frame: GeoDataFrame) -> "Cleaning":
        """A table's rows as they were read, before any step has run."""
        return Cleaning(table, len(frame), frame, (), None)

    @property
    def dropped(self) -> int:
        """The rows lost, counted from the rows read, whichever filter dropped them."""
        return self.read - len(self.frame)

    @property
    def outcome(self) -> Outcome:
        """What the log needs of a cleaned table, without its rows."""
        return Outcome(self.table, self.read, len(self.frame), self.log)
