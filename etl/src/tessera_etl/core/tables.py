"""The work done on each table between the extract and the script.

It reads no file and runs no program, so it is a ***pure function***: the same rows in give the
same result out. shell/program.py does the reading and writing around it, and the tests call it
with tables they build themselves.
"""

from dataclasses import dataclass

from ..kernel.result import Err, Ok, Result
from . import table_sql
from .failure import Failure, Refused, Stopped
from .geojson import Extract
from .pipeline import CleaningPlan
from .rows import Cleaning, Outcome


@dataclass(frozen=True, slots=True)
class Loaded:
    """A table cleaned and written: its counts and log lines, and the SQL that makes and fills it."""

    outcome: Outcome
    sql: tuple[str, ...]


def load(plan: CleaningPlan, extract: Extract) -> Result[Loaded, Failure]:
    """Runs the table's chain of filters and stop rules on its rows, and writes them as SQL.

    It returns the first failure met, and does no work after it.
    """
    done = plan.chain(Cleaning.start(plan.table, extract.frame))
    if done.stop is not None:
        return Err(Stopped(done.outcome, done.stop))
    match table_sql.statements(plan.table, done.frame, extract.absent):
        case Ok(sql):
            return Ok(Loaded(done.outcome, sql))
        case Err(why):
            return Err(Refused(done.outcome, why))
