"""The small measures the ***filters***' judges share.

Whether a shape touches the area, whether a value is blank, a value as text, tidy text, and the
distance between two points. Sharing them keeps each judge to a few lines, and makes two
filters measure alike.
"""

import math
import re
import unicodedata
from dataclasses import dataclass

from shapely.geometry import Point
from shapely.geometry import box as shapely_box
from shapely.geometry.base import BaseGeometry

from . import shapes
from .plan import Area
from .rows import Value


@dataclass(frozen=True, slots=True)
class Box:
    """The area's box, in degrees, and the same box as a shape.

    A shape's own box is tested against the numbers first; the shape itself is tested against
    `polygon` only when the boxes meet.
    """

    west: float
    south: float
    east: float
    north: float
    polygon: BaseGeometry


def box(area: Area) -> Box:
    """The box of an area, made once for every row a filter judges."""
    west, south, east, north = area.min_longitude, area.min_latitude, area.max_longitude, area.max_latitude
    polygon = shapely_box(west, south, east, north)
    # 'Preparing' a shape builds an index of its edges once, so each later test runs faster.
    shapes.prepare(polygon)
    return Box(west, south, east, north, polygon)


def inside(shape: BaseGeometry | None, area_box: Box) -> bool:
    """True when any part of the shape touches the box: a road may run out of the area.

    The four comparisons of the shape's own box come first, since they are quick and rule out
    most places far away.
    """
    if shape is None or shape.is_empty:
        return False
    west, south, east, north = shape.bounds
    # Written as 'not meets', so a shape with a number that is not a number fails the test.
    if not (west <= area_box.east and east >= area_box.west and south <= area_box.north and north >= area_box.south):
        return False
    # A point whose box meets the area's box lies in it; only longer shapes need the slow test.
    if isinstance(shape, Point):
        return True
    return shapes.intersects(area_box.polygon, shape)


SPACE = r"[^\S\x1c-\x1f]"
"""One mark of ***white space***: a space, a tab, a line break, or any other mark that shows as a
gap in a label. Python's \\s also matches U+001C to U+001F, four old marks that split records in
a file and show no gap, so the class leaves them out."""

_BLANK = re.compile(f"{SPACE}*")
_SPACES = re.compile(f"{SPACE}+")


def is_blank(value: Value) -> bool:
    """True for no value, or text of spaces alone: such a value shows nothing on the map."""
    return value is None or (isinstance(value, str) and _BLANK.fullmatch(value) is not None)


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


def tidy_text(value: str) -> str | None:
    """One Unicode form, one space between words, no spaces at the ends; None when nothing is left.

    NFC, the ***normal form*** most systems write, makes 'é' typed two ways compare equal.
    """
    return _SPACES.sub(" ", unicodedata.normalize("NFC", value)).strip(" ") or None


def folded(name: str) -> str:
    """A name in lower case, letter by letter, for comparing two names whatever their case.

    Python lowers two capitals by their neighbours or into two letters: a Greek sigma at the
    end of a word, and the Turkish dotted I. Mapping those first keeps each letter one letter,
    lowered alone, so two spellings of a name compare letter for letter.
    """
    return name.replace("İ", "i").replace("Σ", "σ").lower()


_RADIUS = 6_371_008.8


def metres(a: tuple[float, float], b: tuple[float, float]) -> float:
    """The distance in metres between two points given as (longitude, latitude).

    It runs along the Earth's surface, by the ***haversine formula*** on a sphere of the
    Earth's mean radius. Over a few hundred metres it errs by well under a metre.
    """

    def radians(degrees: float) -> float:
        return degrees * math.pi / 180

    d_lat = radians(b[1] - a[1])
    d_lon = radians(b[0] - a[0])
    h = math.pow(math.sin(d_lat / 2), 2) + math.cos(radians(a[1])) * math.cos(radians(b[1])) * math.pow(math.sin(d_lon / 2), 2)
    return 2 * _RADIUS * math.asin(min(1.0, math.sqrt(h)))
