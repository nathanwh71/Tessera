"""The log of a whole run, as plain text.

A dropped row leaves no trace in the script, so this log is the publisher's only account of
what was left out and why. It is a ***pure function*** of the run's results: the program writes
the file.
"""

from collections.abc import Sequence
from datetime import datetime

from .pipeline import CleaningPlan
from .plan import Pipeline
from .rows import Act, Outcome


def stamp(when: datetime) -> str:
    """A moment as .NET's pattern 'yyyy-MM-dd HH:mm:ss zzz' writes it, such as 2026-10-15 13:00:00 +08:00."""
    offset = when.utcoffset()
    minutes = 0 if offset is None else int(offset.total_seconds() // 60)
    sign = "+" if minutes >= 0 else "-"
    return f"{when:%Y-%m-%d %H:%M:%S} {sign}{abs(minutes) // 60:02d}:{abs(minutes) % 60:02d}"


def log(
    pipeline_file: str, pipeline: Pipeline, tables: Sequence[tuple[CleaningPlan, Outcome]], result: str, when: datetime
) -> str:
    """The source, then for each table its counts and touched rows; then the result.

    For each table: the rows read, a count for each ***filter***, and one line for each row a
    filter fixed, dropped or stopped at. Each table comes as its plan, for the filters' names
    in order, and its outcome: the counts and lines its chain ended with.
    """
    lines = [
        f"Tessera ETL log, {stamp(when)}",
        f"Pipeline: {pipeline_file}",
        f"Source:   {pipeline.source}",
        f"Output:   {pipeline.output}",
    ]
    for plan, done in tables:
        table = done.table
        layer = f", from {table.layer}" if table.layer is not None else ""
        where = f", where {table.where}" if table.where is not None else ""
        lines += ["", f"== {table.table}{layer}{where}", f"Read {done.read} rows."]
        # Each filter's counts come from the joined log, whose lines name their filter. The
        # rows kept after a filter are those read less every row dropped up to and including
        # it. A filter listed twice gets one line, with the counts of both its runs.
        kept_so_far = done.read
        for name in dict.fromkeys(plan.filters):
            mine = [line for line in done.log if line.filter == name]
            dropped = sum(1 for line in mine if line.act is Act.DROPPED)
            fixed = sum(1 for line in mine if line.act is Act.FIXED)
            kept_so_far -= dropped
            stopped = any(line.act is Act.STOPPED for line in mine)
            lines.append(f"  {name}: {'stopped' if stopped else f'kept {kept_so_far}'}, fixed {fixed}, dropped {dropped}")
            # The filters after a stop never ran, so they get no line of counts.
            if stopped:
                break
        # A filter that stops hands back its rows unjudged, so none counts as kept. A table
        # stopped by its stop rules did judge every row, so its counts still stand.
        if any(line.act is Act.STOPPED for line in done.log):
            lines.append("Stopped; nothing kept.")
        else:
            lines.append(f"Kept {done.kept} of {done.read}; dropped {done.dropped}.")
        lines += [f"  {line}" for line in done.log]
    lines += ["", f"Result: {result}"]
    return "\n".join(lines) + "\n"
