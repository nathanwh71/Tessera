"""Turns a file of places into a seed script for DbUp, cleaning it on the way.

    tessera-etl data/macau.pipeline.json

Extract:   pyogrio reads each table from the source, such as a geodatabase's feature class, in
           longitude and latitude (shell/extract.py), two tables side by side.
Transform: a chain of steps cleans each table's rows: the pipeline file's filters, in the order
           it lists them, then the stop rules (kernel/pipes.py, core/steps.py).
Load:      the program writes each table's cleaned rows as SQL (core/table_sql.py), one table
           after another, into one numbered script.

Every run that passes its checks writes a log beside the scripts' folder, such as
db/seed-logs/, of what each filter did to each row; a wrong command, pipeline file, source or
script name stops before that, with no log, since no row has been read. A run stops, and writes
no script, when a filter set to stop finds a bad row, or when a table breaks its stop rules. It
ends with an ***exit code***, the number a program hands back to whoever ran it, so scripts can
tell the outcomes apart: 0 when it wrote the script, 1 when it stopped or failed, 2 when a check
fails before any row is read.

Only this module, shell/extract.py and shell/gdal.py touch files or run programs; everything
they call in core/ is pure.
"""

import argparse
import io
import json
import shutil
import sys
import tempfile
from collections.abc import Iterator, Sequence
from concurrent.futures import Future, ThreadPoolExecutor
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import assert_never

from ..core import failure, pipeline, report, seed_script, tables
from ..core.geojson import Extract
from ..core.pipeline import CleaningPlan, Plan
from ..core.plan import Area
from ..core.rows import Outcome
from ..kernel.result import Err, Ok, Result
from . import extract

SIDE_BY_SIDE = 2
"""How many tables are read at once. Each read waits on GDAL, which lets other work run in the
meantime; more than two at once gains little, and holds more tables in memory."""


def main(arguments: Sequence[str] | None = None) -> int:
    """Runs the tool on the command's arguments and returns its exit code."""
    # The script and the log are UTF-8 with plain line ends on every system, and so is what
    # the program prints.
    for stream in (sys.stdout, sys.stderr):
        if isinstance(stream, io.TextIOWrapper):
            stream.reconfigure(encoding="utf-8", newline="\n")
    code = run(sys.argv[1:] if arguments is None else list(arguments))
    sys.stdout.flush()
    return code


def _fail(code: int, *problems: str) -> int:
    """Prints the problems and returns the exit code, with no log.

    It serves the checks before any row is read, so a log would have nothing to report.
    """
    for problem in problems:
        print(problem, file=sys.stderr)
    return code


PARSER = argparse.ArgumentParser(
    prog="npm run etl --",
    description="Cleans a file of places into one SQL seed script, and logs what it did to every row.",
    exit_on_error=False,
)
PARSER.add_argument("pipeline", metavar="PIPELINE.json", type=Path, help="the pipeline file, such as data/macau.pipeline.json")


def run(arguments: list[str]) -> int:
    """The whole run: the checks, then each table, then the script and its log."""
    try:
        given = PARSER.parse_args(arguments)
    except argparse.ArgumentError as error:
        return _fail(2, PARSER.format_usage().rstrip(), f"{error}.")
    pipeline_file: Path = given.pipeline
    if not pipeline_file.is_file():
        return _fail(2, f"{pipeline_file}: no such file.")
    text = pipeline_file.read_bytes().decode("utf-8-sig", "replace")
    match pipeline.read(text, area_from_settings(Path("tessera.json"))):
        case Err(problems):
            return _fail(2, *problems)
        case Ok(plan):
            return _checked(str(pipeline_file), plan)


def area_from_settings(settings_file: Path) -> Area | None:
    """The area's box from tessera.json's Area section, when the file is here.

    A pipeline then need not repeat it; None when the file or its Area section is missing.
    """
    if not settings_file.is_file():
        return None
    area = json.loads(settings_file.read_text(encoding="utf-8-sig")).get("Area")
    if area is None:
        return None
    return Area.model_validate(
        {key[0].lower() + key[1:]: float(area[key]) for key in ("MinLongitude", "MinLatitude", "MaxLongitude", "MaxLatitude")}
    )


def _checked(pipeline_file: str, plan: Plan) -> int:
    """Checks the source and the script's name, then runs every table in a work folder of its own."""
    file = plan.pipeline
    # A full path, since GDAL's Docker image sees each folder at the same path.
    source = Path(file.source).absolute()
    if not source.exists():
        return _fail(2, f"{file.source}: no such file or folder.")
    # DbUp runs a script once, known by its name, and never again; a changed script would
    # change nothing.
    if Path(file.output).is_file():
        return _fail(2, f"{file.output} already exists. Give new data a new script, with the next number.")
    # A fresh folder for the copy and the script as it grows, deleted however the run ends.
    with tempfile.TemporaryDirectory(prefix="tessera-etl-") as work:
        tables_run = _Run(pipeline_file, plan, [])
        return tables_run.finish(*tables_run.tables(source, Path(work)))


def message(fault: failure.Failure) -> str:
    """The line the program prints, and the log ends with, when a table fails."""
    match fault:
        case failure.NotExtracted(table, why):
            return f"{table.table}: extraction failed. {why}"
        case failure.Stopped(_, why):
            return f"stopped. {why} No script written."
        case failure.Refused(_, why):
            return why
        case _:
            assert_never(fault)


def read_ahead(source: extract.Source, plans: Sequence[CleaningPlan]) -> Iterator[Result[Extract, str]]:
    """Each table's rows, in the order of the plans, read up to SIDE_BY_SIDE at once.

    The next read starts as each table is handed out, so no more than SIDE_BY_SIDE tables wait
    in memory at a time.
    """
    with ThreadPoolExecutor(max_workers=SIDE_BY_SIDE) as pool:
        waiting: list[Future[Result[Extract, str]]] = [
            pool.submit(extract.read_table, source, plan.table) for plan in plans[:SIDE_BY_SIDE]
        ]
        for upcoming in plans[SIDE_BY_SIDE:]:
            yield waiting.pop(0).result()
            waiting.append(pool.submit(extract.read_table, source, upcoming.table))
        for read in waiting:
            yield read.result()


@dataclass(slots=True)
class _Run:
    """One run past its checks, and each table's counts for the log as the run goes."""

    pipeline_file: str
    plan: Plan
    cleaned: list[tuple[CleaningPlan, Outcome]]

    def tables(self, source: Path, work: Path) -> tuple[int, str]:
        """Extracts, cleans and loads each table; the exit code and the result line."""
        file = self.plan.pipeline
        plans = self.plan.tables
        if seed_script.copy_first(str(source), len(plans)):
            print(f"{file.source}: copying into a GeoPackage, which each table reads...", flush=True)
        match extract.open_source(source, len(plans), work):
            case Err(why):
                return 1, f"{file.source}: the copy failed. {why}"
            case Ok(opened):
                pass
        print(f"extracting {len(plans)} tables...", flush=True)
        # The script is written in the work folder as each table passes, and moved to its place
        # only once every table has passed, so a run that stops still leaves no script.
        draft = work / "script.sql"
        with draft.open("w", encoding="utf-8", newline="\n") as script:
            script.writelines(f"{line}\n" for line in seed_script.head(self.pipeline_file, (plan.table for plan in plans)))
            for plan, read in zip(plans, read_ahead(opened, plans), strict=True):
                match read:
                    case Err(why):
                        loaded: Result[tables.Loaded, failure.Failure] = Err(failure.NotExtracted(plan.table, why))
                    case Ok(rows):
                        loaded = tables.load(plan, rows)
                match loaded:
                    case Err(fault):
                        # A stop or a refusal comes after the cleaning, so its counts still
                        # reach the log.
                        counted = failure.cleaned(fault)
                        if counted is not None:
                            self.cleaned.append((plan, counted))
                        return 1, message(fault)
                    case Ok(done):
                        self.cleaned.append((plan, done.outcome))
                        outcome = done.outcome
                        print(
                            f"{plan.table.table}: read {outcome.read}, kept {outcome.kept}, dropped {outcome.dropped}.",
                            flush=True,
                        )
                        script.writelines(f"{statement}\n" for statement in done.sql)
        output = Path(file.output)
        output.absolute().parent.mkdir(parents=True, exist_ok=True)
        shutil.move(draft, output)
        kept = sum(outcome.kept for _, outcome in self.cleaned)
        return 0, f"wrote {file.output}: {len(plans)} tables, {kept} rows. Read it, then commit it; 'Tessera seed' runs it once."

    def finish(self, code: int, result: str) -> int:
        """Writes the log, prints the result and returns the exit code.

        Every success, stop and failure after the checks comes through here, so a stopped run
        leaves a log as surely as a finished one. The log lies beside the scripts' folder:
        db/seed/0002-x.sql logs to db/seed-logs/0002-x.log.
        """
        output = self.plan.pipeline.output
        log_file = Path(f"{output.rpartition('/')[0]}-logs", f"{Path(output).stem}.log")
        log_file.absolute().parent.mkdir(parents=True, exist_ok=True)
        text = report.log(self.pipeline_file, self.plan.pipeline, self.cleaned, result, datetime.now().astimezone())
        log_file.write_text(text, encoding="utf-8", newline="\n")
        print(f"{'Done' if code == 0 else 'Stopped'}: {result}", file=sys.stdout if code == 0 else sys.stderr, flush=True)
        print(f"The log is in {log_file}.", flush=True)
        return code


def entry() -> None:
    """The console command, `tessera-etl PIPELINE.json`."""
    sys.exit(main())
