"""The few shapely functions the pipeline calls, with plain types.

shapely, the Python wrapper of the GEOS geometry library, declares many of its functions with
loose types, which a strict type checker cannot follow. Each function here passes straight
through to shapely, so the rest of the code sees one clear signature.
"""

from collections.abc import Callable, Sequence
from typing import Any, cast

import numpy as np
import shapely
from numpy.typing import NDArray
from shapely.geometry import GeometryCollection
from shapely.geometry.base import BaseGeometry, BaseMultipartGeometry

type Points = NDArray[np.float64]
"""A table of points, one row each, with x in the first column and y in the second."""

_shapely: Any = shapely


def is_many(shape: BaseGeometry) -> bool:
    """True for a many-part shape or a collection, whose parts `parts` lists."""
    return isinstance(shape, BaseMultipartGeometry)


def parts(shape: BaseGeometry) -> list[BaseGeometry]:
    """The parts of a many-part shape or a collection, in order."""
    collection = cast("GeometryCollection[BaseGeometry]", shape)
    return list(collection.geoms)


def transform(shape: BaseGeometry, change: Callable[[Points], Points]) -> BaseGeometry:
    """The shape with every point changed by `change`, which takes and gives a table of points."""
    return cast(BaseGeometry, _shapely.transform(shape, change))


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


def from_geojson(text: str) -> BaseGeometry:
    """A shape from a GeoJSON geometry's text."""
    return cast(BaseGeometry, _shapely.from_geojson(text))


def ewkb_hex(shape: BaseGeometry, srid: int) -> str:
    """The shape as PostGIS's extended well-known binary, least significant byte first, in hex."""
    with_srid = _shapely.set_srid(shape, srid)
    return str(_shapely.to_wkb(with_srid, hex=True, output_dimension=2, byte_order=1, include_srid=True))


def type_id(shape: BaseGeometry) -> int:
    """GEOS's number for the shape's type: 0 to 7 for the types GeoJSON holds, more for curves."""
    return int(_shapely.get_type_id(shape))


def invalid_reason(shape: BaseGeometry) -> str:
    """Why GEOS finds the shape invalid, such as 'Self-intersection[113.545 22.185]'."""
    return str(_shapely.is_valid_reason(shape))


def make_valid_ring(shape: BaseGeometry) -> BaseGeometry:
    """GEOS's 'structure' repair, without parts that collapse to lines or points."""
    return cast(BaseGeometry, _shapely.make_valid(shape, method="structure", keep_collapsed=False))


def union(first: BaseGeometry, second: BaseGeometry) -> BaseGeometry:
    """The union of two shapes by GEOS's robust overlay, with no shortcut for distant shapes.

    A grid size of 0 asks GEOS for its overlay at full precision. Without it, GEOS sets two
    shapes whose boxes do not meet side by side untouched, where NetTopologySuite rebuilds
    their corners, so the order of the corners would differ.
    """
    return cast(BaseGeometry, _shapely.union(first, second, grid_size=0))


def difference(first: BaseGeometry, second: BaseGeometry) -> BaseGeometry:
    """The part of `first` outside `second`."""
    return cast(BaseGeometry, _shapely.difference(first, second))


def intersects(first: BaseGeometry, second: BaseGeometry) -> bool:
    """True when the two shapes share any point."""
    return bool(_shapely.intersects(first, second))


GEOSException: type[Exception] = cast(type[Exception], shapely.errors.GEOSException)
"""The error GEOS raises for a shape it cannot read or work on."""
