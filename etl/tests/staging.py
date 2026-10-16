"""Stages a golden case in a fresh folder, runs a tool there, and keeps what it left behind.

tests/golden/make_golden.py made the golden files under tests/golden by running the pipeline
this way; the golden tests run it the same way and compare.
"""

import json
import shutil
import subprocess
from dataclasses import asdict, dataclass
from pathlib import Path

FIXTURES = Path(__file__).parent / "fixtures"
CASES = FIXTURES / "cases"
GOLDEN = Path(__file__).parent / "golden"


@dataclass(frozen=True, slots=True)
class Ran:
    """What one run printed and wrote: its exit code, its two streams, its script and its log."""

    code: int
    stdout: str
    stderr: str
    script: str | None
    log: str | None


def stage(case: str, work: Path) -> Path:
    """A folder in `work` holding the case's own files and every source, under sources/."""
    folder = work / case
    shutil.copytree(CASES / case, folder)
    shutil.copytree(FIXTURES / "sources", folder / "sources")
    return folder


def run(command: list[str], case: str, work: Path, pipeline: str = "p.json") -> Ran:
    """Runs `command PIPELINE` in a copy of the case staged in `work`, and gathers the outcome.

    The log's first line holds the time of the run, so it is left out.
    """
    folder = stage(case, work)
    done = subprocess.run([*command, pipeline], cwd=folder, capture_output=True, check=False)
    output = str(json.loads((folder / pipeline).read_text(encoding="utf-8"))["output"])
    script = folder / output
    log = folder / f"{output.rpartition('/')[0]}-logs" / (Path(output).stem + ".log")
    existed = (CASES / case / output).is_file()
    return Ran(
        done.returncode,
        done.stdout.decode("utf-8"),
        done.stderr.decode("utf-8"),
        script.read_text(encoding="utf-8") if script.is_file() and not existed else None,
        "\n".join(log.read_text(encoding="utf-8").split("\n")[1:]) if log.is_file() else None,
    )


def save(case: str, ran: Ran) -> None:
    """Keeps an outcome as the case's golden files."""
    folder = GOLDEN / case
    folder.mkdir(parents=True, exist_ok=True)
    fields = asdict(ran)
    for name, suffix in (("script", ".sql"), ("log", ".log")):
        text = fields.pop(name)
        path = folder / f"expected{suffix}"
        if text is None:
            path.unlink(missing_ok=True)
        else:
            path.write_text(text, encoding="utf-8", newline="\n")
    (folder / "expected.json").write_text(json.dumps(fields, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def load(case: str) -> Ran:
    """The case's recorded outcome."""
    folder = GOLDEN / case
    fields = json.loads((folder / "expected.json").read_text(encoding="utf-8"))
    script, log = folder / "expected.sql", folder / "expected.log"
    return Ran(
        fields["code"],
        fields["stdout"],
        fields["stderr"],
        script.read_text(encoding="utf-8") if script.is_file() else None,
        log.read_text(encoding="utf-8") if log.is_file() else None,
    )


def cases() -> list[str]:
    """Every case with golden files."""
    return sorted(path.name for path in GOLDEN.iterdir() if (path / "expected.json").is_file())
