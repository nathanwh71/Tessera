"""Reads and checks a ***pipeline file*** before any work starts.

A mistake in the file then costs a second, not a half-finished run. Every fault is named at
once, so one failed run shows everything to fix; each table's ***filters*** are built and
chained while the file is read, so a bad setting is found before GDAL runs.
"""

import re
from collections import Counter
from dataclasses import dataclass
from typing import Any, assert_never

from ..kernel.pipes import Handler, chain
from ..kernel.result import Err, Ok, Result
from . import dotnet, filters, json_text, stj
from .plan import Area, Pipeline, StopWhen, TablePlan
from .rows import Cleaning
from .steps import stop_when


@dataclass(frozen=True, slots=True)
class CleaningPlan:
    """A table ready to clean: its plan, its filters' names in order, and their one chain.

    The stop rules come last in the chain. The names stay beside it, since the log counts
    each filter by name.
    """

    table: TablePlan
    filters: tuple[str, ...]
    chain: Handler[Cleaning]


@dataclass(frozen=True, slots=True)
class Plan:
    """A pipeline file read and checked, with each table's filters built and chained.

    A run starts only from a Plan, so it never starts from a file with a fault in it.
    """

    pipeline: Pipeline
    tables: tuple[CleaningPlan, ...]


DEFAULTS = StopWhen.model_validate({"droppedOverPercent": 10.0, "rowsUnder": 1})
"""The ***stop rules*** when the file sets none: stop when more than 10 % of a table's rows are
dropped, or when none remain. Stopping is the default, so a publisher who accepts more loss
must say so in the file."""

# A plain lower-case name needs no quotes in SQL and reads the same in a tile's address.
_PLAIN_NAME = re.compile(r"^[a-z_][a-z0-9_]*$")

# DbUp, the library that runs the seed scripts, runs them in the order of their names, so a
# four-digit number leads each name.
_SCRIPT_NAME = re.compile(r"(^|/)[0-9]{4}-[a-z0-9-]+\.sql$")


def read(text: str, area_from_settings: Area | None) -> Result[Plan, tuple[str, ...]]:
    """Reads and checks a pipeline file's text, and builds each table's chain of filters.

    `area_from_settings` stands in when the file names no area of its own.
    """
    parsed = json_text.parse(text)
    if isinstance(parsed, json_text.SyntaxFault):
        return Err((f"The pipeline file is not valid: {parsed}",))
    if parsed.value is None:
        return Err(("The pipeline file needs source, output and tables.",))
    match stj.read(Pipeline, parsed):
        case Err(why):
            return Err((f"The pipeline file is not valid: {why}",))
        case Ok(file):
            pass
        case _:
            assert_never(parsed)
    if any(table is None for table in file.tables):
        return Err(("tables: each entry must be an object that names its table.",))
    tables = tuple(table for table in file.tables if table is not None)
    pipeline = file.model_copy(update={"area": file.area or area_from_settings})
    built = [_build(table, pipeline) for table in tables]
    problems = list(_problems(pipeline, tables))
    for result in built:
        if isinstance(result, Err):
            problems.extend(result.error)
    if problems:
        return Err(tuple(problems))
    return Ok(Plan(pipeline, tuple(result.value for result in built if isinstance(result, Ok))))


def _build(table: TablePlan, pipeline: Pipeline) -> Result[CleaningPlan, tuple[str, ...]]:
    """Builds a table's filters from the file, in order, and joins them with `>>`.

    The stop rules come last. Each object names its filter in `filter`; the rest are its
    settings. The table's own stop rules win over the file's, and the file's over the
    defaults, so one table may allow more loss than the rest.
    """
    problems: list[str] = []
    names: list[str] = []
    steps: list[Handler[Cleaning]] = []
    for step in table.filters:
        given: dict[str, Any] = dict(step or {})
        name = given.get("filter")
        kind = filters.find(name) if isinstance(name, str) else None
        if kind is None:
            known = ", ".join(known.name for known in filters.KINDS)
            problems.append(f"{table.table}: '{name if isinstance(name, str) else ''}' is no filter; the filters are {known}.")
            continue
        settings = {key: value for key, value in given.items() if key != "filter"}
        match kind.build(settings, pipeline.area):
            case Ok(handler):
                names.append(kind.name)
                steps.append(handler)
            case Err(why):
                problems.extend(f"{table.table}: {problem}" for problem in why)
            case _ as never:
                assert_never(never)
    rules = table.stop_when or pipeline.stop_when or DEFAULTS
    joined = chain(steps) >> stop_when(rules)
    return Err(tuple(problems)) if problems else Ok(CleaningPlan(table, tuple(names), joined))


def _problems(pipeline: Pipeline, tables: tuple[TablePlan, ...]) -> list[str]:
    """The faults in the file as a whole, beyond its filters.

    Each rule guards a later step: GDAL needs a source, DbUp runs scripts by numbered name, the
    area box must not be empty, and each table must have one plain name of its own, so that SQL
    and a tile's address read it unquoted.
    """
    problems: list[str] = []
    if dotnet.is_null_or_white_space(pipeline.source):
        problems.append("source: name the file to read, such as data/macau.gdb.")
    if dotnet.is_null_or_white_space(pipeline.output) or not _SCRIPT_NAME.search(pipeline.output):
        problems.append(
            f"output: '{pipeline.output}' must be named NNNN-words.sql, such as db/seed/0002-parks.sql, so DbUp runs it in order."
        )
    if not tables:
        problems.append("tables: name at least one table.")
    area = pipeline.area
    if area is not None and (area.min_longitude >= area.max_longitude or area.min_latitude >= area.max_latitude):
        problems.append("area: the box must run west to east and south to north.")
    counts = Counter(table.table for table in tables)
    problems.extend(f"{name}: named more than once." for name, count in counts.items() if count > 1)
    for table in tables:
        parts = table.table.split(".")
        if len(parts) != 2 or not all(_PLAIN_NAME.match(part) for part in parts):
            problems.append(f"{table.table}: write the table as schema.table, in lower-case letters, digits and _.")
    return problems
