"""The measures the ***filters***' judges share, each over a whole column at once.

Whether shapes touch the area, which values are blank, values as text, tidy text, and distances
along the Earth. Sharing them keeps each judge to a few lines, and makes two filters measure
alike.
"""

import re
import unicodedata
from dataclasses import dataclass
from typing import Any

import numpy as np
import pandas as pd
import shapely
from numpy.typing import NDArray
from shapely.geometry import box as shapely_box
from shapely.geometry.base import BaseGeometry

from . import shapes as shape_kit
from .plan import Area
from .rows import Value

type Shapes = NDArray[np.object_]
"""A column of shapes as a ***NumPy array***, the form shapely's functions take a whole column in."""


def points(coordinates: NDArray[np.float64]) -> Shapes:
    """A point for each row of (longitude, latitude)."""
    return shape_kit.points(coordinates)


@dataclass(frozen=True, slots=True)
class Box:
    """The area's box, in degrees, and the same box as a shape."""

    west: float
    south: float
    east: float
    north: float
    polygon: BaseGeometry


def box(area: Area) -> Box:
    """The box of an area, made once for every table a filter judges."""
    west, south, east, north = area.min_longitude, area.min_latitude, area.max_longitude, area.max_latitude
    polygon = shapely_box(west, south, east, north)
    # 'Preparing' a shape builds an index of its edges once, so each later test runs faster.
    shapely.prepare(polygon)
    return Box(west, south, east, north, polygon)


def inside(shapes: Shapes, area_box: Box) -> NDArray[np.bool_]:
    """True for each shape that touches the box with any part: a road may run out of the area.

    No shape, or an empty one, lies outside. Each shape's own box is compared first, since
    four comparisons are quick and rule out most places far away; a point whose box meets the
    area's box lies in it, so only longer shapes need the slow test.
    """
    west, south, east, north = shape_kit.bounds(shapes).T
    # A missing shape's box is not a number, and every comparison with one is false.
    meets = (west <= area_box.east) & (east >= area_box.west) & (south <= area_box.north) & (north >= area_box.south)
    points = shape_kit.type_ids(shapes) == 0
    return meets & (points | shapely.intersects(area_box.polygon, shapes))


SPACE = r"[^\S\x1c-\x1f]"
"""One mark of ***white space***: a space, a tab, a line break, or any other mark that shows as a
gap in a label. Python's \\s also matches U+001C to U+001F, four old marks that split records in
a file and show no gap, so the class leaves them out."""

_BLANK = re.compile(f"{SPACE}*")
_SPACES = re.compile(f"{SPACE}+")


def is_blank(value: Value) -> bool:
    """True for no value, or text of spaces alone: such a value shows nothing on the map."""
    return value is None or (isinstance(value, str) and _BLANK.fullmatch(value) is not None)


def is_text(column: "pd.Series[Any]") -> "pd.Series[bool]":
    """True for each value that is text; pandas' .str methods make anything else 'not a number'."""
    return column.map(lambda value: isinstance(value, str)).astype(bool)


def blank(column: "pd.Series[Any]") -> "pd.Series[bool]":
    """True for each value that is null, or text of spaces alone."""
    text = column[is_text(column)].astype(str)
    return column.isna() | text.str.fullmatch(_BLANK.pattern).reindex(column.index, fill_value=False).astype(bool)


def number_text(value: float) -> str:
    """A number as text: its shortest digits that read back as the same number.

    A whole number below 10 to the 16th drops its '.0', so 3.0 reads as 3, as the integer
    3 does; beyond that, Python writes the number with an exponent, such as 1e+16.
    """
    return str(int(value)) if value.is_integer() and abs(value) < 1e16 else repr(value)


def text(value: Value) -> str | None:
    """A value as text, for comparing ids and names; numbers keep their exact digits.

    None for no value, or text of spaces alone; true and false read 'True' and 'False'.
    """
    match value:
        case None:
            return None
        case str():
            return None if is_blank(value) else value
        case bool() | int():
            return str(value)
        case float():
            return number_text(value)


def tidied(column: "pd.Series[str]") -> "pd.Series[str]":
    """Each text in one Unicode form, one space between words, no spaces at the ends.

    NFC, the ***normal form*** most systems write, makes 'é' typed two ways compare equal. Text
    that tidies to nothing comes out empty.
    """
    return column.str.normalize("NFC").str.replace(_SPACES.pattern, " ", regex=True).str.strip(" ")


def tidy_text(value: str) -> str | None:
    """One text tidied, as `tidied` tidies a column; None when nothing is left."""
    return _SPACES.sub(" ", unicodedata.normalize("NFC", value)).strip(" ") or None


def folded(column: "pd.Series[str]") -> "pd.Series[str]":
    """Each name in lower case, letter by letter, for comparing two names whatever their case.

    Python lowers two capitals by their neighbours or into two letters: a Greek sigma at the
    end of a word, and the Turkish dotted I. Mapping those first keeps each letter one letter,
    lowered alone, so two spellings of a name compare letter for letter.
    """
    return column.str.replace("İ", "i", regex=False).str.replace("Σ", "σ", regex=False).str.lower()


RADIUS = 6_371_008.8
"""The Earth's mean radius, in metres."""


def metres(a: NDArray[np.float64], b: NDArray[np.float64]) -> NDArray[np.float64]:
    """The distance in metres between pairs of points, each row of `a` and `b` a (longitude, latitude).

    It runs along the Earth's surface, by the ***haversine formula*** on a sphere of the
    Earth's mean radius. Over a few hundred metres it errs by well under a metre.
    """
    lon_a, lat_a, lon_b, lat_b = (degrees * np.pi / 180 for degrees in (a[:, 0], a[:, 1], b[:, 0], b[:, 1]))
    h = np.sin((lat_b - lat_a) / 2) ** 2 + np.cos(lat_a) * np.cos(lat_b) * np.sin((lon_b - lon_a) / 2) ** 2
    return 2 * RADIUS * np.arcsin(np.minimum(1.0, np.sqrt(h)))
