# Tessera ETL in Python

**This tool turns a file of places into one SQL seed script, and writes the same script, byte for byte, as Tessera's C# ETL tool.** It reads the same pipeline file, prints the same lines, returns the same exit codes and writes the same log, less the time of the run. On every check below, from 2 rows to 90,000, the two tools agree.

```
uv sync                                                   # once, in this folder
cd /path/to/tessera                                       # the folder that holds tessera.json
uv run --project /path/to/tessera-etl-python tessera-etl data/macau.pipeline.json
```

Run it in the folder that holds `tessera.json`, as the C# tool runs, so filters that need the area find it there. `python -m tessera_etl PIPELINE.json` runs it too.

## The key points

- **The same outcome as the C# tool.** Five acceptance runs match byte for byte: the dates fixture, the geodatabase fixture, the Macau check, `data/macau.pipeline.json`, and the 90,000-place run. 27 further golden cases and 96 faulty pipeline files, each recorded from the C# tool, match too (see [How it is checked](#how-it-is-checked)).
- **It reads sources with pyogrio, not ogr2ogr.** pyogrio reads each layer straight into memory. Every value then goes through the rounding GDAL's GeoJSON writer gave it, since the C# tool cleaned the numbers ogr2ogr wrote, not the source's own.
- **It falls back on ogr2ogr in four cases.** A table with true curves, a table in another coordinate system, a field whose coded domain uses text codes, and a read pyogrio refuses each go through ogr2ogr, as the C# tool did. The result then matches by construction.
- **It reads like F#.** Steps are handlers joined with a real `>>`; a verdict is the union `Keep | Fix | Fail`, taken apart with `match` and closed with `assert_never`; results are `Ok | Err`; every record is frozen.
- **Pure code stays pure.** `kernel` and `core` never touch a file or run a program; import-linter enforces that. Only `shell` reads files, runs pyogrio and ogr2ogr, and prints.
- **The checks pass.** 252 tests (all 84 of the C# tool's, by intent, plus goldens and hypothesis properties), pyright in strict mode, ruff and import-linter.

## What it does

The tool reads a pipeline file, such as:

```json
{
  "source": "data/macau-pois.geojson",
  "output": "db/seed/0002-macau-places-cleaned.sql",
  "tables": [
    { "table": "food.cafes", "where": "layer = 'cafes'",
      "filters": [ { "filter": "insideArea", "onFail": "stop" }, { "filter": "requireFields", "fields": ["id", "name"] } ] }
  ]
}
```

For each table it **extracts** the rows, **transforms** them through the chain of filters and the stop rules, and **loads** them as SQL in the form GDAL's PGDump driver wrote. The script lands at `output` only when every table passes; the log lands beside the scripts' folder, in `db/seed-logs/`, on every run. The exit code is 0 when it wrote the script, 1 when a table stopped or failed, and 2 when the command, the pipeline file, the source or the script's name is wrong.

The eight filters are those of the C# tool: `dropEmptyShapes`, `repairShapes`, `swapCoordinates`, `insideArea`, `tidyText`, `requireFields`, `dropDuplicateIds` and `dropNearDuplicates`.

## How the code is laid out

| Layer | Folder | Holds |
|---|---|---|
| Kernel | `src/tessera_etl/kernel` | `pipes.py`: `Handler`, whose `>>` joins two steps, with `identity`, `chain` and `run`; `result.py`: `Ok`, `Err` and `Result` |
| Core (pure) | `src/tessera_etl/core` | The pipeline file (`plan.py`, `pipeline.py`), the rows and verdicts (`rows.py`), the steps (`steps.py`), the eight filters (`filters/`), the SQL (`table_sql.py`), the log (`report.py`), and the rules that make the text match .NET's and GDAL's (`dotnet.py`, `gdal_text.py`, `json_text.py`, `stj.py`, `fixer.py`) |
| Shell | `src/tessera_etl/shell` | `program.py`, the command; `extract.py`, the reads with pyogrio; `gdal.py`, the ogr2ogr runs |

Each filter is one module in `core/filters/` that declares its `KIND`: a `FilterKind[S]` with its name, its description, the pydantic model `S` of its settings, its `create` function and, for `insideArea` and `swapCoordinates`, a `check` that asks for the area. A new filter is one new module and one line in `core/filters/__init__.py`.

## How the Python build matches the C# build

The C# tool never saw a source's own values: ogr2ogr wrote each table as GeoJSON, and the tool read that text. So the Python build reproduces what that text held.

| What | How the C# tool got it | How the Python build matches it |
|---|---|---|
| Coordinates | ogr2ogr wrote 15 decimals, then trimmed rounding noise | `gdal_text.coordinate`, a port of GDAL 3.8's own code, checked against ogr2ogr on 3,000 numbers |
| Numbers with a fraction | ogr2ogr wrote 17 significant digits, fewer when that dropped a run of 0s or 9s | `gdal_text.real` and `gdal_text.single` |
| Dates and times | GDAL's ISO text, with milliseconds only when there are any | pyogrio reads date-times as text; `gdal_text.date_time` rewrites them as GDAL 3.8 did |
| Coded domains | `-resolveDomains` added a field named `…_resolved` | pyogrio hands whole-number codes over as a dictionary column; text codes go to ogr2ogr |
| A GeoJSON source of one table | GDAL kept each feature's own JSON, so the feature's own `id` came first and a missing property stayed out | the source's JSON is read beside pyogrio, and each row found by its feature id |
| A GeoJSON source of several tables | copied once into a GeoPackage, which each table read | the same copy, made with pyogrio, with dates and lists stored as GDAL's GeoPackage held them |
| Repaired shapes | NetTopologySuite's GeometryFixer | `core/fixer.py`, a port of it on shapely, with NTS's cascaded union tree; it matches NTS on 679 shapes |
| Messages for a faulty pipeline file | System.Text.Json's exceptions | pydantic checks the file; `stj.py` picks the fault .NET met first and writes .NET's sentence, with its path, line and byte |
| Numbers in the log and the SQL | .NET's `R`, `0.#` and `0` formats | `core/dotnet.py`, checked against .NET 10 on 3,052 numbers |

## How it is checked

```
uv run pytest            # 252 tests; the 90,000-place test runs when TESSERA_SCALE_SOURCE names big.geojson
uv run pyright           # strict
uv run ruff check .
uv run lint-imports
```

- **The C# tests.** Each of the 84 keeps its intent, in a test of the same name: `test_chain.py`, `test_filters.py`, `test_all_filters.py`, `test_closed_types.py`, `test_pipeline.py`, `test_table_sql.py`, `test_tables.py`, `test_run.py` and `test_inspect.py`. The last runs the C# repository's `inspect.ts` with Node.js when both are present, and is skipped otherwise.
- **Golden cases.** `tests/fixtures/cases` holds 27 pipeline files, and `tests/golden` the C# tool's outcome for each: exit code, both streams, script and log. They cover the acceptance fixtures, the C# tool's own test geodatabase, coded domains, curves, a projected layer, 3D lines, dates, lists and JSON values, feature ids, stops, refusals, and extraction failures. `tests/golden/pipelines.json` holds 96 faulty pipeline files with the C# tool's words for each. The 90,000-place script is 78 MB, so `tests/golden/scale.json` keeps its checksum and log instead.
- **Properties.** hypothesis, Python's FsCheck, checks the laws of the chain (identity at either end, grouping either way), that a filter keeps or drops every row it reads, that tidy text is settled after one pass, that a laundered name fits 63 bytes, that SQL text reads back, and that the JSON reader agrees with Python's own.

To record the goldens again from the C# tool, build it in Release and run:

```
uv run python tests/golden/make_golden.py
uv run python tests/golden/make_pipeline_golden.py
uv run python tools/compare_with_csharp.py FOLDER[:PIPELINE] ...
```

`tests/fixtures/make_fixtures.py` rebuilds the fixture sources; it needs GDAL's own Python bindings (`/usr/bin/python3.12` with Ubuntu's python3-gdal), since pyogrio cannot write coded domains or curves.

## Where the two builds may still differ

These cases fall outside every check above:

- **The C# tool crashes; the Python tool answers.** A pipeline file whose filter list holds `null`, or whose `requireFields` list holds `null`, crashes the C# tool with a stack trace. The Python tool refuses the first (`'' is no filter`) and reads the second as a field that is always missing. A broken `tessera.json` crashes both, with different traces.
- **GDAL versions.** pyogrio carries GDAL 3.12; the C# tool ran the machine's GDAL 3.8.4. Both type GeoJSON fields alike on every fixture here, but a later GDAL may type a rare form of date differently.
- **A `where` on a date in a GeoJSON source of several tables.** Both builds run it in SQLite, on a GeoPackage copy, but the Python copy stores date-times as GDAL's GeoJSON text, where GDAL's own copy stores its GeoPackage form. A `where` comparing date-times as text may then pick other rows.
- **Unicode.** `tidyText` and `dropNearDuplicates` use Python's Unicode tables, where the C# tool used .NET's. Letters added to Unicode in recent years may normalise or lower-case differently.
- **Speed and memory.** The 90,000-place run takes 20 seconds and 390 MB, where the C# tool takes 15 seconds and 300 MB. A GeoJSON source of one table is also read once more as JSON, for each feature's own id.
- **Docker.** Without a local ogr2ogr, the four fallback cases run GDAL's Docker image, `ghcr.io/osgeo/gdal:alpine-small-3.11.4`, as the C# tool did; that path is untested.
