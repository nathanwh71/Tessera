"""NetTopologySuite's GeometryFixer, rebuilt on shapely, so a repaired shape matches the C# tool's.

The C# tool repaired invalid shapes with NetTopologySuite (NTS), the .NET geometry library.
GEOS, the library behind shapely, has a port of the same fixer, but it differs in two places
that change the script: GEOS unites the parts of a many-part area even when only one part is
left, which can reorder its corners, and it returns one area where NTS keeps a set of one. This
module follows NTS 2.6's code step by step (Geometries/Utilities/GeometryFixer.cs), and leaves
each single ring to GEOS, whose repair of one ring matches NTS's.

A ***ring*** is a closed line, the edge of an area; a ***hole*** is a ring inside an area that
cuts a piece out of it.
"""

import math
from collections.abc import Sequence
from typing import cast

from shapely.geometry import (
    GeometryCollection,
    LinearRing,
    LineString,
    MultiLineString,
    MultiPoint,
    MultiPolygon,
    Point,
    Polygon,
)
from shapely.geometry.base import BaseGeometry

from .shapes import difference, intersects, is_many, make_valid_ring, parts, union


def fix(shape: BaseGeometry) -> BaseGeometry:
    """A valid shape as close to the given one as NTS's GeometryFixer makes it.

    Parts that collapse to lines or points are dropped, and many-part shapes stay many-part,
    as NTS's defaults have it.
    """
    if _parts(shape) == 0:
        return shape
    match shape:
        case Point():
            return _point(shape) or Point()
        case LinearRing():
            return _ring_element(shape) or LinearRing()
        case LineString():
            return _line_element(shape) or LineString()
        case Polygon():
            return _polygon_element(shape) or Polygon()
        case MultiPoint():
            points = [fixed for part in parts(shape) if isinstance(part, Point) and (fixed := _point(part)) is not None]
            return MultiPoint(points)
        case MultiLineString():
            return _multi_line(shape)
        case MultiPolygon():
            return _multi_polygon(shape)
        case GeometryCollection():
            return GeometryCollection([fix(part) for part in parts(shape)])
        case _:
            return shape


def _parts(shape: BaseGeometry) -> int:
    """How many parts NTS counts: a shape with none, such as an empty collection, is copied."""
    if is_many(shape):
        return len(parts(shape))
    return 1


def _point(point: Point) -> Point | None:
    if point.is_empty or not all(math.isfinite(value) for value in (point.x, point.y)):
        return None
    return point


def _clean(coordinates: Sequence[tuple[float, ...]]) -> list[tuple[float, ...]]:
    """The coordinates without repeated or invalid points, as NTS's CoordinateArrays does."""
    kept: list[tuple[float, ...]] = []
    for coordinate in coordinates:
        if not all(math.isfinite(value) for value in coordinate[:2]):
            continue
        if kept and kept[-1][:2] == coordinate[:2]:
            continue
        kept.append(coordinate)
    return kept


def _line_element(line: LineString) -> LineString | None:
    if line.is_empty:
        return None
    points = _clean(list(line.coords))
    return LineString(points) if len(points) > 1 else None


def _ring_element(ring: LinearRing) -> BaseGeometry | None:
    if ring.is_empty:
        return None
    points = _clean(list(ring.coords))
    if len(points) <= 3:
        return None
    fixed = LinearRing(points)
    return fixed if fixed.is_valid else LineString(points)


def _multi_line(shape: MultiLineString) -> BaseGeometry:
    fixed = [line for part in parts(shape) if isinstance(part, LineString) and (line := _line_element(part)) is not None]
    return MultiLineString(fixed)


def _fix_ring(ring: LinearRing) -> BaseGeometry:
    """An area from one ring, rebuilt by buffering it by nothing in both directions.

    GEOS's repair of a one-ring area does exactly this, and its corners come out as NTS's do.
    """
    return make_valid_ring(Polygon(ring))


def _polygon_element(polygon: Polygon) -> BaseGeometry | None:
    shell = _fix_ring(polygon.exterior)
    if shell.is_empty:
        return None
    if not polygon.interiors:
        return shell
    holes_fixed = [_fix_ring(hole) for hole in polygon.interiors]
    holes = [hole for hole in holes_fixed if intersects(shell, hole)]
    shells = [hole for hole in holes_fixed if not intersects(shell, hole)]
    with_holes = shell if not holes else difference(shell, _union(holes))
    if not shells:
        return with_holes
    return _union([*shells, with_holes])


def _union(shapes: Sequence[BaseGeometry]) -> BaseGeometry:
    """The union of areas as NTS's OverlayNGRobust.Union makes it.

    NTS unites areas by ***cascaded union***: it sorts them into a tree by where they lie, then
    unites neighbours pair by pair up the tree. The order of the pairs fixes the order of the
    parts and corners in the result, so the tree is built here as NTS builds it.
    """
    if not shapes:
        return Polygon()
    if len(shapes) == 1:
        return shapes[0]
    polygons = [polygon for shape in shapes for polygon in _polygons(shape)]
    if not polygons:
        return Polygon()
    root = _higher([_Leaf(polygon, polygon.bounds) for polygon in polygons])
    return _union_tree(_items(root)) or Polygon()


def _polygons(shape: BaseGeometry) -> list[Polygon]:
    if isinstance(shape, Polygon):
        return [] if shape.is_empty else [shape]
    if is_many(shape) and not isinstance(shape, (MultiPoint, MultiLineString)):
        return [polygon for part in parts(shape) for polygon in _polygons(part)]
    return []


type _Bounds = tuple[float, float, float, float]
_CAPACITY = 4


class _Leaf:
    """One area in the tree, with its box."""

    def __init__(self, shape: Polygon, bounds: _Bounds) -> None:
        self.shape = shape
        self.bounds = bounds


class _Node:
    """A node of the tree: up to four children, and the box around them all."""

    def __init__(self, children: list["_Leaf | _Node"]) -> None:
        self.children = children
        self.bounds: _Bounds = (
            min(child.bounds[0] for child in children),
            min(child.bounds[1] for child in children),
            max(child.bounds[2] for child in children),
            max(child.bounds[3] for child in children),
        )


def _centre_x(item: "_Leaf | _Node") -> float:
    return (item.bounds[0] + item.bounds[2]) / 2


def _centre_y(item: "_Leaf | _Node") -> float:
    return (item.bounds[1] + item.bounds[3]) / 2


def _higher(level: list["_Leaf | _Node"]) -> _Node:
    """Builds the tree's levels upwards by Sort-Tile-Recursive packing, until one node is left.

    Each level is sorted by the centres' x, cut into vertical slices, and each slice sorted by
    the centres' y and packed into nodes of four.
    """
    parents: list[_Leaf | _Node] = []
    least_leaves = math.ceil(len(level) / _CAPACITY)
    by_x = sorted(level, key=_centre_x)
    slices = math.ceil(math.sqrt(least_leaves))
    size = math.ceil(len(by_x) / slices)
    for start in range(0, slices * size, size):
        nodes: list[list[_Leaf | _Node]] = [[]]
        for item in sorted(by_x[start : start + size], key=_centre_y):
            if len(nodes[-1]) == _CAPACITY:
                nodes.append([])
            nodes[-1].append(item)
        parents.extend(_Node(children) for children in nodes if children)
    if len(parents) == 1:
        only = parents[0]
        assert isinstance(only, _Node)
        return only
    return _higher(parents)


def _items(node: _Node) -> list[object]:
    """The tree as nested lists of areas, as NTS's ItemsTree gives it."""
    return [_items(child) if isinstance(child, _Node) else child.shape for child in node.children]


def _union_tree(tree: list[object]) -> BaseGeometry | None:
    shapes: list[BaseGeometry | None] = []
    for item in tree:
        if isinstance(item, list):
            shapes.append(_union_tree(cast(list[object], item)))
        else:
            shapes.append(cast(BaseGeometry, item))
    return _binary(shapes, 0, len(shapes))


def _binary(shapes: list[BaseGeometry | None], start: int, end: int) -> BaseGeometry | None:
    def at(index: int) -> BaseGeometry | None:
        return shapes[index] if index < len(shapes) else None

    if end - start <= 1:
        return _safe(at(start), None)
    if end - start == 2:
        return _safe(at(start), at(start + 1))
    middle = (end + start) // 2
    return _safe(_binary(shapes, start, middle), _binary(shapes, middle, end))


def _safe(first: BaseGeometry | None, second: BaseGeometry | None) -> BaseGeometry | None:
    if first is None:
        return second
    if second is None:
        return first
    united = union(first, second)
    if isinstance(united, (Polygon, MultiPolygon)):
        return united
    polygons = _polygons(united)
    return polygons[0] if len(polygons) == 1 else MultiPolygon(polygons)


def _multi_polygon(shape: MultiPolygon) -> BaseGeometry:
    fixed = [
        part
        for polygon in parts(shape)
        if isinstance(polygon, Polygon) and (part := _polygon_element(polygon)) is not None and not part.is_empty
    ]
    if not fixed:
        return MultiPolygon()
    result = _union(fixed)
    return MultiPolygon([result]) if isinstance(result, Polygon) else result
