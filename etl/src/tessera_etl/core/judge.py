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

from . import dotnet, shapes
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


def is_blank(value: Value) -> bool:
    """True for no value, or text of spaces alone: such a value shows nothing on the map."""
    return value is None or (isinstance(value, str) and dotnet.is_null_or_white_space(value))


def text(value: Value) -> str | None:
    """A value as text, for comparing ids and names; numbers keep their exact digits.

    None for no value, or text of spaces alone. True and false read as .NET wrote them,
    'True' and 'False'.
    """
    match value:
        case None:
            return None
        case str():
            return None if dotnet.is_null_or_white_space(value) else value
        case bool():
            return "True" if value else "False"
        case int():
            return str(value)
        case float():
            return dotnet.number_text(value)


# Any run of white space, tabs and line breaks included: each shows as a gap in a label. The
# class lists .NET's white space, since Python's \s also matches four old control marks.
_SPACES = re.compile("[" + re.escape("".join(sorted(dotnet.WHITE_SPACE))) + "]+")


# Text that needs tidying: white space other than a plain space, two marks of white space in a
# row, or white space at either end.
_UNTIDY = re.compile("[" + re.escape("".join(sorted(dotnet.WHITE_SPACE - {" "}))) + "]| {2}|^ | $")


def tidy_text(value: str) -> str | None:
    """One Unicode form, one space between words, no spaces at the ends; None when nothing is left.

    NFC, the ***normal form*** most systems write, makes 'é' typed two ways compare equal.
    """
    # Most text is tidy already; two quick tests find it, and the work below runs only on the rest.
    if value and unicodedata.is_normalized("NFC", value) and _UNTIDY.search(value) is None:
        return value
    clean = dotnet.trim(_SPACES.sub(" ", unicodedata.normalize("NFC", value)))
    return clean or None


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
