"""The work done on each table between the extract and the script.

It reads no file and runs no program, so it is a ***pure function***: the same rows in give the
same result out. shell/program.py does the reading and writing around it, and the tests call it
with GeoJSON they write themselves.
"""

from dataclasses import dataclass

from ..kernel.result import Err, Ok, Result
from . import geojson, table_sql
from .failure import Failure, Refused, Stopped, Unreadable
from .pipeline import CleaningPlan
from .rows import Cleaning, Outcome


@dataclass(frozen=True, slots=True)
class Loaded:
    """A table cleaned and written: its counts and log lines, and the SQL that makes and fills it."""

    outcome: Outcome
    sql: tuple[str, ...]


def load(plan: CleaningPlan, extract: Result[geojson.Extract, str]) -> Result[Loaded, Failure]:
    """Runs the table's chain of filters and stop rules on its rows, and writes them as SQL.

    It returns the first failure met, and does no work after it.
    """
    match extract:
        case Err(why):
            return Err(Unreadable(plan.table, why))
        case Ok(read):
            pass
    done = plan.chain(Cleaning.start(plan.table, read.rows))
    if done.stop is not None:
        return Err(Stopped(done.outcome, done.stop))
    match table_sql.statements(plan.table, done.rows, read.columns):
        case Ok(sql):
            return Ok(Loaded(done.outcome, sql))
        case Err(why):
            return Err(Refused(done.outcome, why))


def load_geojson(plan: CleaningPlan, text: str) -> Result[Loaded, Failure]:
    """As `load`, from GeoJSON text, the form the C# tool read every table in."""
    return load(plan, geojson.read(text))
