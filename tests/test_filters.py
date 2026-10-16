"""Each ***filter*** alone, built by its create function: what it keeps, fixes and drops.

Each is a pure function, so these tests need no files. (C#: FilterTests.cs)
"""

from helpers import MACAU, clean, log_lines, place, shaped
from shapely.geometry import LineString, Point, Polygon

from tessera_etl.core import judge
from tessera_etl.core.filters import (
    drop_duplicate_ids,
    drop_empty_shapes,
    drop_near_duplicates,
    inside_area,
    repair_shapes,
    require_fields,
    swap_coordinates,
    tidy_text,
)
from tessera_etl.core.plan import OnFail
from tessera_etl.core.rows import Act


def coordinate(row_shape: object) -> tuple[float, float]:
    assert isinstance(row_shape, Point)
    return (row_shape.x, row_shape.y)


def test_drops_a_row_with_no_shape_or_an_empty_one() -> None:
    outcome = clean(drop_empty_shapes.create(), [place(1, 113.55, 22.19), shaped(2, None), shaped(3, Point())])

    assert [row.number for row in outcome.rows] == [1]
    assert log_lines(outcome) == [
        "Row 2: dropped by dropEmptyShapes: it has no shape",
        "Row 3: dropped by dropEmptyShapes: it has no shape",
    ]


def test_stops_at_the_first_bad_row_when_told_to_and_keeps_every_row() -> None:
    outcome = clean(inside_area.create(MACAU, OnFail.STOP), [place(1, 113.55, 22.19), place(2, 0.5, 51.5), place(3, 0.6, 51.6)])

    assert outcome.stop == "insideArea stopped the run at row 2: its shape lies outside the area."
    assert [line.act for line in outcome.log] == [Act.STOPPED]
    assert len(outcome.rows) == 3


def test_swaps_a_point_whose_longitude_and_latitude_changed_places() -> None:
    outcome = clean(swap_coordinates.create(MACAU), [place(1, 22.19, 113.55), place(2, 113.55, 22.19), place(3, 0.5, 51.5)])

    assert coordinate(outcome.rows[0].shape) == (113.55, 22.19)
    # A place already inside, and one outside whichever way round, stay as they were.
    assert coordinate(outcome.rows[1].shape) == (113.55, 22.19)
    assert coordinate(outcome.rows[2].shape) == (0.5, 51.5)
    assert log_lines(outcome) == ["Row 1: fixed by swapCoordinates: swapped longitude and latitude"]


def test_keeps_a_line_that_runs_out_of_the_area() -> None:
    road = shaped(1, LineString([(113.60, 22.15), (113.70, 22.15)]))

    assert len(clean(inside_area.create(MACAU), [road]).rows) == 1


def test_repairs_an_area_whose_edge_crosses_itself() -> None:
    bow_tie = Polygon([(113.54, 22.18), (113.55, 22.19), (113.55, 22.18), (113.54, 22.19), (113.54, 22.18)])
    assert not bow_tie.is_valid

    outcome = clean(repair_shapes.create(), [shaped(1, bow_tie)])

    repaired = outcome.rows[0].shape
    assert repaired is not None
    assert repaired.is_valid
    [line] = log_lines(outcome)
    assert line.startswith("Row 1: fixed by repairShapes: repaired a shape that was invalid (Self-intersection")


def test_tidies_text_into_one_form_with_single_spaces() -> None:
    # 'e' followed by a combining accent is the same letter as 'é', written two ways.
    outcome = clean(tidy_text.create(), [place(1, 113.55, 22.19, name="  Café \t Uno  ", note="   ", floors=3)])

    assert outcome.rows[0].value("name") == "Café Uno"
    # Text that tidies to nothing stays in the row as null, the value of an empty cell.
    assert "note" in outcome.rows[0].values
    assert outcome.rows[0].values["note"] is None
    assert outcome.rows[0].value("floors") == 3
    assert log_lines(outcome) == ["Row 1: fixed by tidyText: tidied name, note"]


def test_tidies_only_the_fields_named() -> None:
    outcome = clean(tidy_text.create(["name"]), [place(1, 113.55, 22.19, name=" a ", code=" b ")])

    assert (outcome.rows[0].value("name"), outcome.rows[0].value("code")) == ("a", " b ")


def test_drops_a_row_missing_a_required_field_or_holding_only_spaces() -> None:
    outcome = clean(
        require_fields.create(["name"]),
        [place(1, 113.55, 22.19, name="A"), place(2, 113.55, 22.19, name="  "), place(3, 113.55, 22.19)],
    )

    assert [row.number for row in outcome.rows] == [1]
    assert all(line.detail == "name is missing" for line in outcome.log)


def test_keeps_the_first_of_two_rows_with_one_id() -> None:
    outcome = clean(
        drop_duplicate_ids.create("id"),
        [place(1, 113.55, 22.19, id=7), place(2, 113.55, 22.19, id=8), place(3, 113.56, 22.20, id=7), place(4, 113.56, 22.20)],
    )

    assert [row.number for row in outcome.rows] == [1, 2, 4]
    assert log_lines(outcome) == ["Row 3: dropped by dropDuplicateIds: it repeats id '7' of row 1"]


def test_drops_a_place_entered_twice_nearby_and_keeps_one_of_the_same_name_far_off() -> None:
    # 0.0001 degrees of latitude is about 11 metres; 0.01 is about 1.1 kilometres.
    outcome = clean(
        drop_near_duplicates.create("name", metres=25),
        [
            place(1, 113.55, 22.19, name="Café Uno"),
            place(2, 113.55, 22.1901, name="café  uno "),
            place(3, 113.55, 22.20, name="Café Uno"),
        ],
    )

    assert [row.number for row in outcome.rows] == [1, 3]
    assert log_lines(outcome) == ["Row 2: dropped by dropNearDuplicates: its name matches row 1, 11 m away"]


def test_measures_a_degree_of_latitude_as_about_111_kilometres() -> None:
    assert 111_150 <= judge.metres((113.55, 22), (113.55, 23)) <= 111_250
