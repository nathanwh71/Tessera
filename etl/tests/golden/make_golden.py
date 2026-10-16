"""Records the pipeline's outcome for each case in tests/fixtures/cases, as golden files.

    uv run python tests/golden/make_golden.py [CASE ...]

Compare each changed script with the one it replaces before committing it, with
tools/compare_meaning.py: a script may change its text, but never its meaning (ADR 0009).
"""

import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from staging import CASES, run, save

PIPELINE = [sys.executable, "-m", "tessera_etl"]


def main() -> None:
    """Runs the pipeline on each case named, or on every case, and keeps the outcome."""
    names = sys.argv[1:] or sorted(path.name for path in CASES.iterdir() if path.is_dir())
    for name in names:
        with tempfile.TemporaryDirectory(prefix="tessera-golden-") as work:
            ran = run(PIPELINE, name, Path(work))
        save(name, ran)
        print(f"{name}: exit {ran.code}")


if __name__ == "__main__":
    main()
