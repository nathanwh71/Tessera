"""The ***pipeline file***, the ***stop rules***, the log, and GDAL's arguments.

Each is checked without running GDAL, so they run fast and on any machine. (C#: PipelineTests.cs)
"""

from datetime import datetime, timedelta, timezone

import pytest
from helpers import MACAU, clean, place, table

from tessera_etl.core import geojson, pipeline, report, seed_script
from tessera_etl.core.filters import require_fields, tidy_text
from tessera_etl.core.pipeline import DEFAULTS, CleaningPlan, Plan
from tessera_etl.core.plan import OnFail, StopWhen, TablePlan
from tessera_etl.core.rows import Cleaning, Row
from tessera_etl.core.steps import stop_when
from tessera_etl.kernel.pipes import Handler, chain, run
from tessera_etl.kernel.result import Err, Ok, Result

# A pipeline file with no fault. The refusal tests below each change one thing in it, so every
# fault the reader finds comes from that change.
GOOD = """
{
  "source": "data/macau.gdb",
  "output": "db/seed/0002-macau-gdb.sql",
  "tables": [
    { "table": "food.cafes", "layer": "Cafes", "filters": [
      { "filter": "insideArea", "onFail": "stop" },
      { "filter": "requireFields", "fields": ["name"] } ] }
  ]
}
"""


def read(text: str, area: bool = True) -> Plan:
    found = pipeline.read(text, MACAU if area else None)
    assert isinstance(found, Ok), found
    return found.value


def problems(text: str, area: bool = True) -> tuple[str, ...]:
    found: Result[Plan, tuple[str, ...]] = pipeline.read(text, MACAU if area else None)
    assert isinstance(found, Err), found
    return found.error


def test_reads_a_pipeline_and_takes_the_area_from_the_settings() -> None:
    plan = read(GOOD)

    assert plan.pipeline.area == MACAU
    [cafes] = plan.tables
    assert (cafes.table.table, cafes.table.layer) == ("food.cafes", "Cafes")
    assert cafes.filters == ("insideArea", "requireFields")
    # The chain stops at a place outside the area, since the file sets onFail to stop.
    assert clean(cafes.chain, [place(1, 0.5, 51.5, name="Far")]).stop is not None


def test_refuses_a_setting_that_belongs_to_another_filter() -> None:
    text = GOOD.replace('{ "filter": "insideArea", "onFail": "stop" }', '{ "filter": "insideArea", "metres": 25 }')

    assert problems(text) == ("food.cafes: insideArea: 'metres' is not a setting; its settings are onFail.",)


def test_refuses_a_filter_that_lacks_a_setting_it_needs() -> None:
    assert problems(GOOD.replace(', "fields": ["name"]', "")) == ("food.cafes: requireFields: needs fields.",)


def test_refuses_a_misspelt_setting() -> None:
    assert problems(GOOD.replace('"fields"', '"feilds"')) == (
        "food.cafes: requireFields: 'feilds' is not a setting; its settings are fields, onFail.",
        "food.cafes: requireFields: needs fields.",
    )


def test_refuses_a_table_that_is_null() -> None:
    # The JSON reader lets a null stand in a list; it is refused with a reason, not a crash.
    text = '{ "source": "data/places.gdb", "output": "db/seed/0002-places.sql", "tables": [null] }'

    assert problems(text) == ("tables: each entry must be an object that names its table.",)


def test_names_every_fault_at_once() -> None:
    bad = """
        { "source": "x.gdb", "output": "db/seed/parks.sql", "tables": [
          { "table": "Food.Cafes", "filters": [ { "filter": "dropDuplicates" }, { "filter": "dropNearDuplicates", "field": "name" } ] },
          { "table": "outdoors.parks", "filters": [ { "filter": "insideArea" } ] } ] }
        """

    found = problems(bad, area=False)

    assert len(found) == 5
    assert any(problem.startswith("output:") for problem in found)
    assert any("schema.table" in problem for problem in found)
    assert any("'dropDuplicates' is no filter" in problem for problem in found)
    assert "Food.Cafes: dropNearDuplicates: needs metres." in found
    assert any("insideArea: needs an area" in problem for problem in found)


def cafes(rules: StopWhen | None = None) -> TablePlan:
    """One table for the tests below, with the stop rules a test gives it."""
    return table("food.cafes", layer="Cafes", stopWhen=None if rules is None else rules.model_dump(by_alias=True))


def plan_of(rules: StopWhen | None, *steps: tuple[str, Handler[Cleaning]]) -> CleaningPlan:
    """A cleaning plan built by hand, as pipeline.read would build it."""
    return CleaningPlan(
        cafes(rules), tuple(name for name, _ in steps), chain([step for _, step in steps]) >> stop_when(rules or DEFAULTS)
    )


def clean_plan(plan: CleaningPlan, rows: list[Row]) -> Cleaning:
    """Runs a plan's chain on the rows, as the program does for each table."""
    return run(plan.chain, Cleaning.start(plan.table, tuple(rows)))


def require_name() -> tuple[str, Handler[Cleaning]]:
    """The filter the stop-rule tests drop rows with, since dropping by a missing name is easy to count."""
    return (require_fields.NAME, require_fields.create(["name"]))


def ten(named: int) -> list[Row]:
    """Ten places, the first `named` of them with a name.

    With ten rows, each unnamed row is ten per cent of the table: one row meets the default
    limit of 10 %, and a second crosses it.
    """
    return [place(number, 113.55, 22.19, name=f"Place {number}" if number <= named else None) for number in range(1, 11)]


def rules(**settings: object) -> StopWhen:
    return StopWhen.model_validate(settings)


def test_stops_a_table_that_drops_more_than_ten_per_cent_by_default() -> None:
    report_ = clean_plan(plan_of(None, require_name()), ten(named=8))

    assert report_.stop == "food.cafes: 2 of 10 rows were dropped (20 %), more than the 10 % allowed."


def test_lets_a_table_set_its_own_rules() -> None:
    assert clean_plan(plan_of(rules(droppedOverPercent=25), require_name()), ten(named=8)).stop is None
    assert (
        clean_plan(plan_of(rules(droppedOverPercent=50, rowsUnder=9), require_name()), ten(named=8)).stop
        == "food.cafes: only 8 rows remain, fewer than the 9 required."
    )


def test_ends_each_chain_from_the_file_with_the_closest_stop_rules() -> None:
    # The chain pipeline.read builds ends with the stop rules: the table's own when it has
    # them, else the file's, else the defaults.
    file = GOOD.replace('"tables":', '"stopWhen": { "droppedOverPercent": 5 },\n  "tables":').replace(
        '{ "filter": "insideArea", "onFail": "stop" },', ""
    )
    [file_rules] = read(file).tables
    assert clean_plan(file_rules, ten(named=9)).stop == "food.cafes: 1 of 10 rows were dropped (10 %), more than the 5 % allowed."

    own = file.replace('"layer": "Cafes",', '"layer": "Cafes", "stopWhen": { "droppedOverPercent": 20 },')
    [own_rules] = read(own).tables
    assert clean_plan(own_rules, ten(named=9)).stop is None


def test_runs_filters_in_order_and_ends_at_the_first_stop() -> None:
    plan = plan_of(
        rules(droppedOverPercent=100),
        (require_fields.NAME, require_fields.create(["name"], OnFail.STOP)),
        (tidy_text.NAME, tidy_text.create()),
    )

    report_ = clean_plan(plan, ten(named=9))

    assert report_.stop == "requireFields stopped the run at row 10: name is missing."
    assert all(line.filter == require_fields.NAME for line in report_.log)


def test_logs_each_filter_and_each_row_it_touched() -> None:
    file = read(GOOD).pipeline
    plan = plan_of(rules(droppedOverPercent=50), (tidy_text.NAME, tidy_text.create()), require_name())
    done = clean_plan(plan, ten(named=9))

    log = report.log(
        "data/macau.pipeline.json",
        file,
        [(plan, done.outcome)],
        "wrote it.",
        datetime(2026, 10, 15, 13, 0, 0, tzinfo=timezone(timedelta(hours=8))),
    )

    assert "Tessera ETL log, 2026-10-15 13:00:00 +08:00" in log
    assert "  tidyText: kept 10, fixed 0, dropped 0" in log
    assert "  requireFields: kept 9, fixed 0, dropped 1" in log
    assert "  Row 10: dropped by requireFields: name is missing" in log
    assert log.endswith("Result: wrote it.\n")


def test_extracts_one_layer_flat_and_in_longitude_and_latitude() -> None:
    arguments = seed_script.extract_arguments(
        "/data/macau.gdb", "/work/cafes.geojson", table("food.cafes", layer="Cafes", where="kind = 'cafe'")
    )

    assert arguments[:5] == ("-f", "GeoJSON", "/work/cafes.geojson", "/data/macau.gdb", "Cafes")
    assert "-resolveDomains" in arguments
    assert arguments[-2:] == ("-where", "kind = 'cafe'")
    assert arguments[arguments.index("-dim") + 1] == "XY"


@pytest.mark.parametrize(
    ("source", "tables", "copy"),
    [
        ("data/places.geojson", 9, True),
        ("data/places.GeoJSON", 2, True),
        ("data/places.json", 9, True),
        ("data/places.geojson", 1, False),
        ("data/macau.gdb", 9, False),
    ],
)
def test_copies_only_a_GeoJSON_source_of_several_tables(source: str, tables: int, copy: bool) -> None:
    # GeoJSON has no index, so a pipeline of several tables copies it once into a GeoPackage;
    # a geodatabase, or a single table, reads the source as it is.
    assert seed_script.copy_first(source, tables) is copy


def test_copies_the_source_as_it_is_into_a_GeoPackage() -> None:
    assert seed_script.copy_arguments("/data/places.geojson", "/work/source.gpkg") == (
        "-f",
        "GPKG",
        "/work/source.gpkg",
        "/data/places.geojson",
    )


def test_reads_a_features_own_id_as_the_id_column() -> None:
    # GDAL writes a text field named id as the feature's own id, outside its properties.
    text = (
        '{"type":"FeatureCollection","features":[{"type":"Feature","id":"cafes-001","properties":{"name":"A"},"geometry":null}]}'
    )

    found = geojson.read(text)

    assert isinstance(found, Ok)
    assert found.value.columns == ("id", "name")
    assert found.value.rows[0].value("id") == "cafes-001"


@pytest.mark.parametrize(
    "text",
    [
        '{"type":"FeatureCollection","features":[{"type":',
        '{"type":"FeatureCollection"}',
        '{"type":"FeatureCollection","features":{}}',
    ],
)
def test_refuses_GeoJSON_that_is_no_FeatureCollection(text: str) -> None:
    # GDAL may fail part-way and leave a file that is not a FeatureCollection; the reader says
    # why, and the run stops with its log, rather than crashing.
    assert isinstance(geojson.read(text), Err)
