"""swapCoordinates: a common slip writes latitude where longitude belongs.

A shape outside the area whose swapped copy falls inside it is swapped back; anything else is
left alone.
"""

import numpy as np
import pandas as pd
import shapely
from geopandas import GeoDataFrame, GeoSeries

from ...kernel.pipes import Step
from .. import judge as measure
from ..filter_kind import FilterKind, needs_area
from ..plan import Area, Model, OnFail
from ..rows import Cleaning, Judged
from ..steps import filter_step, fixes

NAME = "swapCoordinates"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """No settings, not even onFail: this filter only mends rows and never fails one.

    A shape it cannot mend passes on for insideArea to judge.
    """


def create(area: Area) -> Step[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file."""
    area_box = measure.box(area)

    def judge(frame: GeoDataFrame) -> Judged:
        shapes = frame.geometry.to_numpy()
        outside = ~shapely.is_missing(shapes) & ~shapely.is_empty(shapes) & ~measure.inside(shapes, area_box)
        # shapely hands the points of every shape as one table, x then y; reversing its two
        # columns swaps each point's numbers. shapely makes new shapes, so the table read stays.
        turned = shapely.transform(shapes[outside], lambda points: points[:, ::-1].copy())
        lands = measure.inside(turned, area_box)
        swapped = np.zeros(len(shapes), dtype=bool)
        swapped[np.flatnonzero(outside)[lands]] = True
        mended = shapes.copy()
        mended[swapped] = turned[lands]
        return Judged(
            frame.set_geometry(GeoSeries(mended, index=frame.index, crs=frame.crs)),
            fixes(pd.Series(swapped, index=frame.index).astype(bool), "swapped longitude and latitude"),
        )

    # It never fails a row, so OnFail.DROP never comes into play.
    return filter_step(NAME, OnFail.DROP, judge)


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
