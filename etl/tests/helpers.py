"""Places, areas and runners the tests share, so every test builds its tables the same way."""

from collections.abc import Sequence
from dataclasses import dataclass, field

from geopandas import GeoDataFrame
from shapely.geometry import Point
from shapely.geometry.base import BaseGeometry

from tessera_etl.core.plan import Area, TablePlan
from tessera_etl.core.rows import Cleaning, Value, numbers, shapes, table_of, values
from tessera_etl.kernel.pipes import Step

MACAU = Area.model_validate({"minLongitude": 113.52, "minLatitude": 22.10, "maxLongitude": 113.61, "maxLatitude": 22.22})
"""The area: Macau's box, as tessera.json holds it."""

TABLE = TablePlan.model_validate({"table": "food.cafes"})
"""The table every test cleans; the filters ignore it, but the stop rules and the log name it."""


@dataclass(frozen=True, slots=True)
class Place:
    """One row for a test: its number in the source, its shape, and its values."""

    number: int
    shape: BaseGeometry | None
    values: dict[str, Value] = field(default_factory=dict[str, Value])


def table(name: str = "food.cafes", **settings: object) -> TablePlan:
    """A table plan as a pipeline file would give it, with its settings in camelCase."""
    return TablePlan.model_validate({"table": name, **settings})


def place(number: int, longitude: float, latitude: float, **found: Value) -> Place:
    """A point place with the given values."""
    return Place(number, Point(longitude, latitude), found)


def shaped(number: int, shape: BaseGeometry | None, **found: Value) -> Place:
    """A place with any shape, or none."""
    return Place(number, shape, found)


def frame_of(places: Sequence[Place]) -> GeoDataFrame:
    """The table of the places; a place that lacks a column holds null there."""
    names = dict.fromkeys(name for one in places for name in one.values)
    return table_of(
        [one.shape for one in places],
        {name: [one.values.get(name) for one in places] for name in names},
        [one.number for one in places],
    )


def clean(steps: Step[Cleaning], places: Sequence[Place], plan: TablePlan = TABLE) -> Cleaning:
    """Runs one step, or a chain of them, on the places, as the pipeline runs a table's chain."""
    return steps(Cleaning.start(plan, frame_of(places)))


def kept(cleaning: Cleaning) -> list[int]:
    """The numbers of the rows a cleaning kept, in order."""
    return numbers(cleaning.frame)


def value(cleaning: Cleaning, row: int, column: str) -> Value:
    """One value of a kept row."""
    return values(cleaning.frame, column)[kept(cleaning).index(row)]


def shape(cleaning: Cleaning, row: int) -> BaseGeometry | None:
    """The shape of a kept row."""
    return shapes(cleaning.frame)[kept(cleaning).index(row)]


def log_lines(cleaning: Cleaning) -> list[str]:
    """The log as the file writes it, one line for each row a filter touched."""
    return [str(line) for line in cleaning.log]
