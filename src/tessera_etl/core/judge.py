"""The small measures the ***filters***' judges share.

Whether a shape touches the area, whether a value is blank, a value as text, tidy text, and the
distance between two points. Sharing them keeps each judge to a few lines, and makes two
filters measure alike.
"""

import math
import re
import unicodedata
from dataclasses import dataclass

from shapely.geometry import box as shapely_box
from shapely.geometry.base import BaseGeometry

from . import dotnet
from .plan import Area
from .rows import Value


@dataclass(frozen=True, slots=True)
class Box:
    """The area's box, in degrees; a shape's own box is tested against it before the shape."""

    west: float
    south: float
    east: float
    north: float

    def polygon(self) -> BaseGeometry:
        """The box as a shape, for the slower test of the shape itself."""
        return shapely_box(self.west, self.south, self.east, self.north)


def box(area: Area) -> Box:
    """The box of an area."""
    return Box(area.min_longitude, area.min_latitude, area.max_longitude, area.max_latitude)


def inside(shape: BaseGeometry | None, area_box: Box) -> bool:
    """True when any part of the shape touches the box: a road may run out of the area.

    The four comparisons of the shape's own box come first, since they are quick and rule out
    most places far away.
    """
    if shape is None or shape.is_empty:
        return False
    west, south, east, north = shape.bounds
    if west > area_box.east or east < area_box.west or south > area_box.north or north < area_box.south:
        return False
    return bool(shape.intersects(area_box.polygon()))


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


def tidy_text(value: str) -> str | None:
    """One Unicode form, one space between words, no spaces at the ends; None when nothing is left.

    NFC, the ***normal form*** most systems write, makes 'é' typed two ways compare equal.
    """
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
