"""Every ***filter*** a ***pipeline file*** may name.

A new filter is one new module beside these, declaring its KIND, and one more line in each of
`AnyKind` and `KINDS`.
"""

from ..filter_kind import FilterKind
from ..plan import Model
from . import (
    drop_duplicate_ids,
    drop_empty_shapes,
    drop_near_duplicates,
    inside_area,
    repair_shapes,
    require_fields,
    swap_coordinates,
    tidy_text,
)

type AnyKind = (
    FilterKind[drop_empty_shapes.Settings]
    | FilterKind[repair_shapes.Settings]
    | FilterKind[swap_coordinates.Settings]
    | FilterKind[inside_area.Settings]
    | FilterKind[tidy_text.Settings]
    | FilterKind[require_fields.Settings]
    | FilterKind[drop_duplicate_ids.Settings]
    | FilterKind[drop_near_duplicates.Settings]
)

KINDS: tuple[AnyKind, ...] = (
    drop_empty_shapes.KIND,
    repair_shapes.KIND,
    swap_coordinates.KIND,
    inside_area.KIND,
    tidy_text.KIND,
    require_fields.KIND,
    drop_duplicate_ids.KIND,
    drop_near_duplicates.KIND,
)
"""The filters in the order the user story lists them, which is also the order an error
message lists them in when a pipeline file names one that does not exist."""


def find(name: str) -> AnyKind | None:
    """The filter a pipeline file names, or None.

    The match is exact, case included, as JSON keys are, so 'insidearea' is refused rather
    than quietly read as 'insideArea'.
    """
    return next((kind for kind in KINDS if kind.name == name), None)


__all__ = ["KINDS", "AnyKind", "FilterKind", "Model", "find"]
