"""The few shapely functions the pipeline calls, with plain types.

shapely, the Python wrapper of the GEOS geometry library, declares some of its functions with
loose types, which a strict type checker cannot follow. Each function here passes straight
through to shapely, so the rest of the code sees one clear signature.
"""

from collections.abc import Callable, Sequence
from typing import Any, cast

import numpy as np
import shapely
from numpy.typing import NDArray
from shapely.geometry.base import BaseGeometry

type Points = NDArray[np.float64]
"""A table of points, one row each, with x in the first column and y in the second."""

_shapely: Any = shapely

# GEOS's numbers for the types GeoJSON holds: point, line, ring, area, their many-part forms,
# and collections. Any other type, such as a circular arc, needs GDAL to straighten it.
_PLAIN_TYPES = frozenset(range(8))
_MANY = frozenset(range(4, 8))


def bounds(shapes: NDArray[np.object_]) -> NDArray[np.float64]:
    """Each shape's box as west, south, east, north; not a number for no shape or an empty one."""
    return cast(NDArray[np.float64], _shapely.bounds(shapes))


def type_ids(shapes: NDArray[np.object_]) -> NDArray[np.int32]:
    """GEOS's number for each shape's type: 0 for a point, -1 for no shape."""
    return cast(NDArray[np.int32], _shapely.get_type_id(shapes))


def points(coordinates: Points) -> NDArray[np.object_]:
    """A point for each row of x and y."""
    return cast(NDArray[np.object_], _shapely.points(coordinates))


def plain(shape: BaseGeometry) -> bool:
    """True when the shape, and every part of it, is a type GeoJSON can hold as it is."""
    kind = int(_shapely.get_type_id(shape))
    if kind not in _PLAIN_TYPES:
        return False
    parts = cast(list[BaseGeometry], list(_shapely.get_parts(shape))) if kind in _MANY else []
    return all(plain(part) for part in parts)


def transform_all(shapes: Sequence[BaseGeometry | None], change: Callable[[Points], Points]) -> list[BaseGeometry | None]:
    """Each shape with every point changed, in one call for the whole list."""
    found = cast(NDArray[np.object_], _shapely.transform(np.asarray(shapes, dtype=object), change))
    return [cast(BaseGeometry | None, shape) for shape in found]


def force_2d(shapes: Sequence[BaseGeometry | None]) -> list[BaseGeometry | None]:
    """Each shape with heights and measures dropped."""
    found = cast(NDArray[np.object_], _shapely.force_2d(np.asarray(shapes, dtype=object)))
    return [cast(BaseGeometry | None, shape) for shape in found]


def from_wkb(blobs: Sequence[bytes | None]) -> list[BaseGeometry | None]:
    """Shapes from ***well-known binary***, the standard byte form of a shape."""
    array = np.empty(len(blobs), dtype=object)
    array[:] = list(blobs)
    found = cast(NDArray[np.object_], _shapely.from_wkb(array))
    return [cast(BaseGeometry | None, shape) for shape in found]


def ewkb_hex(shapes: Sequence[BaseGeometry], srid: int) -> list[str]:
    """Each shape as PostGIS's extended well-known binary, least significant byte first, in hex."""
    array = np.empty(len(shapes), dtype=object)
    array[:] = list(shapes)
    with_srid = _shapely.set_srid(array, srid)
    return [str(text) for text in _shapely.to_wkb(with_srid, hex=True, output_dimension=2, byte_order=1, include_srid=True)]


GEOSException: type[Exception] = cast(type[Exception], shapely.errors.GEOSException)
"""The error GEOS raises for a shape it cannot read or work on."""
