"""The kernel, kernel/pipes.py, and steps joined with >>.

A chain must behave as its ***filters*** do one after another, a stop must end it, and the
script must hold the columns the last filter left. (C#: ChainTests.cs)
"""

from dataclasses import replace

from helpers import MACAU, clean, log_lines, place

from tessera_etl.core import geojson, table_sql
from tessera_etl.core.filters import inside_area, require_fields, swap_coordinates
from tessera_etl.core.plan import OnFail, TablePlan
from tessera_etl.core.rows import Cleaning, Fix, Row, frozen_values
from tessera_etl.core.steps import each_row, filter_step
from tessera_etl.kernel.pipes import Handler, Next, chain, identity, run
from tessera_etl.kernel.result import Ok


def note(name: str) -> Handler[tuple[str, ...]]:
    """A step that only notes its name, so the kernel's tests can see which steps ran, and in
    what order, with no rows at all."""
    return Handler(lambda rest: lambda seen: rest((*seen, name)))


def test_runs_steps_in_the_order_written_and_hands_back_the_context() -> None:
    assert run(note("a") >> note("b") >> note("c"), ()) == ("a", "b", "c")
    assert run(chain([note("a"), note("b")]), ()) == ("a", "b")
    empty: list[Handler[tuple[str, ...]]] = []
    assert run(chain(empty), ()) == ()


def test_lets_a_step_run_code_after_the_rest_of_the_chain() -> None:
    # A step receives the rest of the chain, so it can wrap it: the power the handler shape
    # adds over a plain function.
    def around(rest: Next[tuple[str, ...]]) -> Next[tuple[str, ...]]:
        return lambda seen: (*rest((*seen, "before")), "after")

    assert run(Handler(around) >> note("a"), ()) == ("before", "a", "after")


def test_runs_the_second_filter_on_what_the_first_kept_and_joins_their_logs() -> None:
    steps = swap_coordinates.create(MACAU) >> inside_area.create(MACAU) >> require_fields.create(["name"])
    outcome = clean(steps, [place(1, 22.19, 113.55, name="Swapped"), place(2, 0.5, 51.5, name="Far"), place(3, 113.55, 22.19)])

    assert [row.number for row in outcome.rows] == [1]
    assert log_lines(outcome) == [
        "Row 1: fixed by swapCoordinates: swapped longitude and latitude",
        "Row 2: dropped by insideArea: its shape lies outside the area",
        "Row 3: dropped by requireFields: name is missing",
    ]


def test_ends_the_chain_at_a_stop() -> None:
    ran: list[bool] = []

    def later(rest: Next[Cleaning]) -> Next[Cleaning]:
        def step(cleaning: Cleaning) -> Cleaning:
            ran.append(True)
            return rest(cleaning)

        return step

    outcome = clean(inside_area.create(MACAU, OnFail.STOP) >> Handler(later), [place(1, 0.5, 51.5)])
    assert outcome.stop is not None
    assert not ran


def test_starts_with_a_step_that_changes_nothing() -> None:
    # Identity changes nothing, so a chain that starts or ends with it gives what the filter
    # alone gives.
    rows = [place(1, 113.55, 22.19), place(2, 0.5, 51.5)]
    alone = clean(inside_area.create(MACAU), rows)
    nothing: Handler[Cleaning] = identity()
    for chained in (
        clean(nothing >> inside_area.create(MACAU), rows),
        clean(inside_area.create(MACAU) >> nothing, rows),
    ):
        assert chained.rows == alone.rows
        assert chained.log == alone.log


def test_writes_a_column_a_filter_adds_and_drops_one_a_filter_removes() -> None:
    rows = [place(1, 113.55, 22.19, name="A", owner_phone="123", note=None)]

    def add_area(row: Row) -> Fix:
        return Fix(replace(row, values=frozen_values({**row.values, "area_m2": 12.5})), "added area_m2")

    def drop_phone(row: Row) -> Fix:
        kept = {column: value for column, value in row.values.items() if column != "owner_phone"}
        return Fix(replace(row, values=frozen_values(kept)), "removed owner_phone")

    steps = filter_step("addArea", OnFail.DROP, each_row(add_area)) >> filter_step("dropPhone", OnFail.DROP, each_row(drop_phone))
    kept = clean(steps, rows).rows

    # The empty source column stays; the removed one goes; the new one follows the source's.
    assert geojson.columns(kept, ["name", "owner_phone", "note"]) == ("name", "note", "area_m2")
    sql = table_sql.statements(TablePlan.model_validate({"table": "food.cafes"}), kept, ["name", "owner_phone", "note"])
    assert isinstance(sql, Ok)
    assert 'ALTER TABLE "food"."cafes" ADD COLUMN "area_m2" FLOAT8;' in sql.value
    assert not any("owner_phone" in statement for statement in sql.value)
