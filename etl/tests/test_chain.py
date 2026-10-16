"""The kernel, kernel/pipes.py, and steps joined with >>.

A chain must behave as its ***filters*** do one after another, a stop must end it, and the
script must hold the columns the last filter left.
"""

from dataclasses import dataclass, replace

import pandas as pd
from geopandas import GeoDataFrame
from helpers import MACAU, clean, kept, log_lines, place

from tessera_etl.core import table_sql
from tessera_etl.core.filters import inside_area, require_fields, swap_coordinates
from tessera_etl.core.plan import OnFail, TablePlan
from tessera_etl.core.rows import Cleaning, Judged
from tessera_etl.core.steps import IDENTITY, chain, filter_step
from tessera_etl.kernel.pipes import Step
from tessera_etl.kernel.result import Ok


@dataclass(frozen=True, slots=True)
class Seen:
    """A context for the kernel's tests alone: the names of the steps that ran, and any stop."""

    names: tuple[str, ...] = ()
    stop: str | None = None


def note(name: str) -> Step[Seen]:
    """A step that only notes its name, so a test can see which steps ran, and in what order."""
    return Step(lambda seen: replace(seen, names=(*seen.names, name)))


def halt(why: str) -> Step[Seen]:
    """A step that stops the chain."""
    return Step(lambda seen: replace(seen, stop=why))


def test_runs_steps_in_the_order_written_and_hands_back_the_context() -> None:
    assert (note("a") >> note("b") >> note("c"))(Seen()).names == ("a", "b", "c")
    assert (note("a") >> note("b"))(Seen()).names == ("a", "b")


def test_joins_a_list_of_steps_into_one_chain() -> None:
    rows = [place(1, 113.55, 22.19, name="A"), place(2, 0.5, 51.5, name="Far"), place(3, 113.55, 22.19)]
    joined = chain([inside_area.create(MACAU), require_fields.create(["name"])])
    by_hand = clean(inside_area.create(MACAU) >> require_fields.create(["name"]), rows)
    assert (kept(clean(joined, rows)), clean(joined, rows).log) == (kept(by_hand), by_hand.log)
    assert kept(clean(chain([]), rows)) == kept(clean(IDENTITY, rows)) == [1, 2, 3]


def test_runs_no_step_after_a_stop() -> None:
    assert (note("a") >> halt("enough") >> note("b"))(Seen()) == Seen(("a",), "enough")


def test_runs_the_second_filter_on_what_the_first_kept_and_joins_their_logs() -> None:
    steps = swap_coordinates.create(MACAU) >> inside_area.create(MACAU) >> require_fields.create(["name"])
    outcome = clean(steps, [place(1, 22.19, 113.55, name="Swapped"), place(2, 0.5, 51.5, name="Far"), place(3, 113.55, 22.19)])

    assert kept(outcome) == [1]
    assert log_lines(outcome) == [
        "Row 1: fixed by swapCoordinates: swapped longitude and latitude",
        "Row 2: dropped by insideArea: its shape lies outside the area",
        "Row 3: dropped by requireFields: name is missing",
    ]


def test_ends_the_chain_at_a_stop() -> None:
    ran: list[bool] = []

    def later(cleaning: Cleaning) -> Cleaning:
        ran.append(True)
        return cleaning

    outcome = clean(inside_area.create(MACAU, OnFail.STOP) >> Step(later), [place(1, 0.5, 51.5)])
    assert outcome.stop is not None
    assert not ran


def test_starts_with_a_step_that_changes_nothing() -> None:
    # Identity changes nothing, so a chain that starts or ends with it gives what the filter
    # alone gives.
    rows = [place(1, 113.55, 22.19), place(2, 0.5, 51.5)]
    alone = clean(inside_area.create(MACAU), rows)
    for chained in (
        clean(IDENTITY >> inside_area.create(MACAU), rows),
        clean(inside_area.create(MACAU) >> IDENTITY, rows),
    ):
        assert kept(chained) == kept(alone)
        assert chained.log == alone.log


def test_writes_a_column_a_filter_adds_and_drops_one_a_filter_removes() -> None:
    rows = [place(1, 113.55, 22.19, name="A", owner_phone="123", note=None)]

    def add_area(frame: GeoDataFrame) -> Judged:
        return Judged(frame.assign(area_m2=pd.Series(12.5, index=frame.index, dtype=object)), ())

    def drop_phone(frame: GeoDataFrame) -> Judged:
        return Judged(frame.drop(columns=["owner_phone"]), ())

    steps = filter_step("addArea", OnFail.DROP, add_area) >> filter_step("dropPhone", OnFail.DROP, drop_phone)
    done = clean(steps, rows).frame

    # The empty source column stays; the removed one goes; the new one follows the source's.
    assert table_sql.written(done, {}) == ["name", "note", "area_m2"]
    sql = table_sql.statements(TablePlan.model_validate({"table": "food.cafes"}), done, {})
    assert isinstance(sql, Ok)
    assert 'ALTER TABLE "food"."cafes" ADD COLUMN "area_m2" FLOAT8;' in sql.value
    assert not any("owner_phone" in statement for statement in sql.value)
