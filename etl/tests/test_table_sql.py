"""The SQL that makes and fills one table, core/table_sql.py.

It is checked against the text GDAL's PGDump driver wrote for the same rows, so a new script
loads as the old ones did. (C#: TableSqlTests.cs)
"""

import pytest
from helpers import shaped, table
from shapely.geometry import LineString, MultiLineString, Point

from tessera_etl.core import table_sql
from tessera_etl.core.rows import Row, Value
from tessera_etl.kernel.result import Err, Ok

CAFES = table("food.cafes")


def statements(rows: list[Row], columns: list[str], lines: bool = False) -> tuple[str, ...]:
    found = table_sql.statements(table("food.cafes", lines=lines), rows, columns)
    assert isinstance(found, Ok), found
    return found.value


def test_writes_a_table_as_GDAL_wrote_it() -> None:
    # The statements GDAL wrote for one café, line for line, less its BEGIN and COMMIT, with
    # the statistics the script adds. The shape's hex is GDAL's own, for the point 113.55, 22.19.
    rows = [shaped(1, Point(113.55, 22.19), id="cafes-001", name="O'Neil's", open=True)]

    assert statements(rows, ["id", "name", "open"]) == (
        "SET standard_conforming_strings = ON;",
        'DROP TABLE IF EXISTS "food"."cafes" CASCADE;',
        'CREATE TABLE "food"."cafes"();',
        'ALTER TABLE "food"."cafes" ADD COLUMN "fid" SERIAL CONSTRAINT "cafes_pk" PRIMARY KEY;',
        "SELECT AddGeometryColumn('food','cafes','geom',4326,'POINT',2);",
        'ALTER TABLE "food"."cafes" ADD COLUMN "id" VARCHAR;',
        'ALTER TABLE "food"."cafes" ADD COLUMN "name" VARCHAR;',
        'ALTER TABLE "food"."cafes" ADD COLUMN "open" BOOLEAN;',
        'INSERT INTO "food"."cafes" ("geom", "id", "name", "open") VALUES '
        "('0101000020E61000003333333333635C40713D0AD7A3303640', 'cafes-001', 'O''Neil''s', 't');",
        'CREATE INDEX "cafes_geom_geom_idx" ON "food"."cafes" USING GIST ("geom");',
        'ANALYZE "food"."cafes";',
    )


def test_leaves_out_a_shape_a_row_lacks() -> None:
    # A row with no shape leaves the column out of its INSERT, so it holds NULL; a value it
    # lacks is NULL too.
    assert 'INSERT INTO "food"."cafes" ("name") VALUES (NULL);' in statements([shaped(1, None, name=None)], ["name"])


def test_gives_the_column_the_one_type_its_shapes_share() -> None:
    # A table of lines or areas stores one part as a set of one, so the column keeps one type,
    # as GDAL's PROMOTE_TO_MULTI did; shapes of two kinds take the column type that holds any.
    line = LineString([(113.55, 22.19), (113.56, 22.2)])
    promoted = statements([shaped(1, line), shaped(2, MultiLineString([line]))], [], lines=True)

    assert "SELECT AddGeometryColumn('food','cafes','geom',4326,'MULTILINESTRING',2);" in promoted
    assert all("'0105000020E6100000" in insert for insert in promoted if insert.startswith("INSERT"))
    mixed = statements([shaped(1, Point(113.55, 22.19)), shaped(2, line)], [])
    assert "SELECT AddGeometryColumn('food','cafes','geom',4326,'GEOMETRY',2);" in mixed


@pytest.mark.parametrize(
    ("column", "name"),
    [
        ("Name Zh", "name zh"),
        ("has-dash#", "has_dash_"),
        ("Ünï", "Ünï"),
        (
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ABCDEFGH",
            "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz0123456789a",
        ),
    ],
)
def test_names_columns_as_GDAL_did(column: str, name: str) -> None:
    # GDAL's names: A to Z lower case, each ' - # made _, cut to 63 bytes; other letters stay.
    assert table_sql.launder(column) == name


@pytest.mark.parametrize(
    ("kind", "values"),
    [
        ("VARCHAR", []),
        ("VARCHAR", ["a", None]),
        ("INTEGER", [3, 4]),
        ("INT8", [3, 5_000_000_000]),
        ("FLOAT8", [3, 2.5]),
        ("BOOLEAN", [True, False]),
        ("INTEGER", [True, 2]),
        ("VARCHAR", ["a", 3.5]),
        ("VARCHAR", ["a", False]),
    ],
)
def test_types_a_column_by_the_values_it_holds(kind: str, values: list[Value]) -> None:
    # The types GDAL chose, but for text mixed with other kinds, which stays text rather than
    # JSON, a type the tile query leaves out.
    assert table_sql.type_of(values) == kind


@pytest.mark.parametrize(
    ("kind", "values"),
    [
        ("date", ["2024-05-01", "2023/01/02"]),
        ("time", ["10:30:00", "11:00:00.5"]),
        ("timestamp with time zone", ["2024-05-01T10:30:00Z", "2024-05-01 11:00:00"]),
        ("timestamp with time zone", ["2024-05-01T10:30:00.123+08:00", "2024-05-01T10:30:00-05"]),
        ("timestamp with time zone", ["2024-05-01", "2024-05-01T10:30:00"]),
        ("VARCHAR", ["2024-05-01", "soon"]),
        ("VARCHAR", ["2024-13-45"]),
        ("VARCHAR", ["2024-5-1"]),
        ("VARCHAR", ["2024-05-01", "10:30:00"]),
    ],
)
def test_types_dates_and_times_as_GDAL_did(kind: str, values: list[Value]) -> None:
    # Text that is all dates, times or date-times takes that type, as GDAL typed it; a date
    # that cannot be, or any other text beside the dates, keeps the column as text.
    assert table_sql.type_of(values) == kind


def test_writes_a_date_as_its_text() -> None:
    # A date goes into the script as written, in quotes, and PostgreSQL reads it as the type says.
    assert table_sql.literal("2024-05-01T10:30:00Z", "timestamp with time zone") == "'2024-05-01T10:30:00Z'"


@pytest.mark.parametrize(
    ("value", "kind", "text"),
    [
        (0.30000000000000004, "FLOAT8", "0.30000000000000004"),
        (3, "FLOAT8", "3"),
        (True, "INTEGER", "1"),
        (3.5, "VARCHAR", "'3.5'"),
        (False, "VARCHAR", "'false'"),
        ("a\\b", "VARCHAR", "'a\\b'"),
    ],
)
def test_writes_each_value_for_its_column(value: Value, kind: str, text: str) -> None:
    # Every digit a number holds survives, where GDAL kept fifteen and wrote 0.3.
    assert table_sql.literal(value, kind) == text


def test_refuses_two_columns_that_become_one_name() -> None:
    # Two columns that become one name would make a script the database refuses, so the run stops.
    rows = [shaped(1, Point(113.55, 22.19), Name="A", name="B")]

    assert table_sql.statements(CAFES, rows, ["Name", "name"]) == Err(
        'food.cafes: two columns become "name"; rename one in the source.'
    )


def test_refuses_a_table_with_no_rows() -> None:
    assert table_sql.statements(CAFES, [], []) == Err("food.cafes: no rows to write.")
