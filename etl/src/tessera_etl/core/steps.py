"""The two kinds of step a table's chain is made of.

***Filters***, built from a judge by `filter_step`, and the ***stop rules***, which
`stop_when` puts last. Each is a ***pure function***: it reads only the context and its
settings, so the tests check every rule with no files.
"""

import operator
from collections.abc import Callable, Iterable
from dataclasses import replace
from functools import reduce
from typing import assert_never

import pandas as pd
from geopandas import GeoDataFrame

from ..kernel.pipes import Step
from .plan import OnFail, StopWhen
from .rows import Act, Cleaning, Failed, Fixed, Judged, LogLine, Verdict

type Judge = Callable[[GeoDataFrame], Judged]
"""A function that judges a whole table at once: it mends some rows and fails others."""

IDENTITY: Step[Cleaning] = Step(lambda cleaning: cleaning)
"""The step that changes nothing: the start of a fold, so a list of no filters is a chain too."""


def chain(steps: Iterable[Step[Cleaning]]) -> Step[Cleaning]:
    """The steps joined with `>>`, in order, as `a >> b >> c` joins them.

    A ***fold***, here `reduce`, starts from IDENTITY and joins each step on in turn.
    """
    # typeshed declares operator.rshift for any operands, which strict pyright reads as unknown.
    return reduce(operator.rshift, steps, IDENTITY)  # pyright: ignore[reportUnknownMemberType, reportUnknownArgumentType]


def fixes(mask: "pd.Series[bool]", did: "str | pd.Series[str]") -> tuple[Fixed, ...]:
    """A Fixed verdict for each row the ***mask*** marks, a mask being a column of true and false.

    `did` says what changed: the same words for every row, or a column of words by row.
    """
    marked = mask[mask].index
    words = [did] * len(marked) if isinstance(did, str) else did.loc[marked].tolist()
    return tuple(Fixed(int(row), word) for row, word in zip(marked, words, strict=True))


def failures(mask: "pd.Series[bool]", reason: "str | pd.Series[str]") -> tuple[Failed, ...]:
    """A Failed verdict for each row the mask marks, with the same reason or a reason by row."""
    marked = mask[mask].index
    words = [reason] * len(marked) if isinstance(reason, str) else reason.loc[marked].tolist()
    return tuple(Failed(int(row), word) for row, word in zip(marked, words, strict=True))


def in_row_order(*groups: tuple[Verdict, ...]) -> tuple[Verdict, ...]:
    """Verdicts of several kinds, merged into the order of the rows, as the log lists them."""
    return tuple(sorted((verdict for group in groups for verdict in group), key=lambda verdict: verdict.row))


def filter_step(name: str, on_fail: OnFail, judge: Judge) -> Step[Cleaning]:
    """Turns a judge into a step.

    Mended rows pass on as mended; each failed row is dropped and logged. With `OnFail.STOP`,
    the first failed row ends the run instead: the step returns the context with the reason,
    so no later step runs.
    """

    def step(cleaning: Cleaning) -> Cleaning:
        judged = judge(cleaning.frame)
        log: list[LogLine] = []
        for verdict in judged.verdicts:
            match verdict:
                case Fixed(row, did):
                    log.append(LogLine(row, name, Act.FIXED, did))
                case Failed(row, reason) if on_fail is OnFail.STOP:
                    # A stop hands back the rows it was given, unjudged: the run ends and
                    # writes no script, so nothing uses them as kept rows.
                    log.append(LogLine(row, name, Act.STOPPED, reason))
                    return replace(
                        cleaning, log=cleaning.log + tuple(log), stop=f"{name} stopped the run at row {row}: {reason}."
                    )
                case Failed(row, reason):
                    log.append(LogLine(row, name, Act.DROPPED, reason))
                case _:
                    assert_never(verdict)
        dropped = [verdict.row for verdict in judged.verdicts if isinstance(verdict, Failed)]
        return replace(cleaning, frame=judged.frame.drop(index=dropped), log=cleaning.log + tuple(log))

    return Step(step)


def stop_when(rules: StopWhen) -> Step[Cleaning]:
    """The stop rules, the last step of every table's chain.

    The run stops when fewer than `rows_under` rows remain, or when more than
    `dropped_over_percent` of the rows read were dropped. A chain that a filter stopped never
    reaches this step.
    """

    def step(cleaning: Cleaning) -> Cleaning:
        kept = len(cleaning.frame)
        percent = 0.0 if cleaning.read == 0 else 100.0 * cleaning.dropped / cleaning.read
        least, most = rules.rows_under, rules.dropped_over_percent
        if least is not None and kept < least:
            reason = f"only {kept} rows remain, fewer than the {least} required"
        elif most is not None and percent > most:
            reason = (
                f"{cleaning.dropped} of {cleaning.read} rows were dropped "
                f"({round(percent, 1):g} %), more than the {most:g} % allowed"
            )
        else:
            return cleaning
        return replace(cleaning, stop=f"{cleaning.table.table}: {reason}.")

    return Step(step)
