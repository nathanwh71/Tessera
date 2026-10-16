"""Runs the C# tool and the Python tool on the same folders, and says where their outputs differ.

    uv run python tools/compare_with_csharp.py FOLDER[:PIPELINE] ...

Each folder holds a pipeline file, p.json unless named after a colon, and may hold tessera.json and the source. Each
tool runs in a fresh copy of the folder, so neither sees the other's script. The comparison
covers the exit code, what each printed, the script, and the log, less its first line, which
holds the time of the run. Set TESSERA_CSHARP to the C# tool's built .dll if it is not in the
usual place.
"""

import json
import os
import shutil
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

CSHARP = os.environ.get(
    "TESSERA_CSHARP", "/home/claude/tessera/tools/Tessera.Etl/bin/Release/net10.0/Tessera.Etl.dll"
)
DOTNET = os.environ.get("DOTNET", "/opt/dotnet/dotnet")
PYTHON_TOOL = [sys.executable, "-m", "tessera_etl"]


@dataclass(frozen=True, slots=True)
class Outcome:
    """What one run left behind."""

    code: int
    out: str
    err: str
    script: bytes | None
    log: list[str] | None


def _run(command: list[str], folder: Path, pipeline: str) -> Outcome:
    work = Path(tempfile.mkdtemp(prefix="tessera-compare-"))
    copy = work / folder.name
    shutil.copytree(folder, copy, ignore=shutil.ignore_patterns("seed", "seed-logs", "db"))
    environment = dict(os.environ, DOTNET_ROOT=str(Path(DOTNET).parent))
    done = subprocess.run([*command, pipeline], cwd=copy, capture_output=True, text=True, env=environment, check=False)
    try:
        output = str(json.loads((copy / pipeline).read_text(encoding="utf-8"))["output"])
    except (ValueError, KeyError, TypeError):
        output = ""
    script = copy / output
    log = copy / f"{os.path.dirname(output)}-logs" / (Path(output).stem + ".log")
    outcome = Outcome(
        done.returncode,
        done.stdout,
        done.stderr,
        script.read_bytes() if output and script.is_file() else None,
        log.read_text(encoding="utf-8").split("\n")[1:] if output and log.is_file() else None,
    )
    shutil.rmtree(work)
    return outcome


def compare(folder: Path, pipeline: str = "p.json") -> list[str]:
    """The differences between the two tools' runs in one folder; none when they agree."""
    csharp = _run([DOTNET, CSHARP], folder, pipeline)
    python = _run(PYTHON_TOOL, folder, pipeline)
    differences: list[str] = []
    for name in ("code", "out", "err", "script", "log"):
        if getattr(csharp, name) != getattr(python, name):
            differences.append(f"{name} differs:\n  C#:     {str(getattr(csharp, name))[:600]!r}\n  Python: {str(getattr(python, name))[:600]!r}")
    return differences


def main() -> int:
    """Compares every folder named, and returns 1 when any differs."""
    failed = 0
    for name in sys.argv[1:]:
        folder, _, pipeline = name.partition(":")
        differences = compare(Path(folder).resolve(), pipeline or "p.json")
        print(f"{name}: {'same' if not differences else 'DIFFERENT'}")
        for difference in differences:
            print("  " + difference)
        failed += bool(differences)
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
