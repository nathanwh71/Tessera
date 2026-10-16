# Tessera's ETL pipeline

**The pipeline turns a file of places into one SQL seed script, cleaning each table on the way, and writes a log of what it did to every row.** DbUp then _runs_ the _script_ _once_, _through_ `Tessera seed` (ADR 0009). It is Python, in this _folder_, and `npm run etl` _runs_ it from the repository's _root_.

```
npm run etl -- data/macau.pipeline.json                  # from the repository's root
uv run --project etl tessera-etl data/macau.pipeline.json  # the same, without the script's checks
```

_Run_ it from the _folder_ that _holds_ `tessera.json`, so the ***filters*** that _need_ the _area_ _find_ it there. uv, Python's _project_ _tool_, _installs_ Python and the ***pipeline***'s _packages_ on the _first_ _run_.

## The key points

- **One script, or none.** The _script_ _lands_ at the ***pipeline file***'s `output` _only_ when every _table_ _passes_; the _log_ _lands_ in `db/seed-logs/` on every _run_. The exit _code_ is 0 when the _pipeline_ _wrote_ the _script_, 1 when a _table_ _stopped_ or _failed_, and 2 when the _command_, the _pipeline_ _file_, the _source_ or the _script_'s _name_ is _wrong_.
- **It reads sources with pyogrio.** pyogrio _reads_ _each_ ***layer*** _through_ GDAL, _straight_ into _memory_. _Four_ _cases_ _go_ _through_ ogr2ogr, GDAL's _own_ _tool_, _instead_: a _table_ with _true_ curves, a _table_ in another coordinate _system_, a _field_ whose _coded_ domain _uses_ _text_ _codes_, and a _read_ pyogrio _refuses_.
- **It writes SQL as GDAL's PGDump driver does,** with _each_ _name_ laundered, _each_ _column_ _typed_ by the _values_ it _holds_, _dates_ _included_, and _each_ _shape_ as EWKB, PostGIS's binary _form_, in hex.
- **It reads like F#.** _Steps_ are _handlers_ _joined_ with a _real_ `>>`; a verdict is the _union_ `Keep | Fix | Fail`, _taken_ _apart_ with `match` and _closed_ with `assert_never`; _results_ are `Ok | Err`; every _record_ is _frozen_.
- **Pure code stays pure.** `kernel` and `core` _never_ _touch_ a _file_ or _run_ a _program_; import-linter _enforces_ that. _Only_ `shell` _reads_ _files_, _runs_ pyogrio and ogr2ogr, and _prints_.
- **The checks pass.** 252 _tests_, _among_ them golden _cases_ and hypothesis _properties_; pyright in strict _mode_; ruff; and import-linter. `npm test` and the GitHub workflow _run_ all _four_.

## What it does

The _pipeline_ _reads_ a _pipeline_ _file_, such as:

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

For _each_ _table_ it **extracts** the _rows_, **transforms** them _through_ the _chain_ of filters and the ***stop rules***, and **loads** them as SQL. The _eight_ filters are `dropEmptyShapes`, `repairShapes`, `swapCoordinates`, `insideArea`, `tidyText`, `requireFields`, `dropDuplicateIds` and `dropNearDuplicates`.

## How the code is laid out

| Layer | Folder | Holds |
|---|---|---|
| Kernel | `src/tessera_etl/kernel` | `pipes.py`: `Handler`, whose `>>` _joins_ _two_ _steps_, with `identity`, `chain` and `run`; `result.py`: `Ok`, `Err` and `Result` |
| Core (pure) | `src/tessera_etl/core` | The _pipeline_ _file_ (`plan.py`, `pipeline.py`), the _rows_ and verdicts (`rows.py`), the _steps_ (`steps.py`), the _eight_ filters (`filters/`), the SQL (`table_sql.py`), the _log_ (`report.py`), the _shape_ _repair_ (`fixer.py`), and the _rules_ that _fix_ how _text_ is _written_ (`dotnet.py`, `gdal_text.py`, `json_text.py`, `stj.py`) |
| Shell | `src/tessera_etl/shell` | `program.py`, the _command_; `extract.py`, the _reads_ with pyogrio; `gdal.py`, the ogr2ogr _runs_ |

_Each_ filter is _one_ ***module*** in `core/filters/` that declares its `KIND`: a `FilterKind[S]` with its _name_, its _description_, the pydantic _model_ `S` of its _settings_, its `create` _function_ and, for `insideArea` and `swapCoordinates`, a `check` that _asks_ for the _area_. A _new_ filter is _one_ _new_ _module_ and _one_ _line_ in `core/filters/__init__.py`.

## How it reads a source

The _pipeline_ _cleans_ the _values_ as GDAL's GeoJSON writer would _write_ them, so every _source_ _gives_ the _same_ _script_ whichever _way_ it is _read_.

| What | How the pipeline reads it |
|---|---|
| Coordinates | _Rounded_ as GDAL's GeoJSON writer _rounds_ them, to 15 decimals with _rounding_ noise trimmed (`gdal_text.coordinate`, _checked_ _against_ ogr2ogr on 3,000 _numbers_) |
| Numbers with a fraction | 17 significant digits, _fewer_ where that _drops_ a _run_ of 0s or 9s (`gdal_text.real` and `gdal_text.single`) |
| Dates and times | GDAL's ISO _text_, with _milliseconds_ _only_ when there are _any_ (`gdal_text.date_time`) |
| Coded domains | A _coded_ _field_ _gains_ a _second_ _field_, `…_resolved`, _holding_ _each_ _code_'s _description_; whole-number _codes_ _arrive_ from pyogrio as a dictionary _column_, and _text_ _codes_ _go_ _through_ ogr2ogr |
| A GeoJSON source of one table | The _source_'s JSON is _read_ _beside_ pyogrio, so _each_ _feature_'s _own_ `id` _comes_ _first_ and a _missing_ _property_ _stays_ out |
| A GeoJSON source of several tables | _Copied_ _once_ into a GeoPackage, with pyogrio, which _each_ _table_ then _reads_ |
| Invalid shapes | `core/fixer.py` _repairs_ them as NetTopologySuite's `GeometryFixer` does, on shapely, with its cascaded _union_ tree; it agrees with NetTopologySuite on 679 _shapes_ |
| A faulty pipeline file | pydantic _checks_ the _file_; `stj.py` _names_ the _first_ _fault_, with its _path_, _line_ and _byte_ |
| Numbers in the log and the SQL | _Written_ in the shortest _form_ that _reads_ _back_ (`core/dotnet.py`, _checked_ on 3,052 _numbers_) |

## How it is checked

```
uv run pytest            # 252 tests; the 90,000-place test runs when TESSERA_SCALE_SOURCE names big.geojson
uv run pyright           # strict
uv run ruff check .
uv run lint-imports
```

- **The pipeline's rules.** `test_chain.py`, `test_filters.py`, `test_all_filters.py`, `test_closed_types.py`, `test_pipeline.py`, `test_table_sql.py`, `test_tables.py`, `test_run.py` and `test_inspect.py`. The _last_ _runs_ `scripts/inspect.ts` on a ***geodatabase*** and _checks_ that the _pipeline_ _accepts_ the _file_ it _prints_; it _needs_ Node.js, GDAL's `ogrinfo` and the _scripts_' _packages_, and is _skipped_ _without_ them.
- **Golden cases.** `tests/fixtures/cases` _holds_ 27 _pipeline_ _files_, and `tests/golden` the _expected_ outcome of _each_: exit _code_, both _streams_, _script_ and _log_. They _cover_ _dates_, a _test_ geodatabase, _coded_ domains, curves, a _projected_ _layer_, 3D _lines_, _lists_ and JSON _values_, _feature_ ids, _stops_, _refusals_ and extraction _failures_. `tests/golden/pipelines.json` _holds_ 96 faulty _pipeline_ _files_ with the _message_ _each_ must _give_. The 90,000-place _script_ is 78 MB, so `tests/golden/scale.json` _keeps_ its checksum and _log_ _instead_.
- **Properties.** hypothesis _checks_ the _laws_ of the _chain_ (identity at either _end_, _grouping_ either _way_), that a filter _keeps_ or _drops_ every _row_ it _reads_, that _tidy_ _text_ is _settled_ _after_ _one_ _pass_, that a laundered _name_ _fits_ 63 _bytes_, that SQL _text_ _reads_ _back_, and that the JSON _reader_ agrees with Python's _own_.

`tests/fixtures/make_fixtures.py` rebuilds the fixture _sources_; it _needs_ GDAL's _own_ Python bindings (`/usr/bin/python3.12` with Ubuntu's python3-gdal), _since_ pyogrio _cannot_ _write_ _coded_ domains or curves.

## Known limits

- **GDAL's version.** pyogrio _carries_ GDAL 3.12, and the fallback _cases_ _run_ the _machine_'s ogr2ogr. Both _type_ GeoJSON _fields_ alike on every fixture here, but another GDAL may _type_ a _rare_ _form_ of _date_ _differently_.
- **A `where` on a date in a GeoJSON source of several tables.** It _runs_ in SQLite, on the GeoPackage _copy_, which _stores_ date-times as GDAL's GeoJSON _text_; a `where` that _compares_ date-times as _text_ _compares_ that _form_.
- **Unicode.** `tidyText` and `dropNearDuplicates` _use_ Python's Unicode _tables_, so _letters_ _added_ to Unicode in _recent_ _years_ _follow_ Python's _release_.
- **Speed and memory.** The 90,000-place _run_ _takes_ 20 _seconds_ and 390 MB. A GeoJSON _source_ of _one_ _table_ is _read_ _once_ more as JSON, for _each_ _feature_'s _own_ id.
- **Docker.** _Without_ a local ogr2ogr, the fallback _cases_ _run_ GDAL's Docker _image_, `ghcr.io/osgeo/gdal:alpine-small-3.11.4`; that _path_ is untested.
