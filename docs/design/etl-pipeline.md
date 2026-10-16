# The ETL pipeline, built from steps

**The short answer.** The ***pipeline*** _cleans_ _each_ _table_ with _one_ _chain_ of _steps_, _run_ _between_ _two_ _edges_ that _touch_ the _world_. It _lives_ in `etl/`, in Python; `npm run etl` _runs_ it _through_ uv, Python's _project_ _tool_, and it _writes_ the ***seed scripts*** that DbUp _runs_ _once_. pyogrio, a Python _library_ that _reads_ _map_ _files_ _through_ GDAL, **extracts** a _table_ into a GeoDataFrame, geopandas' _table_ of _rows_ with a _column_ of _shapes_. A _chain_ of _steps_ **transforms** it: _each_ ***filter*** in the _order_ the ***pipeline file*** _lists_ them, then the ***stop rules***. _Each_ filter _judges_ the _whole_ _table_ at _once_ and _returns_ a _new_ _one_. The _pipeline_ **loads** the _rows_ that _survive_ as SQL. A _report_ _writes_ the _log_ on every _run_, _stopped_ or not. _Everything_ _between_ the _two_ _edges_ is _pure_, so the _tests_ _check_ the _cleaning_ with _values_ _alone_. A _small_ _generic_ kernel, `Step` in `kernel/pipes.py`, does the _chaining_, and the _pipeline_'s context, `Cleaning`, _carries_ a _table_, its _log_ and _any_ _reason_ to _stop_. The _pipeline_ _meets_ the _six_ _criteria_ of US-36, and its 253 _tests_ _check_ _each_ _one_.

A _step_ is a _function_ from the context to a _changed_ _copy_ of it. _Steps_ _join_ with `>>`, Python's shift operator, which the kernel _borrows_ to _mean_ 'then', as F#'s `>>` composes _two_ _functions_.

## What the pipeline must do

US-36 and ADR 0009 _set_ _six_ _requirements_, and _each_ has _one_ _home_ in the _code_. Every _path_ _below_ _lies_ _under_ `src/tessera_etl/`.

| Requirement | Where the code meets it |
|---|---|
| c1: each table runs its filters in order, each on the rows the one before kept | The _chain_: `a >> b` _runs_ `b` on the _table_ `a` _left_ (`kernel/pipes.py`) |
| c2: a failing row is dropped, or with `"onFail": "stop"` ends the run, writing no script | `filter_step` in `core/steps.py` _turns_ verdicts into _dropped_ _rows_ or a _stop_ |
| c3: a table that drops over `droppedOverPercent` (10) or keeps under `rowsUnder` (1) stops | `stop_when` in `core/steps.py`, the _last_ _step_ of every _table_'s _chain_ |
| c4: every run writes a log: counts, and a line for each row fixed, dropped or stopped at | The _log_ _travels_ in the context, so a _stopped_ _run_ _still_ _holds_ it; `core/report.py` _writes_ its _text_ |
| c5: a misspelt filter, a missing setting or an unknown key: name every fault, run nothing | `read` in `core/pipeline.py` _returns_ every _fault_ at _once_, _before_ _any_ _table_ is _read_ |
| c6: a geodatabase table reads its feature class, with curves straightened, heights dropped and coded values as descriptions | The Extract _edge_, `shell/extract.py`, with ogr2ogr's _options_ in `core/seed_script.py` |

## The shape: four stages, effects at the edges

```
pipeline file ──read──► Plan ──────────────────────────────────────────────────────┐
                                                                                   │
source ──Extract (pyogrio)──► table ──Transform (pure chain)──► Cleaning ──Load──► SQL script
                                                                   │
                                                                   └──Report──► log file (every run)
```

_Only_ Extract, _Load_ and _Report_ _touch_ _files_ or _run_ _programs_. _Reading_ the _plan_ and the _whole_ of _Transform_ are ***pure functions***: they _read_ their _inputs_ and _change_ _nothing_ _else_ (ADR 0004). So the _cleaning_, the _stop_ _rules_ and the _plan_'s _checks_ _run_ in _tests_ with no GDAL and no _files_.

The _code_ _keeps_ that _split_ in _three_ _packages_. `kernel/` _holds_ the _chain_ and `Result`; `core/` _holds_ every _rule_ and _touches_ no _file_; `shell/` _reads_ _files_, _runs_ pyogrio and ogr2ogr, and _prints_. import-linter, a _tool_ that _checks_ which ***module*** may _import_ which, _enforces_ _three_ _contracts_: `kernel` and `core` may not _import_ pyogrio, `pathlib`, `os`, `subprocess` or the _shell_; `kernel` may not _import_ a _table_ or a _shape_, so it _stays_ _generic_; and _each_ _package_ may _call_ _only_ the _one_ _below_ it. The _core_ may _import_ geopandas, _since_ a _table_ in _memory_ _touches_ no _file_.

## The values

`core/rows.py` _holds_ them. _Each_ _record_ is a _frozen_ dataclass: a _record_ whose _fields_ _cannot_ _change_ _after_ it is _made_.

```python
type Value = str | int | float | bool | None   # a value in a row; each column keeps Python's own

# A filter's word on one row it does not pass as it is: one of two cases, as an F# union would be.
@dataclass(frozen=True, slots=True)
class Fixed:
    row: int                          # the row's number in the source, counting from 1
    did: str

@dataclass(frozen=True, slots=True)
class Failed:
    row: int
    reason: str

type Verdict = Fixed | Failed

@dataclass(frozen=True, slots=True)
class Judged:                         # what a filter made of a table
    frame: GeoDataFrame               # every row, the mended ones mended
    verdicts: tuple[Verdict, ...]     # in row order

@dataclass(frozen=True, slots=True)
class LogLine:                        # one line of the log
    row: int
    filter: str
    act: Act                          # FIXED, DROPPED or STOPPED
    detail: str

# The context every step reads and hands on: one table, its log, and any reason to stop.
@dataclass(frozen=True, slots=True)
class Cleaning:
    table: TablePlan
    read: int
    frame: GeoDataFrame
    log: tuple[LogLine, ...]
    stop: str | None
```

The _table_'s index _numbers_ _each_ _row_ as the _source_ does, so the _log_ _names_ a _row_ by the _number_ it had in the _source_, _whatever_ the filters _dropped_ _before_ it. _Each_ _column_ of _values_ is an 'object' _column_, which _holds_ Python's _own_ _values_: a _column_ may mix _text_ with _numbers_, and a _whole_ _number_ _stays_ _whole_ _beside_ a _null_.

A verdict is a _union_: _always_ _exactly_ _one_ of its _two_ _cases_, _each_ _carrying_ _only_ what it _needs_, and a _row_ with no verdict _passes_ as it is. _Code_ _takes_ a verdict _apart_ with `match`, and _each_ `match` _ends_ with `assert_never`, so pyright, the _type_ checker, _refuses_ _one_ that misses a _case_. pyright _cannot_ _notice_ a _record_ meant as a _case_ but _left_ out of the _union_, so `tests/test_closed_types.py` _checks_ that the _union_ and the _records_ agree.

`read` _keeps_ the _number_ of _rows_ extracted, which the _stop_ _rules_ _need_ _after_ the filters have _dropped_ _some_. A _step_ _never_ _changes_ a _value_ or a _table_; it _returns_ a _changed_ _copy_, _made_ with `dataclasses.replace`.

## The kernel: steps joined with >>

`kernel/pipes.py` _holds_ _one_ _generic_ _type_. The _pipeline_ _fills_ it in with `Cleaning`, but the kernel _knows_ _nothing_ of _places_:

```python
class Stoppable(Protocol):                   # a context that can carry a reason to stop
    @property
    def stop(self) -> str | None: ...

@dataclass(frozen=True, slots=True)
class Step[C: Stoppable]:
    apply: Callable[[C], C]

    def __call__(self, context: C) -> C:
        return self.apply(context)

    def __rshift__(self, then: "Step[C]") -> "Step[C]":
        def both(context: C) -> C:
            done = self(context)
            return done if done.stop is not None else then(done)
        return Step(both)
```

A _stop_ is a context with its _reason_ _inside_: `>>` _runs_ no _step_ _after_ it, and the _reason_ and the _log_ _reach_ the _report_. _Steps_ _never_ _wait_, so the _chain_ is _plain_ _functions_, with no `async`. _Only_ the _edges_ _wait_, on _files_ and on GDAL.

`core/steps.py` _gives_ the _chain_ its _start_, `IDENTITY`, the _step_ that _changes_ _nothing_, and `chain`, which _folds_ a _list_ of _steps_ into _one_ with `functools.reduce(operator.rshift, steps, IDENTITY)`. A _list_ of no filters is then a _chain_ _too_. hypothesis _checks_ the _two_ _laws_ that _make_ the _fold_ _safe_: `IDENTITY` may _stand_ at either _end_ of a _chain_, and `(a >> b) >> c` _runs_ as `a >> (b >> c)` does.

## Steps: filters and stop rules

A filter is _written_ as a _judge_: a _function_ from a _table_ to a `Judged`, the _table_ with _each_ mended _row_ mended and a verdict for _each_ _row_ it touched. `filter_step` _lifts_ _any_ _judge_ into a _step_, so every filter _drops_, logs and _stops_ alike:

```python
type Judge = Callable[[GeoDataFrame], Judged]

def filter_step(name: str, on_fail: OnFail, judge: Judge) -> Step[Cleaning]: ...
```

A _judge_ _builds_ its verdicts from masks, _columns_ of _true_ and _false_, _one_ for _each_ _row_: `fixes` and `failures` _turn_ a mask into verdicts, and `in_row_order` merges _two_ _kinds_. A mended _row_ _passes_ on to the _next_ _step_, and a _failed_ _row_ is _dropped_ and logged. With `OnFail.STOP`, the _first_ _failed_ _row_ _ends_ the _run_ _instead_: the _step_ _returns_ the context with the _reason_, so no _later_ _step_ _runs_.

The _stop_ _rules_ are a _step_ _too_, `stop_when(rules)`, the _last_ in every _chain_, so a _table_'s _whole_ _cleaning_ is _one_ _value_. `read` in `core/pipeline.py` _builds_ _each_ _table_'s _chain_:

```python
rules = table.stop_when or pipeline.stop_when or DEFAULTS
joined = chain([*steps, stop_when(rules)])
```

The _table_'s _own_ _rules_ _win_ over the _file_'s, and the _file_'s over the _defaults_. `load` in `core/tables.py` then _runs_ the _chain_ on _each_ _table_:

```python
done = plan.chain(Cleaning.start(plan.table, extract.frame))
```

## The pipeline file

pydantic, a _library_ that _checks_ _data_ _against_ a declared _shape_, _reads_ the _pipeline_ _file_ into _frozen_ _models_ with camelCase _keys_ (`core/plan.py`), and _refuses_ a _key_ a _model_ _lacks_, so a _misspelt_ _setting_ _cannot_ _pass_ unseen. Python's JSON _reader_ _reads_ the _text_, _once_ `without_comments` has _made_ _each_ `//` or `/* */` _comment_ a _space_. _Each_ _fault_ pydantic _finds_ _becomes_ _one_ _plain_ _line_ that _names_ the _table_, the filter and the _key_, such as `a.b: requireFields: fields: Input should be a valid tuple.`. A _number_ may _come_ as _text_, such as `"25"`, and `onFail` in _any_ _case_, or as a _number_, 1 for _stop_.

## The eight filters

_Each_ filter is _one_ _module_ in `core/filters/`. It declares its `KIND`: a `FilterKind` with the filter's _name_, what it does, the pydantic _model_ of its _settings_, the _function_ that _builds_ its _step_, and, for `insideArea` and `swapCoordinates`, a _check_ that _asks_ for the _area_. _Each_ _module_ _also_ has a `create` _function_ that _builds_ the filter from _plain_ _values_, and _wraps_ its _judge_ in `filter_step`. The _judge_ is a _pure_ _function_, so _each_ filter's _tests_ _need_ _only_ a _table_. _Each_ _works_ on _whole_ _columns_, in the idiom _each_ _kind_ of _work_ _suits_.

| Filter | How it judges the table | Verdicts it gives |
|---|---|---|
| `dropEmptyShapes` | A mask of _missing_ and _empty_ _shapes_ | _Fail_ a _row_ with no _shape_ or an _empty_ _one_ |
| `repairShapes` | shapely's array _functions_: `is_valid`, then `make_valid` by its 'structure' _method_ | _Fix_ an invalid _shape_; _fail_ _one_ that _comes_ out _empty_ |
| `swapCoordinates` | shapely's `transform`, which _swaps_ the _two_ _numbers_ of every _point_ at _once_ | _Fix_ a _shape_ _outside_ the _area_ that _falls_ _inside_ it with its _numbers_ _swapped_ |
| `insideArea` | A mask from _each_ _shape_'s _box_, then `intersects` for _any_ _shape_ _longer_ than a _point_ | _Fail_ a _shape_ that does not _touch_ the _area_'s _box_ |
| `tidyText` | pandas' `.str` _methods_, on the _text_ that _needs_ tidying | _Fix_ _text_: Unicode NFC _form_, _single_ _spaces_, trimmed _ends_, _empty_ to _null_ |
| `requireFields` | A mask of blank _values_ for _each_ _field_, the _first_ _missing_ _field_ _named_ | _Fail_ a _row_ _missing_ a _named_ _field_, or _holding_ _only_ _spaces_ there |
| `dropDuplicateIds` | A mask of ids `duplicated` _marks_ | _Fail_ a _row_ that _repeats_ an earlier _row_'s id |
| `dropNearDuplicates` | The ***spatial index***, a tree of _boxes_, _finds_ the _near_ _places_; _one_ _pass_ in _row_ _order_ _settles_ _each_ | _Fail_ a _row_ whose _name_ _matches_ an earlier _kept_ _row_'s _within_ the _given_ _metres_ |

`core/judge.py` _holds_ the _measures_ the filters _share_: _inside_ the _area_, blank, _text_, _tidy_ _text_, folded _names_ and _metres_ along the Earth. pandas may _hold_ _text_ in Arrow's _form_, whose _patterns_ _know_ _only_ ASCII's _white_ _space_, so every _measure_ _turns_ its _column_ into Python's _own_ _strings_ _first_, and Python's _rules_ _decide_. A _new_ filter is _one_ _new_ _module_, _modelled_ on `inside_area.py`, and _one_ _line_ in `KINDS`, in `core/filters/__init__.py`.

## The edges

| Edge | Where | What it runs |
|---|---|---|
| Copy | `seed_script.copy_first`, `extract.open_source` | For a GeoJSON _source_ of _several_ _tables_, _one_ _copy_ into a GeoPackage, so _each_ _table_ _reads_ an indexed _copy_, not the _whole_ _file_ _again_ |
| Extract | `extract.read_table`, `gdal.run` | pyogrio _reads_ _each_ _table_ into _memory_, _flat_ and in longitude and latitude, with _each_ _coded_ _value_'s _description_ in a _field_ _named_ _after_ it with `_resolved` _added_ (c6); the _program_ _reads_ the _next_ _table_ _while_ it _cleans_ the _current_ _one_ |
| Load | `seed_script.head`, `tables.load`, `table_sql.statements` | No GDAL: the _program_ _writes_ _each_ _table_'s SQL from its _cleaned_ _rows_, in the _form_ of GDAL's PGDump _driver_, _one_ _table_ at a _time_, into the _script_ in the _work_ _folder_, and _moves_ the _script_ to its _place_ when _whole_ |
| Report | `report.log` | The _log_ _beside_ the _scripts_, in `db/seed-logs/`, on every _run_ _past_ its _checks_ (c4), from _each_ _table_'s `Outcome`: its _counts_ and _log_ _lines_, _without_ its _rows_ |

_Three_ _cases_ _go_ _through_ GDAL's ogr2ogr, with the _options_ `seed_script.extract_arguments` _builds_: `-t_srs EPSG:4326`, `-nlt CONVERT_TO_LINEAR`, `-dim XY` and `-resolveDomains` (c6). They are a _table_ with _true_ curves, such as a ***geodatabase***'s arcs, which ogr2ogr straightens; a _field_ whose _coded_ _values_ _use_ _text_ _codes_, whose _words_ pyogrio does not _hand_ over; and a _table_ in another coordinate _system_, _since_ pyproj, the Python _library_, _moves_ _points_ by _other_ _last_ digits than GDAL, and the _values_ would _change_. ogr2ogr _writes_ the _table_ into a GeoPackage of its _own_, which pyogrio then _reads_. Every _value_ _goes_ _through_ the _rounding_ GDAL's GeoJSON writer _gives_ it (`core/gdal_text.py`), so a _table_ _reads_ the _same_ by either _path_. `gdal.run` _uses_ the _machine_'s _own_ ogr2ogr, or GDAL's Docker _image_, `ghcr.io/osgeo/gdal:alpine-small-3.11.4`, when it has _none_.

`load` in `core/tables.py` does a _table_'s _work_ _between_ its extract and the _script_: it _cleans_ the _rows_ and _writes_ the SQL, and _returns_ the SQL or a `Failure`. It _reads_ no _file_, so the _tests_ _call_ it with _tables_ of their _own_. `Failure`, in `core/failure.py`, is a _union_ _like_ `Verdict`: the _table_ could not be extracted, a filter or the _stop_ _rules_ _stopped_ the _run_, or the SQL could not _hold_ the _rows_. The _last_ _two_ _carry_ the _table_'s _counts_, so the _log_ _keeps_ them.

`shell/program.py` does the _reading_ and _writing_ _around_ it. argparse _reads_ its _one_ _argument_. It _runs_ the _stages_ in _order_, _turns_ _each_ `Failure` into its _message_, and _ends_ with an exit _code_: 0 when it _wrote_ the _script_, 1 when a _table_ _stopped_ or _failed_, 2 when the _command_, the _pipeline_ _file_, the _source_ or the _script_'s _name_ is _wrong_. It _refuses_ a _script_ that _already_ _exists_, _since_ DbUp _knows_ a _script_ by its _name_ and _runs_ it _once_.

**Size.** The _run_ _holds_ _two_ _tables_' _rows_ at _most_: the _one_ it _cleans_ and the _next_, which it _reads_ meanwhile. _Once_ a _table_'s SQL is _written_, _only_ its `Outcome` _stays_ for the _log_. _Each_ _table_ is _cleaned_ _whole_ in _memory_, _since_ the duplicate filters _compare_ _rows_ _across_ the _table_ and the _stop_ _rules_ _count_ its losses. The _pipeline_'s README _gives_ 15 _seconds_ and 400 MB for 90,000 _places_, _nine_ _tables_ of 10,000.

## The files

Every _path_ _lies_ _under_ `src/tessera_etl/`.

| File | Holds |
|---|---|
| `kernel/pipes.py` | The _generic_ kernel: `Step`, with its `>>` |
| `kernel/result.py` | `Ok`, `Err` and `Result`, with `map_ok`, `map_err` and `bind` |
| `core/rows.py` | The _table_ and its helpers, `Act`, `Verdict`, `Judged`, `LogLine`, `Outcome` and the context, `Cleaning` |
| `core/tables.py`, `core/failure.py` | `load`, a _table_'s _work_ from _rows_ to SQL, and the _ways_ it can _fail_ |
| `core/steps.py` | `IDENTITY`, `chain`, `filter_step` with the masks' helpers, and `stop_when` |
| `core/judge.py` | The _measures_ the _judges_ _share_ |
| `core/filters/*.py` | The _eight_ filters, _one_ _module_ _each_, and `__init__.py`, which _lists_ them in `KINDS` |
| `core/filter_kind.py`, `core/plan.py`, `core/pipeline.py` | The _pipeline_ _file_'s _format_, _each_ filter's _settings_, and `read` |
| `core/geojson.py`, `core/seed_script.py`, `core/table_sql.py` | _Features_ into a _table_, ogr2ogr's _arguments_ and the _script_'s _head_, and _each_ _table_'s SQL |
| `core/report.py` | The _log_'s _text_ |
| `core/gdal_text.py`, `core/shapes.py` | GDAL's _rounding_ of _values_, and _typed_ _calls_ to shapely |
| `shell/program.py` | The _command_, `tessera-etl PIPELINE.json`: the _files_ and the _script_ _around_ `load`, and the exit _codes_ |
| `shell/extract.py`, `shell/gdal.py` | The _reads_ with pyogrio, and the _runs_ of ogr2ogr |

## The tests

The 253 _tests_ in `tests/` _check_ _nine_ _things_. `uv run pytest` _runs_ them all; _one_, the 90,000-place _run_, _waits_ for `TESSERA_SCALE_SOURCE` to _name_ its _source_.

1. **The kernel** (`test_chain.py`, 7 _tests_). `>>` _runs_ _steps_ in _order_, and no _step_ _after_ a _stop_; `chain` _joins_ a _list_ as `>>` does; filters _run_ on what the _one_ _before_ _kept_.
2. **Each filter alone** (`test_filters.py` and `test_all_filters.py`, 21). A _table_ in, a _table_ and _log_ out: what _each_ _keeps_, _fixes_ and _drops_ (US-36, _test_ 1); every filter _passes_ _clean_ _places_ as they are; and `KINDS` _lists_ every filter _module_, _each_ _under_ a _name_ of its _own_.
3. **The plan, the stop rules and the log** (`test_pipeline.py`, 17). A _file_ with _five_ _faults_ _names_ all _five_, and _runs_ _nothing_. A _table_ that _drops_ 2 of 10 _rows_ _stops_ _under_ the _default_ 10 _per_ _cent_, and _passes_ when its _own_ _rule_ _allows_ more; a _table_'s _own_ _rules_ _win_ over the _file_'s (_test_ 2). ogr2ogr's _arguments_ are _checked_ _without_ _running_ GDAL.
4. **The SQL** (`test_table_sql.py`, 35). A _table_ _comes_ out in the _form_ of GDAL's PGDump _driver_, with its _names_, _types_, _dates_ and _shapes_.
5. **One table** (`test_tables.py`, 4). A _table_ in, SQL or a `Failure` out, _each_ _way_ a _table_ can _end_, with no GDAL.
6. **The unions** (`test_closed_types.py`, 2). Every _record_ meant as a _case_ of `Verdict` or `Failure` is _one_.
7. **End to end** (`test_run.py`, 2, and `test_inspect.py`, 1). A geodatabase _holds_ _seven_ cafés with _one_ _fault_ _each_ and _two_ parks; the _run_ _keeps_ 3 of the cafés, _swaps_ _one_ _back_, _repairs_ a park whose _edge_ _crosses_ _itself_, and logs _each_ _row_; a _run_ _set_ to _stop_ _writes_ no _script_ and _still_ _writes_ its _log_ (_test_ 3). `test_inspect.py` _checks_ that the _starting_ _pipeline_ _file_ `npm run inspect` _prints_ _passes_ the _pipeline_'s _reader_; it _needs_ Node.js and the Tessera repository, and skips _without_ them.
8. **Recorded outcomes** (`test_golden.py`, 128). `tests/fixtures/cases` _holds_ 27 _pipeline_ _files_, and `tests/golden` the _expected_ outcome of _each_: exit _code_, both _streams_, _script_ and _log_, _matched_ _byte_ for _byte_. `tests/golden/pipelines.json` _holds_ 96 faulty _pipeline_ _files_, _each_ with its _expected_ _message_. `tests/golden/make_golden.py` _records_ them _again_; `tools/compare_meaning.py` _checks_ _each_ _changed_ _script_'s _meaning_ _first_.
9. **Rounding and properties** (`test_gdal_text.py` and `test_properties.py`, 36). GDAL's _rounding_, _checked_ _against_ the _machine_'s ogr2ogr on 3,000 _numbers_ when it has _one_; and, _through_ hypothesis, a _library_ that _makes_ _up_ _inputs_ and shrinks _any_ _failure_ to its _smallest_ _form_, the _laws_ of the _chain_, that a filter _keeps_ or _drops_ every _row_ it _reads_, that _tidy_ _text_ is _settled_ _after_ _one_ _pass_, that `dropNearDuplicates` _fails_ the _rows_ a _comparison_ of every _pair_ _fails_, and that SQL _text_ _reads_ _back_.

_Beside_ the _tests_, pyright _checks_ the _types_ in strict _mode_, ruff _checks_ the _style_, and import-linter _checks_ the _three_ _contracts_ _between_ the _packages_.
