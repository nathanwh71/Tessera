"""The Python tool against the C# tool's own outcomes, kept as golden files.

Each case under tests/fixtures/cases ran through the C# tool once (tests/golden/make_golden.py);
here the Python tool runs on the same files, and must print the same lines, return the same exit
code, and write the same script, byte for byte, and the same log, less its first line, which
holds the time of the run. The faulty pipeline files of tests/golden/pipelines.json must give
the C# tool's words for each fault.
"""

import contextlib
import hashlib
import io
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

import pytest
from staging import CASES, GOLDEN, cases, load, run

from tessera_etl.shell import program

PYTHON = [sys.executable, "-m", "tessera_etl"]


@pytest.mark.parametrize("case", cases())
def test_matches_the_CSharp_tool(case: str) -> None:
    expected = load(case)
    found = run(PYTHON, case)

    assert found.code == expected.code
    assert found.stdout == expected.stdout
    assert found.stderr == expected.stderr
    assert found.script == expected.script
    assert found.log == expected.log


PIPELINES = json.loads((GOLDEN / "pipelines.json").read_text(encoding="utf-8"))


def run_here(folder: Path, arguments: list[str]) -> tuple[int, str, str]:
    """Runs the tool in this process, in `folder`, and gathers its exit code and two streams."""
    out, err = io.StringIO(), io.StringIO()
    before = Path.cwd()
    os.chdir(folder)
    try:
        with contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
            code = program.run(arguments)
    finally:
        os.chdir(before)
    return code, out.getvalue(), err.getvalue()


@pytest.mark.parametrize("case", PIPELINES, ids=[case["name"] for case in PIPELINES])
def test_words_each_fault_in_a_pipeline_file_as_the_CSharp_tool_did(case: dict[str, object], tmp_path: Path) -> None:
    (tmp_path / "p.json").write_text(str(case["text"]), encoding="utf-8")
    if case["settings"]:
        shutil.copy(CASES / "macau" / "tessera.json", tmp_path / "tessera.json")

    assert run_here(tmp_path, ["p.json"]) == (case["code"], case["stdout"], case["stderr"])


@pytest.mark.parametrize(
    ("arguments", "stderr"),
    [
        ([], "Usage: npm run etl -- PIPELINE.json\n"),
        (["a.json", "b.json"], "Usage: npm run etl -- PIPELINE.json\n"),
        (["nothing.json"], "nothing.json: no such file.\n"),
    ],
)
def test_refuses_a_wrong_command_as_the_CSharp_tool_did(arguments: list[str], stderr: str, tmp_path: Path) -> None:
    assert run_here(tmp_path, arguments) == (2, "", stderr)


def test_runs_as_a_console_command(tmp_path: Path) -> None:
    command = shutil.which("tessera-etl", path=str(Path(sys.executable).parent))
    assert command is not None
    done = subprocess.run([command], cwd=tmp_path, capture_output=True, text=True, check=False)
    assert (done.returncode, done.stderr) == (2, "Usage: npm run etl -- PIPELINE.json\n")


SCALE = os.environ.get("TESSERA_SCALE_SOURCE")


@pytest.mark.skipif(SCALE is None, reason="set TESSERA_SCALE_SOURCE to big.geojson, 90,000 places, to run")
def test_writes_the_90000_place_script_the_CSharp_tool_wrote(tmp_path: Path) -> None:
    # The script is 78 MB, too large to keep, so its checksum stands in for it.
    assert SCALE is not None
    folder = tmp_path / "scale"
    shutil.copytree(Path(__file__).parent / "fixtures" / "scale", folder)
    os.symlink(SCALE, folder / "big.geojson")

    done = subprocess.run([*PYTHON, "p.json"], cwd=folder, capture_output=True, text=True, check=False)

    assert done.returncode == 0, done.stderr
    expected = json.loads((GOLDEN / "scale.json").read_text(encoding="utf-8"))
    assert hashlib.sha256((folder / "seed/0002-big.sql").read_bytes()).hexdigest() == expected["sha256"]
    log = (folder / "seed-logs/0002-big.log").read_text(encoding="utf-8").split("\n")[1:]
    assert "\n".join(log) == expected["log"]
    assert done.stdout == expected["stdout"]
