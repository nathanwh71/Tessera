"""Rows, areas and runners the tests share, so every test builds its places the same way."""

from collections.abc import Sequence

from shapely.geometry import Point
from shapely.geometry.base import BaseGeometry

from tessera_etl.core.plan import Area, TablePlan
from tessera_etl.core.rows import Cleaning, Row, Value, frozen_values
from tessera_etl.kernel.pipes import Step

MACAU = Area.model_validate({"minLongitude": 113.52, "minLatitude": 22.10, "maxLongitude": 113.61, "maxLatitude": 22.22})
"""The area: Macau's box, as tessera.json holds it."""

TABLE = TablePlan.model_validate({"table": "food.cafes"})
"""The table every test cleans; the filters ignore it, but the stop rules and the log name it."""


def table(name: str = "food.cafes", **settings: object) -> TablePlan:
    """A table plan as a pipeline file would give it, with its settings in camelCase."""
    return TablePlan.model_validate({"table": name, **settings})


def place(number: int, longitude: float, latitude: float, **values: Value) -> Row:
    """A point place with the given values."""
    return Row(number, Point(longitude, latitude), frozen_values(values))


def shaped(number: int, shape: BaseGeometry | None, **values: Value) -> Row:
    """A place with any shape, or none."""
    return Row(number, shape, frozen_values(values))


def clean(steps: Step[Cleaning], rows: Sequence[Row], plan: TablePlan = TABLE) -> Cleaning:
    """Runs one step, or a chain of them, on the rows, as the pipeline runs a table's chain."""
    return steps(Cleaning.start(plan, tuple(rows)))


def log_lines(cleaning: Cleaning) -> list[str]:
    """The log as the file writes it, one line for each row a filter touched."""
    return [str(line) for line in cleaning.log]
