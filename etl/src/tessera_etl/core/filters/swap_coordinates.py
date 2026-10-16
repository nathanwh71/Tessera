"""swapCoordinates: a common slip writes latitude where longitude belongs.

A shape outside the area whose swapped copy falls inside it is swapped back; anything else is
left alone.
"""

from dataclasses import replace

import numpy as np
from numpy.typing import NDArray

from ...kernel.pipes import Step
from .. import judge as measure
from ..filter_kind import FilterKind, needs_area
from ..plan import Area, Model, OnFail
from ..rows import Cleaning, Fix, Keep, Row, Verdict
from ..shapes import transform
from ..steps import each_row, filter_step

NAME = "swapCoordinates"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """No settings, not even onFail: this filter only mends rows and never fails one.

    A shape it cannot mend passes on for insideArea to judge.
    """


def _swap(points: NDArray[np.float64]) -> NDArray[np.float64]:
    """Each point's two numbers swapped, so (latitude, longitude) becomes (longitude, latitude)."""
    return points[:, ::-1].copy()


def create(area: Area) -> Step[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file."""
    area_box = measure.box(area)

    def judge(row: Row) -> Verdict:
        shape = row.shape
        if shape is None or shape.is_empty or measure.inside(shape, area_box):
            return Keep(row)
        # shapely makes a new shape, so the row read stays as it was.
        turned = transform(shape, _swap)
        return (
            Fix(replace(row, shape=turned), "swapped longitude and latitude") if measure.inside(turned, area_box) else Keep(row)
        )

    # It never fails a row, so OnFail.DROP never comes into play.
    return filter_step(NAME, OnFail.DROP, each_row(judge))


def _create(settings: Settings, area: Area | None) -> Step[Cleaning]:
    # needs_area refuses the filter when there is no area, so this never meets None.
    assert area is not None
    return create(area)


KIND = FilterKind(
    NAME,
    "a shape outside the area that falls inside it with longitude and latitude swapped is swapped back",
    Settings,
    _create,
    needs_area,
)
