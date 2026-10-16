"""Turns a file of places into a seed script for DbUp, cleaning it on the way.

    tessera-etl data/macau.pipeline.json

Extract:   pyogrio reads each table from the source, such as a geodatabase's feature class, in
           longitude and latitude (shell/extract.py).
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

Only this module and shell/extract.py touch files or run programs; everything they call in
core/ is pure.
"""

import json
import shutil
import sys
import tempfile
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import assert_never

from ..core import failure, pipeline, report, seed_script, tables
from ..core.pipeline import CleaningPlan, Plan
from ..core.plan import Area
from ..core.rows import Outcome
from ..kernel.result import Err, Ok
from . import extract


def main(arguments: list[str] | None = None) -> int:
    """Runs the tool on the command's arguments and returns its exit code."""
    for stream in (sys.stdout, sys.stderr):
        reconfigure = getattr(stream, "reconfigure", None)
        if reconfigure is not None:
            reconfigure(encoding="utf-8", newline="\n")
    given = sys.argv[1:] if arguments is None else arguments
    code = run(given)
    sys.stdout.flush()
    return code


def _fail(code: int, *problems: str) -> int:
    """Prints the problems and returns the exit code, with no log.

    It serves the checks before any row is read, so a log would have nothing to report.
    """
    for problem in problems:
        print(problem, file=sys.stderr)
    return code


def _area_from_settings(settings_file: Path) -> Area | None:
    """The area's box from tessera.json's Area section, when the file is here.

    A pipeline then need not repeat it; None when the file or its Area section is missing.
    """
    if not settings_file.is_file():
        return None
    settings = json.loads(settings_file.read_text(encoding="utf-8-sig"))
    area = settings.get("Area")
    if area is None:
        return None
    return Area.model_validate(
        {
            "minLongitude": float(area["MinLongitude"]),
            "minLatitude": float(area["MinLatitude"]),
            "maxLongitude": float(area["MaxLongitude"]),
            "maxLatitude": float(area["MaxLatitude"]),
        }
    )


def run(arguments: list[str]) -> int:
    """The whole run: the checks, then each table, then the script and its log."""
    if len(arguments) != 1:
        return _fail(2, "Usage: npm run etl -- PIPELINE.json")
    pipeline_file = arguments[0]
    if not Path(pipeline_file).is_file():
        return _fail(2, f"{pipeline_file}: no such file.")
    text = Path(pipeline_file).read_bytes().decode("utf-8-sig", "replace")
    match pipeline.read(text, _area_from_settings(Path("tessera.json"))):
        case Err(problems):
            return _fail(2, *problems)
        case Ok(plan):
            return _Run(pipeline_file, plan).go()


@dataclass(slots=True)
class _Run:
    """One run past its checks: the files it writes, and each table's counts for the log."""

    pipeline_file: str
    plan: Plan
    cleaned: list[tuple[CleaningPlan, Outcome]] = field(default_factory=list[tuple[CleaningPlan, Outcome]])

    @property
    def log_file(self) -> Path:
        """The log beside the scripts' folder: db/seed/0002-x.sql logs to db/seed-logs/0002-x.log."""
        output = self.plan.pipeline.output
        return Path(f"{output.rpartition('/')[0]}-logs", f"{Path(output).stem}.log")

    def go(self) -> int:
        """Checks the source and the script's name, then extracts, cleans and loads each table."""
        file = self.plan.pipeline
        # A full path, since GDAL's Docker image sees each folder at the same path.
        source = str(Path(file.source).absolute())
        if not Path(source).exists():
            return _fail(2, f"{file.source}: no such file or folder.")
        # DbUp runs a script once, known by its name, and never again; a changed script would
        # change nothing.
        if Path(file.output).is_file():
            return _fail(2, f"{file.output} already exists. Give new data a new script, with the next number.")
        # A fresh folder for the copy and the script as it grows, deleted at the end however
        # the run ends.
        work = tempfile.mkdtemp(prefix="tessera-etl-")
        try:
            return self._tables(source, work)
        finally:
            shutil.rmtree(work, ignore_errors=True)

    def _tables(self, source: str, work: str) -> int:
        file = self.plan.pipeline
        plans = self.plan.tables
        if seed_script.copy_first(source, len(plans)):
            print(f"{file.source}: copying into a GeoPackage, which each table reads...", flush=True)
        match extract.open_source(source, len(plans), work):
            case Err(why):
                return self._finish(1, f"{file.source}: the copy failed. {why}")
            case Ok(reader):
                pass
        print(f"extracting {len(plans)} tables...", flush=True)
        # The script is written in the work folder as each table passes, and moved to its place
        # only once every table has passed, so a run that stops still leaves no script.
        draft = Path(work, "script.sql")
        with draft.open("w", encoding="utf-8", newline="\n") as script:
            for line in seed_script.head(self.pipeline_file, (plan.table for plan in plans)):
                script.write(line + "\n")
            for plan in plans:
                match extract.read_table(reader, plan.table):
                    case Err(why):
                        loaded = Err(failure.NotExtracted(plan.table, why))
                    case Ok(rows):
                        loaded = tables.load(plan, rows)
                match loaded:
                    case Err(fault):
                        # A stop or a refusal comes after the cleaning, so its counts still
                        # reach the log.
                        counted = failure.cleaned(fault)
                        if counted is not None:
                            self.cleaned.append((plan, counted))
                        return self._finish(1, message(fault))
                    case Ok(done):
                        self.cleaned.append((plan, done.outcome))
                        outcome = done.outcome
                        print(
                            f"{plan.table.table}: read {outcome.read}, kept {outcome.kept}, dropped {outcome.dropped}.",
                            flush=True,
                        )
                        for statement in done.sql:
                            script.write(statement + "\n")
        Path(file.output).absolute().parent.mkdir(parents=True, exist_ok=True)
        shutil.move(draft, file.output)
        kept = sum(outcome.kept for _, outcome in self.cleaned)
        return self._finish(
            0, f"wrote {file.output}: {len(plans)} tables, {kept} rows. Read it, then commit it; 'Tessera seed' runs it once."
        )

    def _finish(self, code: int, result: str) -> int:
        """Writes the log, prints the result and returns the exit code.

        Every success, stop and failure after the checks comes through here, so a stopped run
        leaves a log as surely as a finished one.
        """
        log_file = self.log_file
        log_file.absolute().parent.mkdir(parents=True, exist_ok=True)
        text = report.log(self.pipeline_file, self.plan.pipeline, self.cleaned, result, datetime.now().astimezone())
        with log_file.open("w", encoding="utf-8", newline="\n") as file:
            file.write(text)
        print(f"{'Done' if code == 0 else 'Stopped'}: {result}", file=sys.stdout if code == 0 else sys.stderr, flush=True)
        print(f"The log is in {log_file}.", flush=True)
        return code


def message(fault: failure.Failure) -> str:
    """The line the program prints, and the log ends with, when a table fails."""
    match fault:
        case failure.NotExtracted(table, why):
            return f"{table.table}: extraction failed. {why}"
        case failure.Unreadable(table, why):
            return f"{table.table}: GDAL's GeoJSON could not be read. {why}"
        case failure.Stopped(_, why):
            return f"stopped. {why} No script written."
        case failure.Refused(_, why):
            return why
        case _:
            assert_never(fault)


def entry() -> None:
    """The console command, `tessera-etl PIPELINE.json`."""
    sys.exit(main())
