"""Each ***filter*** alone, built by its create function: what it keeps, fixes and drops.

Each is a pure function, so these tests need no files.
"""

import numpy as np
import pytest
from helpers import MACAU, clean, kept, log_lines, place, shape, shaped, value
from shapely.geometry import LineString, Point, Polygon

from tessera_etl.core import filters, judge
from tessera_etl.core.filters import (
    AnyKind,
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
from tessera_etl.kernel.result import Ok


def coordinate(found: object) -> tuple[float, float]:
    assert isinstance(found, Point)
    return (found.x, found.y)


def test_drops_a_row_with_no_shape_or_an_empty_one() -> None:
    outcome = clean(drop_empty_shapes.create(), [place(1, 113.55, 22.19), shaped(2, None), shaped(3, Point())])

    assert kept(outcome) == [1]
    assert log_lines(outcome) == [
        "Row 2: dropped by dropEmptyShapes: it has no shape",
        "Row 3: dropped by dropEmptyShapes: it has no shape",
    ]


def test_stops_at_the_first_bad_row_when_told_to_and_keeps_every_row() -> None:
    outcome = clean(inside_area.create(MACAU, OnFail.STOP), [place(1, 113.55, 22.19), place(2, 0.5, 51.5), place(3, 0.6, 51.6)])

    assert outcome.stop == "insideArea stopped the run at row 2: its shape lies outside the area."
    assert [line.act for line in outcome.log] == [Act.STOPPED]
    assert len(kept(outcome)) == 3


def test_swaps_a_point_whose_longitude_and_latitude_changed_places() -> None:
    outcome = clean(swap_coordinates.create(MACAU), [place(1, 22.19, 113.55), place(2, 113.55, 22.19), place(3, 0.5, 51.5)])

    assert coordinate(shape(outcome, 1)) == (113.55, 22.19)
    # A place already inside, and one outside whichever way round, stay as they were.
    assert coordinate(shape(outcome, 2)) == (113.55, 22.19)
    assert coordinate(shape(outcome, 3)) == (0.5, 51.5)
    assert log_lines(outcome) == ["Row 1: fixed by swapCoordinates: swapped longitude and latitude"]


def test_keeps_a_line_that_runs_out_of_the_area() -> None:
    road = shaped(1, LineString([(113.60, 22.15), (113.70, 22.15)]))

    assert kept(clean(inside_area.create(MACAU), [road])) == [1]


def test_repairs_an_area_whose_edge_crosses_itself() -> None:
    bow_tie = Polygon([(113.54, 22.18), (113.55, 22.19), (113.55, 22.18), (113.54, 22.19), (113.54, 22.18)])
    assert not bow_tie.is_valid

    outcome = clean(repair_shapes.create(), [shaped(1, bow_tie)])

    repaired = shape(outcome, 1)
    assert repaired is not None
    assert repaired.is_valid
    [line] = log_lines(outcome)
    assert line.startswith("Row 1: fixed by repairShapes: repaired a shape that was invalid (Self-intersection")


def test_tidies_text_into_one_form_with_single_spaces() -> None:
    # 'e' followed by a combining accent is the same letter as 'é', written two ways.
    outcome = clean(tidy_text.create(), [place(1, 113.55, 22.19, name="  Café \t Uno  ", note="   ", floors=3)])

    assert value(outcome, 1, "name") == "Café Uno"
    # Text that tidies to nothing stays in the row as null, the value of an empty cell.
    assert "note" in outcome.frame.columns
    assert value(outcome, 1, "note") is None
    assert value(outcome, 1, "floors") == 3
    assert log_lines(outcome) == ["Row 1: fixed by tidyText: tidied name, note"]


def test_tidies_only_the_fields_named() -> None:
    outcome = clean(tidy_text.create(["name"]), [place(1, 113.55, 22.19, name=" a ", code=" b ")])

    assert (value(outcome, 1, "name"), value(outcome, 1, "code")) == ("a", " b ")


def test_drops_a_row_missing_a_required_field_or_holding_only_spaces() -> None:
    outcome = clean(
        require_fields.create(["name"]),
        [place(1, 113.55, 22.19, name="A"), place(2, 113.55, 22.19, name="  "), place(3, 113.55, 22.19)],
    )

    assert kept(outcome) == [1]
    assert all(line.detail == "name is missing" for line in outcome.log)


def test_keeps_the_first_of_two_rows_with_one_id() -> None:
    outcome = clean(
        drop_duplicate_ids.create("id"),
        [place(1, 113.55, 22.19, id=7), place(2, 113.55, 22.19, id=8), place(3, 113.56, 22.20, id=7), place(4, 113.56, 22.20)],
    )

    assert kept(outcome) == [1, 2, 4]
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

    assert kept(outcome) == [1, 3]
    assert log_lines(outcome) == ["Row 2: dropped by dropNearDuplicates: its name matches row 1, 11 m away"]


def test_measures_a_degree_of_latitude_as_about_111_kilometres() -> None:
    assert 111_150 <= judge.metres(np.array([[113.55, 22.0]]), np.array([[113.55, 23.0]]))[0] <= 111_250


# Settings that suit every filter: each takes the ones its model names.
SETTINGS: dict[str, object] = {"field": "id", "fields": ["name"], "metres": 25}


@pytest.mark.parametrize("kind", filters.KINDS, ids=[kind.name for kind in filters.KINDS])
def test_passes_clean_places_as_they_are(kind: AnyKind) -> None:
    names = {info.alias or name for name, info in kind.settings.model_fields.items()}
    built = kind.build({key: value for key, value in SETTINGS.items() if key in names}, MACAU)
    assert isinstance(built, Ok), built
    places = [place(1, 113.55, 22.19, id="cafes-001", name="Café Uno"), place(2, 113.56, 22.20, id="cafes-002", name="Café Dos")]

    outcome = clean(built.value, places)

    assert kept(outcome) == [1, 2]
    assert outcome.log == ()
    assert [value(outcome, row, "name") for row in (1, 2)] == ["Café Uno", "Café Dos"]
