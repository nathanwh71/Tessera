"""Records the C# tool's words for each faulty pipeline file in pipelines.json.

    uv run python tests/golden/make_pipeline_golden.py

Each case is the text of a pipeline file. The C# tool reads it in an empty folder, so a file
that passes every check stops at its missing source; a case marked "settings" runs beside a
copy of tessera.json, whose area some filters need.
"""

import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).parent
# The C# tool's built .dll; nothing in this repository builds it, so it must be named.
CSHARP = os.environ.get("TESSERA_CSHARP", "")
DOTNET = os.environ.get("DOTNET", "/opt/dotnet/dotnet")
SETTINGS = HERE.parent / "fixtures" / "cases" / "macau" / "tessera.json"

GOOD_TABLE = '{ "table": "a.b", "layer": "L" }'
HEAD = '"source": "x.gdb", "output": "db/seed/0002-x.sql"'


def pipeline(tables: str = f"[ {GOOD_TABLE} ]", extra: str = "") -> str:
    return f'{{ {HEAD}, "tables": {tables}{extra} }}'


def filters(*items: str) -> str:
    return pipeline(f'[ {{ "table": "a.b", "filters": [ {", ".join(items)} ] }} ]')


CASES: dict[str, str] = {
    "unknown top key": pipeline(extra=', "extra": 1'),
    "missing source": '{ "output": "db/seed/0002-x.sql", "tables": [ { "table": "a.b" } ] }',
    "missing tables": f"{{ {HEAD} }}",
    "missing source and output": '{ "tables": [] }',
    "missing everything": "{ }",
    "source a number": '{ "source": 5, "output": "db/seed/0002-x.sql", "tables": [ { "table": "a.b" } ] }',
    "lines a string": pipeline('[ { "table": "a.b", "lines": "yes" } ]'),
    "lines null": pipeline('[ { "table": "a.b", "lines": null } ]'),
    "layer a number": pipeline('[ { "table": "a.b", "layer": 5 } ]'),
    "where null": pipeline('[ { "table": "a.b", "where": null } ]'),
    "on fail halt": filters('{ "filter": "insideArea", "onFail": "halt" }'),
    "on fail Stop": filters('{ "filter": "requireFields", "fields": ["a"], "onFail": "Stop" }'),
    "on fail one": filters('{ "filter": "requireFields", "fields": ["a"], "onFail": 1 }'),
    "on fail two": filters('{ "filter": "requireFields", "fields": ["a"], "onFail": 2 }'),
    "on fail text one": filters('{ "filter": "requireFields", "fields": ["a"], "onFail": "1" }'),
    "on fail true": filters('{ "filter": "requireFields", "fields": ["a"], "onFail": true }'),
    "on fail null": filters('{ "filter": "requireFields", "fields": ["a"], "onFail": null }'),
    "fields a string": filters('{ "filter": "requireFields", "fields": "name" }'),
    "fields null": filters('{ "filter": "requireFields", "fields": null }'),
    "fields empty": filters('{ "filter": "requireFields", "fields": [] }'),
    "fields a number in list": filters('{ "filter": "requireFields", "fields": [1] }'),
    "fields null in list": filters('{ "filter": "requireFields", "fields": [null] }'),
    "metres a word": filters('{ "filter": "dropNearDuplicates", "field": "name", "metres": "far" }'),
    "metres as text": filters('{ "filter": "dropNearDuplicates", "field": "name", "metres": "25" }'),
    "metres huge": filters('{ "filter": "dropNearDuplicates", "field": "n", "metres": 1e400 }'),
    "metres below zero": filters('{ "filter": "dropNearDuplicates", "field": "n", "metres": -5 }'),
    "metres null": filters('{ "filter": "dropNearDuplicates", "field": "n", "metres": null }'),
    "field blank and metres zero": filters('{ "filter": "dropNearDuplicates", "field": " ", "metres": 0 }'),
    "field null": filters('{ "filter": "dropDuplicateIds", "field": null }'),
    "field a number": filters('{ "filter": "dropDuplicateIds", "field": 7 }'),
    "field empty": filters('{ "filter": "dropDuplicateIds", "field": "" }'),
    "accent before a fault": filters('{ "filter": "dropNearDuplicates", "field": "café<&>", "metres": "far" }'),
    "swap takes none": filters('{ "filter": "swapCoordinates", "x": 1 }'),
    "setting of another filter": filters('{ "filter": "insideArea", "metres": 25 }'),
    "misspelt setting": filters('{ "filter": "requireFields", "feilds": ["name"] }'),
    "needs a setting": filters('{ "filter": "requireFields" }'),
    "filter a number": filters('{ "filter": 3 }'),
    "filter null": filters('{ "filter": null }'),
    "filter missing": filters('{ "onFail": "stop" }'),
    "filter wrong case": filters('{ "filter": "insidearea" }'),
    "filter not an object": filters("5"),
    "filter a string": filters('"tidyText"'),
    "filters null": pipeline('[ { "table": "a.b", "filters": null } ]'),
    "filters a number": pipeline('[ { "table": "a.b", "filters": 5 } ]'),
    "tidy fields null": filters('{ "filter": "tidyText", "fields": null }'),
    "tables null": pipeline("null"),
    "tables a number": pipeline("5"),
    "tables a number in list": pipeline("[ 5 ]"),
    "table null in list": pipeline("[ null ]"),
    "table empty object": pipeline("[ { } ]"),
    "table name null": pipeline('[ { "table": null } ]'),
    "tables empty": pipeline("[ ]"),
    "table named twice": pipeline('[ { "table": "a.b" }, { "table": "a.b" } ]'),
    "table name capitals": pipeline('[ { "table": "Food.Cafes" } ]'),
    "table name three parts": pipeline('[ { "table": "a.b.c" } ]'),
    "Filters in capitals": pipeline('[ { "table": "a.b", "Filters": [] } ]'),
    "stop rows a fraction": pipeline('[ { "table": "a.b", "stopWhen": { "rowsUnder": 1.5 } } ]'),
    "stop rows as text": pipeline('[ { "table": "a.b", "stopWhen": { "rowsUnder": "2" } } ]'),
    "stop rows huge": pipeline('[ { "table": "a.b", "stopWhen": { "rowsUnder": 3000000000 } } ]'),
    "stop rows exponent": pipeline('[ { "table": "a.b", "stopWhen": { "rowsUnder": 1e2 } } ]'),
    "stop percent as text": pipeline(extra=', "stopWhen": { "droppedOverPercent": "5" }'),
    "stop percent null": pipeline(extra=', "stopWhen": { "droppedOverPercent": null }'),
    "stop unknown key": pipeline(extra=', "stopWhen": { "dropped": 5 }'),
    "stop a number": pipeline(extra=', "stopWhen": 5'),
    "area a number": pipeline(extra=', "area": 5'),
    "area empty": pipeline(extra=', "area": { }'),
    "area as text": pipeline(
        extra=', "area": { "minLongitude": "113.5", "minLatitude": 22.1, "maxLongitude": 113.6, "maxLatitude": 22.2 }'
    ),
    "area null value": pipeline(
        extra=', "area": { "minLongitude": null, "minLatitude": 22.1, "maxLongitude": 113.6, "maxLatitude": 22.2 }'
    ),
    "area backwards": pipeline(extra=', "area": { "minLongitude": 3, "minLatitude": 1, "maxLongitude": 2, "maxLatitude": 2 }'),
    "area unknown key": pipeline(
        extra=', "area": { "minLongitude": 1, "minLatitude": 1, "maxLongitude": 2, "maxLatitude": 2, "name": "x" }'
    ),
    "every file fault": '{ "source": "", "output": "", "tables": [] , "area": { "minLongitude": 3, "minLatitude": 1, "maxLongitude": 2, "maxLatitude": 2 } }',
    "output badly named": '{ "source": "x.gdb", "output": "db/seed/2-x.sql", "tables": [ { "table": "a.b" } ] }',
    "five faults": '{ "source": "x.gdb", "output": "db/seed/parks.sql", "tables": [ { "table": "Food.Cafes", "filters": [ { "filter": "dropDuplicates" }, { "filter": "dropNearDuplicates", "field": "name" } ] }, { "table": "outdoors.parks", "filters": [ { "filter": "insideArea" } ] } ] }',
    "needs an area": filters('{ "filter": "insideArea" }'),
    "null file": "null",
    "a list": "[]",
    "a string": '"x"',
    "empty file": "",
    "white space only": "  \n ",
    "comment only": "// a comment",
    "comments allowed": '// a comment\n{ "source": "x.gdb", /* c */ "output": "db/seed/0002-x.sql", "tables": [ { "table": "a.b" } ] }',
    "cut short": f'{{ {HEAD}, "tables": [ {GOOD_TABLE} ],',
    "open object": f'{{ {HEAD}, "tables": [ {GOOD_TABLE} ]',
    "text after": pipeline() + " x",
    "trailing comma in object": pipeline('[ { "table": "a.b", } ]'),
    "trailing comma in list": pipeline(f"[ {GOOD_TABLE}, ]"),
    "bad literal": pipeline(extra=', "stopWhen": tru'),
    "missing colon": '{ "source" "x.gdb" }',
    "unquoted key": "{ source: 1 }",
    "unterminated string": '{ "source": "x.gdb',
    "line break in string": '{ "source": "x\n.gdb" }',
    "second line fault": '{\n  "source": "x.gdb",\n  "output": 7,\n  "tables": []\n}',
    "third line syntax": '{\n  "source": "x.gdb",\n  "output": "db/seed/0002-x.sql",,\n}',
    "duplicate key": '{ "source": "a.gdb", "source": "x.gdb", "output": "db/seed/0002-x.sql", "tables": [ { "table": "a.b" } ] }',
    "minus alone": '{ "source": - }',
    "settings area": filters('{ "filter": "insideArea" }', '{ "filter": "swapCoordinates" }'),
}
SETTINGS_CASES = {"settings area"}


def run(text: str, settings: bool) -> dict[str, object]:
    folder = Path(tempfile.mkdtemp(prefix="tessera-pipeline-"))
    try:
        (folder / "p.json").write_text(text, encoding="utf-8")
        if settings:
            shutil.copy(SETTINGS, folder / "tessera.json")
        environment = dict(os.environ, DOTNET_ROOT=str(Path(DOTNET).parent))
        done = subprocess.run([DOTNET, CSHARP, "p.json"], cwd=folder, capture_output=True, env=environment, check=False)
        return {"code": done.returncode, "stdout": done.stdout.decode(), "stderr": done.stderr.decode()}
    finally:
        shutil.rmtree(folder)


def main() -> None:
    if not CSHARP:
        sys.exit("Set TESSERA_CSHARP to the C# tool's built .dll.")
    results: list[dict[str, object]] = []
    for name, text in CASES.items():
        settings = name in SETTINGS_CASES
        outcome = run(text, settings)
        results.append({"name": name, "text": text, "settings": settings, **outcome})
        print(f"{name}: exit {outcome['code']}")
    (HERE / "pipelines.json").write_text(json.dumps(results, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
