"""One table's whole path from its rows to SQL, core/tables.py.

It is checked with tables built here, so each way a table can end is tested without GDAL or a
file.
"""

from helpers import Place, frame_of, place, table

from tessera_etl.core import failure, tables
from tessera_etl.core.filters import require_fields
from tessera_etl.core.geojson import Extract
from tessera_etl.core.pipeline import CleaningPlan
from tessera_etl.core.plan import OnFail, StopWhen
from tessera_etl.core.steps import stop_when
from tessera_etl.kernel.result import Err, Ok
from tessera_etl.shell import program

TWO_CAFES = [place(1, 113.55, 22.19, name="Café Uno"), place(2, 113.56, 22.2, name=None)]
"""Two cafés, the second with no name."""


def require_name(on_fail: OnFail = OnFail.DROP) -> CleaningPlan:
    """A plan that drops, or stops at, a café with no name, and lets any share of rows go."""
    return CleaningPlan(
        table("food.cafes"),
        (require_fields.NAME,),
        require_fields.create(["name"], on_fail) >> stop_when(StopWhen.model_validate({"droppedOverPercent": 100})),
    )


def extract(places: list[Place]) -> Extract:
    return Extract(frame_of(places), {})


def test_cleans_a_table_and_writes_its_SQL() -> None:
    loaded = tables.load(require_name(), extract(TWO_CAFES))

    assert isinstance(loaded, Ok)
    assert (loaded.value.outcome.read, loaded.value.outcome.kept) == (2, 1)
    assert len([statement for statement in loaded.value.sql if statement.startswith("INSERT")]) == 1


def test_fails_a_table_a_filter_stops_and_keeps_its_counts() -> None:
    # A stop comes after the cleaning, so it keeps the counts for the log.
    loaded = tables.load(require_name(OnFail.STOP), extract(TWO_CAFES))

    assert isinstance(loaded, Err)
    assert program.message(loaded.error) == "stopped. requireFields stopped the run at row 2: name is missing. No script written."
    counted = failure.cleaned(loaded.error)
    assert counted is not None
    assert counted.read == 2


def test_fails_rows_SQL_cannot_hold_and_keeps_their_counts() -> None:
    # Two columns that become one name: the rows were cleaned, but SQL cannot hold them.
    clash = [place(1, 113.55, 22.19, name="Café Uno"), place(2, 113.56, 22.2, name="Café Dos", Name="x")]

    loaded = tables.load(require_name(), extract(clash))

    assert isinstance(loaded, Err)
    assert isinstance(loaded.error, failure.Refused)
    counted = failure.cleaned(loaded.error)
    assert counted is not None
    assert counted.kept == 2


def test_keeps_no_counts_for_a_table_it_could_not_extract() -> None:
    assert failure.cleaned(failure.NotExtracted(table("food.cafes"), "no such layer")) is None
