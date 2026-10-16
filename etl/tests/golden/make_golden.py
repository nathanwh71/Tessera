"""Records the C# tool's outcome for each case in tests/fixtures/cases, as golden files.

    uv run python tests/golden/make_golden.py [CASE ...]

It needs the C# tool built in Release (see README.md); TESSERA_CSHARP and DOTNET point to it
when it is not in the usual place. The golden files are committed, so the tests need neither.
"""

import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))

from staging import CASES, run, save

CSHARP = os.environ.get("TESSERA_CSHARP", "/home/claude/tessera/tools/Tessera.Etl/bin/Release/net10.0/Tessera.Etl.dll")
DOTNET = os.environ.get("DOTNET", "/opt/dotnet/dotnet")


def main() -> None:
    """Runs the C# tool on each case named, or on every case, and keeps the outcome."""
    os.environ.setdefault("DOTNET_ROOT", str(Path(DOTNET).parent))
    names = sys.argv[1:] or sorted(path.name for path in CASES.iterdir() if path.is_dir())
    for name in names:
        ran = run([DOTNET, CSHARP], name)
        save(name, ran)
        print(f"{name}: exit {ran.code}")


if __name__ == "__main__":
    main()
