"""The two kinds of step a table's chain is made of.

***Filters***, built from a judge by `filter_step`, and the ***stop rules***, which
`stop_when` puts last. Each is a ***pure function***: it reads only the context and its
settings, so the tests check every rule with no files.
"""

from collections.abc import Callable
from dataclasses import replace
from typing import assert_never

from ..kernel.pipes import Handler, Next
from . import dotnet
from .plan import OnFail, StopWhen
from .rows import Act, Cleaning, Fail, Fix, Keep, LogLine, Row, Verdict

type Judge = Callable[[tuple[Row, ...]], tuple[Verdict, ...]]
"""A function that gives one verdict for each row of a table, in order."""


def filter_step(name: str, on_fail: OnFail, judge: Judge) -> Handler[Cleaning]:
    """Turns a judge into a step.

    Kept and mended rows pass on to the next step; a failed row is dropped and logged. With
    `OnFail.STOP`, the first failed row ends the run instead: the step returns the context with
    the reason, and never calls the steps after it.
    """

    def wrap(next_step: Next[Cleaning]) -> Next[Cleaning]:
        def step(cleaning: Cleaning) -> Cleaning:
            kept: list[Row] = []
            log: list[LogLine] = []
            for verdict in judge(cleaning.rows):
                number = verdict.row.number
                match verdict:
                    case Fail(_, reason):
                        if on_fail is OnFail.STOP:
                            # A stop hands back the rows it was given, unjudged: the run ends
                            # and writes no script, so nothing uses them as kept rows.
                            log.append(LogLine(number, name, Act.STOPPED, reason))
                            return replace(
                                cleaning,
                                log=cleaning.log + tuple(log),
                                stop=f"{name} stopped the run at row {number}: {reason}.",
                            )
                        log.append(LogLine(number, name, Act.DROPPED, reason))
                    case Fix(row, did):
                        log.append(LogLine(number, name, Act.FIXED, did))
                        kept.append(row)
                    case Keep(row):
                        kept.append(row)
                    case _:
                        assert_never(verdict)
            return next_step(replace(cleaning, rows=tuple(kept), log=cleaning.log + tuple(log)))

        return step

    return Handler(wrap)


def each_row(judge: Callable[[Row], Verdict]) -> Judge:
    """A judge of one row at a time, for filters that need no other row to decide.

    Only the two duplicate filters must see the rows before each one.
    """
    return lambda rows: tuple(judge(row) for row in rows)


def stop_when(rules: StopWhen) -> Handler[Cleaning]:
    """The stop rules, the last step of every table's chain.

    The run stops when fewer than `rows_under` rows remain, or when more than
    `dropped_over_percent` of the rows read were dropped. A chain that a filter stopped never
    reaches this step.
    """

    def wrap(next_step: Next[Cleaning]) -> Next[Cleaning]:
        def step(cleaning: Cleaning) -> Cleaning:
            percent = 0.0 if cleaning.read == 0 else 100.0 * cleaning.dropped / cleaning.read
            least, most = rules.rows_under, rules.dropped_over_percent
            if least is not None and len(cleaning.rows) < least:
                reason = f"only {len(cleaning.rows)} rows remain, fewer than the {least} required"
            elif most is not None and percent > most:
                reason = (
                    f"{cleaning.dropped} of {cleaning.read} rows were dropped "
                    f"({dotnet.pattern_text(percent, 1)} %), more than the {dotnet.number_text(most)} % allowed"
                )
            else:
                return next_step(cleaning)
            return replace(cleaning, stop=f"{cleaning.table.table}: {reason}.")

        return step

    return Handler(wrap)
