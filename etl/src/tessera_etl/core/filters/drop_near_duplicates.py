"""dropNearDuplicates: two rows with one name, close together, are one place entered twice.

The names are compared after tidying, as tidyText tidies them (one Unicode form, one space
between words, none at the ends), and lowering their case; the shapes' centres must lie within
the given metres; the first row is kept. Neither test alone is enough: a name repeats across a
city, and a position repeats in a busy building.
"""

import numpy as np
import pandas as pd
import shapely
from geopandas import GeoDataFrame, GeoSeries
from numpy.typing import NDArray

from ...kernel.pipes import Step
from .. import judge as measure
from ..filter_kind import FilterKind
from ..plan import Area, Fraction, Model, OnFail, OnFailSetting, Text
from ..rows import Cleaning, Judged, values
from ..steps import failures, filter_step

NAME = "dropNearDuplicates"
"""The name a pipeline file uses; the log and the tests name the filter by it too."""


class Settings(Model):
    """`field` and `metres` have no default, since only the publisher knows which column names
    a place and how close counts as the same place."""

    field: Text
    metres: Fraction
    on_fail: OnFailSetting = OnFail.DROP


def create(field: str, metres: float, on_fail: OnFail = OnFail.DROP) -> Step[Cleaning]:
    """Builds the filter from plain values, so a test can make one without a pipeline file."""

    def judge(frame: GeoDataFrame) -> Judged:
        # A row with no name or no shape passes: requireFields and dropEmptyShapes judge those.
        # A name that tidies to nothing counts as no name.
        texts = pd.Series(values(frame, field), index=frame.index, dtype=object).map(measure.text).dropna()
        names = measure.folded(measure.tidied(texts)).replace("", None).dropna()
        shapes = frame.geometry.to_numpy()
        usable = pd.Series(~shapely.is_missing(shapes) & ~shapely.is_empty(shapes), index=frame.index)
        names = names[usable.loc[names.index]]
        centres = shapely.get_coordinates(shapely.centroid(frame.geometry.loc[names.index].to_numpy()))
        earlier = _near_pairs(centres, names.to_numpy(), metres)
        # A row fails when an earlier row it is near is kept, so each row waits on those before
        # it: one pass in row order settles them all. Each name may have several kept places,
        # since two cafés of one chain may share a name far apart, and both are real.
        failed: dict[int, str] = {}
        rows = names.index.to_numpy()
        for later, candidates in earlier.items():
            match = next(((before, distance) for before, distance in candidates if int(rows[before]) not in failed), None)
            if match is not None:
                failed[int(rows[later])] = f"its {field} matches row {int(rows[match[0]])}, {match[1]:.0f} m away"
        reason = pd.Series(failed, dtype=object).reindex(frame.index)
        return Judged(frame, failures(reason.notna(), reason))

    return filter_step(NAME, on_fail, judge)


def _near_pairs(centres: NDArray[np.float64], names: NDArray[np.object_], metres: float) -> dict[int, list[tuple[int, float]]]:
    """For each place, the earlier places of the same name within `metres`, earliest first.

    The table's ***spatial index***, a tree of boxes, finds the places whose centres fall in a
    box around each centre, so only those few are measured. The box spans the metres in degrees of
    latitude, and the widest span of longitude a place that close could take; a box that
    crosses the 180th meridian, or reaches a pole, spans every longitude.
    """
    if len(centres) == 0:
        return {}
    lon, lat = centres[:, 0], centres[:, 1]
    # A millionth more than the distance, so rounding in the last digit never shuts a place out.
    reach = np.degrees(metres / measure.RADIUS) * 1.000001
    # Two places within the distance differ in longitude by at most this angle, by the
    # haversine formula, at the latitudes they can share.
    widest = np.cos(np.radians(lat)) * np.cos(np.radians(np.minimum(np.abs(lat) + reach, 90.0)))
    with np.errstate(divide="ignore", invalid="ignore"):
        spread = np.sin(np.radians(reach) / 2) / np.sqrt(widest)
    across = np.degrees(2 * np.arcsin(np.minimum(spread, 1.0)))
    every = ~(spread < 1.0) | (lon - across < -180) | (lon + across > 180)
    west, east = np.where(every, -np.inf, lon - across), np.where(every, np.inf, lon + across)
    boxes = shapely.box(west, lat - reach, east, lat + reach)
    later, before = GeoSeries(measure.points(centres)).sindex.query(boxes, predicate="intersects")
    same = (before < later) & (names[before] == names[later])
    later, before = later[same], before[same]
    distances = measure.metres(centres[later], centres[before])
    close = distances <= metres
    found: dict[int, list[tuple[int, float]]] = {}
    for one, other, distance in sorted(zip(later[close], before[close], distances[close], strict=True)):
        found.setdefault(int(one), []).append((int(other), float(distance)))
    return found


def _check(settings: Settings, area: Area | None) -> tuple[str, ...]:
    problems: list[str] = []
    if measure.is_blank(settings.field):
        problems.append('needs a field, such as "name".')
    if not settings.metres > 0:
        problems.append("needs metres above 0, such as 25.")
    return tuple(problems)


def _create(settings: Settings, area: Area | None) -> Step[Cleaning]:
    return create(settings.field, settings.metres, settings.on_fail)


KIND = FilterKind(NAME, "a row whose `field` matches an earlier row's, within `metres` of it, fails", Settings, _create, _check)
