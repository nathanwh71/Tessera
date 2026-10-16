# The ETL pipeline, built around handlers

**The short answer.** The ***pipeline*** _cleans_ _each_ _table_ with _one_ _chain_ of _steps_, _run_ _between_ _two_ _edges_ that _touch_ the _world_. GDAL **extracts** a _table_ into _rows_. A _chain_ of _handlers_, in Giraffe's _shape_, **transforms** them: _each_ ***filter*** in the _order_ the ***pipeline file*** _lists_ them, then the ***stop rules***. GDAL **loads** the _rows_ that _survive_ as SQL, which DbUp _later_ _runs_ _once_. A _report_ _writes_ the _log_ on every _run_, _stopped_ or not. _Everything_ _between_ the _two_ _edges_ is _pure_, so the _tests_ _check_ the _cleaning_ with _values_ _alone_. A _small_ _generic_ kernel, `Handler<TContext>` in `Pipes.cs`, does the _chaining_, and the _pipeline_'s context, `Cleaning`, _carries_ a _table_'s _rows_, its _log_ and _any_ _reason_ to _stop_. The rebuild of 16 _October_ 2026 _meets_ the _six_ _criteria_ of US-36, _keeps_ the _pipeline_ _file_ as it was, and _writes_ the _same_ _script_ and the _same_ _log_ as the _pipeline_ _before_ it.

## What the pipeline must do

US-36 and ADR 0009 _set_ _six_ _requirements_, and _each_ has _one_ _home_ in the _code_:

| Requirement | Where the code meets it |
|---|---|
| c1: each table runs its filters in order, each on the rows the one before kept | The _chain_: `a >> b` _hands_ `b` the _rows_ `a` _kept_ (`Pipes.cs`) |
| c2: a failing row is dropped, or with `"onFail": "stop"` ends the run, writing no script | `Steps.Filter` _turns_ verdicts into _dropped_ _rows_ or a _stop_ |
| c3: a table that drops over `droppedOverPercent` (10) or keeps under `rowsUnder` (1) stops | `Steps.StopWhen`, the _last_ _step_ of every _table_'s _chain_ |
| c4: every run writes a log: counts, and a line for each row fixed, dropped or stopped at | The _log_ _travels_ in the context, so a _stopped_ _run_ _still_ _holds_ it (`Report.cs`) |
| c5: a misspelt filter, a missing setting or an unknown key: name every fault, run nothing | `PipelineFile.Read` _returns_ every _fault_ at _once_, _before_ _any_ GDAL _step_ |
| c6: a geodatabase table reads its feature class, with curves straightened, heights dropped and coded values as descriptions | The Extract _edge_'s GDAL _options_ (`SeedScript.cs`) |

## The shape: four stages, effects at the edges

```
pipeline file ──read──► Plan ─────────────────────────────────────────────────────┐
                                                                                  │
source ──Extract (GDAL)──► rows ──Transform (pure chain)──► Cleaning ──Load (GDAL)──► SQL script
                                                               │
                                                               └──Report──► log file (every run)
```

_Only_ Extract, _Load_ and _Report_ _touch_ _files_ or _run_ _programs_. _Reading_ the _plan_ and the _whole_ of _Transform_ are ***pure functions***: they _read_ their _inputs_ and _change_ _nothing_ _else_ (ADR 0004). So the _cleaning_, the _stop_ _rules_ and the _plan_'s _checks_ _run_ in _tests_ with no GDAL and no _files_.

## The values

`Rows.cs` _holds_ them:

```csharp
public sealed record Row(int Number, Geometry? Shape, ImmutableDictionary<string, object?> Values);

public enum Act { Kept, Fixed, Dropped, Stopped }
public sealed record Verdict(Act Act, Row Row, string Detail);       // a filter's word on one row
public sealed record LogLine(int Row, string Filter, Act Act, string Detail);   // one line of the log

// The context every step reads and hands on: one table's rows, its log, and any reason to stop.
public sealed record Cleaning(TablePlan Table, int Read, ImmutableArray<Row> Rows,
                              ImmutableArray<LogLine> Log, string? Stop);
```

`Read` _keeps_ the _number_ of _rows_ extracted, which the _stop_ _rules_ _need_ _after_ the filters have _dropped_ _some_. _Each_ _value_ is a read-only _record_, and _each_ _step_ _returns_ a _changed_ _copy_ with `with`.

## The kernel: Giraffe's handler, without the web

Giraffe's `HttpFunc` and `HttpHandler` _become_ _two_ _generic_ delegates in `Pipes.cs`. The _pipeline_ _fills_ them in with `Cleaning`, but the kernel _knows_ _nothing_ of _places_:

```csharp
public delegate TContext Next<TContext>(TContext context);               // the rest of the chain
public delegate Next<TContext> Handler<TContext>(Next<TContext> next);   // a step in front of it

public static class Pipes
{
    extension<TContext>(Handler<TContext>)
    {
        // Giraffe's >=>: run the first, then the second.
        public static Handler<TContext> operator >>(Handler<TContext> first, Handler<TContext> second) =>
            next => first(second(next));
    }

    public static Handler<TContext> Identity<TContext>() => next => next;
    public static Handler<TContext> All<TContext>(IEnumerable<Handler<TContext>> steps) => /* a >> b >> … */;
    public static TContext Run<TContext>(this Handler<TContext> chain, TContext context) => chain(done => done)(context);
}
```

_Two_ _choices_ _differ_ from Giraffe, on _purpose_:

- **No `Task`.** The filters compute and _never_ _wait_, so the _chain_ is _plain_ _functions_. _Only_ the _edges_ _wait_ on GDAL.
- **No `option`.** In Giraffe, `None` _means_ 'this handler does not apply'. No _step_ here _needs_ that: _each_ filter _judges_ every _row_ it is _given_, and a filter that _suits_ _some_ _shapes_ _alone_, such as `repairShapes`, _checks_ the _shape_ _itself_. A _stop_ is not `None` either: a _step_ that _stops_ _returns_ the context **without calling `next`**, with the _reason_ _inside_. That is Giraffe's _early_ _return_, and it _keeps_ the _reason_ and the _log_ for the _report_.

## Steps: filters and stop rules

A filter is _written_ as a _judge_: a _function_ from a _table_'s _rows_ to _one_ verdict for _each_ _row_, in _order_. `Steps.Filter` _lifts_ _any_ _judge_ into a _step_, so every filter _drops_, logs and _stops_ alike:

```csharp
public static Handler<Cleaning> Filter(string name, OnFail onFail, Func<ImmutableArray<Row>, ImmutableArray<Verdict>> judge)
```

A _kept_ or mended _row_ _passes_ on to the _next_ _step_, and a _failed_ _row_ is _dropped_ and logged. With `OnFail.Stop`, the _first_ _failed_ _row_ _ends_ the _run_ _instead_: the _step_ _returns_ the context with the _reason_, and _never_ _calls_ the _steps_ _after_ it. _Six_ filters _judge_ _one_ _row_ at a _time_, so `Steps.EachRow(Func<Row, Verdict>)` _turns_ a _row_ _judge_ into a _table_ _judge_. The _two_ duplicate filters _judge_ a _row_ _against_ the _rows_ _before_ it, so they _write_ a _table_ _judge_ _directly_.

The _stop_ _rules_ are a _step_ _too_, `Steps.StopWhen(rules)`, the _last_ in every _chain_, so a _table_'s _whole_ _cleaning_ is _one_ _value_. `PipelineFile.Read` _builds_ _each_ _table_'s _chain_:

```csharp
var chain = Pipes.All(steps) >> Steps.StopWhen(table.StopWhen ?? pipeline.StopWhen ?? Defaults);
```

The _table_'s _own_ _rules_ _win_ over the _file_'s, and the _file_'s over the _defaults_. The _program_ then _runs_ the _chain_ on _each_ _table_'s _rows_:

```csharp
var done = plan.Chain.Run(Cleaning.Start(table, rows));
```

## The eight filters

_Each_ filter is _one_ _file_ in `Filters/`: its _name_, a _record_ of its _settings_, its _kind_ for the _pipeline_ _file_, and a `Create` _function_ that _wraps_ its _judge_ in `Steps.Filter`. The _judge_ is a _pure_ _function_, so _each_ filter's _tests_ _need_ _only_ _rows_.

| Filter | Judge | Verdicts it gives |
|---|---|---|
| `dropEmptyShapes` | _One_ _row_ | _Fail_ a _row_ with no _shape_ or an _empty_ _one_ |
| `repairShapes` | _One_ _row_ | _Fix_ an invalid _shape_ with NetTopologySuite's `GeometryFixer`; _fail_ _one_ that _comes_ out _empty_ |
| `swapCoordinates` | _One_ _row_ | _Fix_ a _shape_ _outside_ the _area_ that _falls_ _inside_ it with its _numbers_ _swapped_ |
| `insideArea` | _One_ _row_ | _Fail_ a _shape_ that does not _touch_ the _area_'s _box_ |
| `tidyText` | _One_ _row_ | _Fix_ _text_: Unicode NFC _form_, _single_ _spaces_, trimmed _ends_, _empty_ to _null_ |
| `requireFields` | _One_ _row_ | _Fail_ a _row_ _missing_ a _named_ _field_, or _holding_ _only_ _spaces_ there |
| `dropDuplicateIds` | The _table_ | _Fail_ a _row_ that _repeats_ an earlier _row_'s id |
| `dropNearDuplicates` | The _table_ | _Fail_ a _row_ whose _name_ _matches_ an earlier _one_'s _within_ the _given_ _metres_ |

A _new_ filter is _one_ _new_ _file_ and _one_ _line_ in `Filters/All.cs`.

## The edges

| Edge | Where | What it runs |
|---|---|---|
| Extract | `SeedScript.ExtractArguments`, `Gdal.Run` | `ogr2ogr -f GeoJSON` for _each_ _table_, with `-t_srs EPSG:4326`, `-nlt CONVERT_TO_LINEAR`, `-dim XY` and `-resolveDomains` (c6) |
| Load | `SeedScript.LoadArguments`, `SeedScript.Join` | `ogr2ogr -f PGDump` on the _cleaned_ _rows_, then the _tables_ _joined_ into the _next_ _numbered_ _script_ |
| Report | `Report.Log` | The _log_ _beside_ the _scripts_, in `db/seed-logs/`, on every _run_ (c4) |

`Program.cs` _runs_ the _stages_ in _order_ and _ends_ with an exit _code_: 0 when it _wrote_ the _script_, 1 when a _table_ _stopped_ or GDAL _failed_, 2 when the _command_ or the _pipeline_ _file_ is _wrong_. Extract and _Load_ _use_ the _machine_'s _own_ GDAL, or GDAL's Docker _image_ when it has _none_.

## The files

| File | Holds |
|---|---|
| `Pipes.cs` | The _generic_ kernel: the _two_ delegates, `>>`, `Identity`, `All` and `Run` |
| `Rows.cs` | `Row`, `Act`, `Verdict`, `LogLine` and the context, `Cleaning` |
| `Steps.cs` | `Filter`, `EachRow` and `StopWhen` |
| `Judge.cs` | The _measures_ the _judges_ _share_: _inside_ the _area_, blank, _text_, _tidy_ _text_, _metres_ |
| `Filters/*.cs` | The _eight_ filters, _one_ _file_ _each_, and `All.cs`, which _lists_ them |
| `FilterKind.cs`, `Pipeline.cs` | The _pipeline_ _file_'s _format_, _each_ filter's _settings_, and `PipelineFile.Read` |
| `GeoJson.cs`, `Gdal.cs`, `SeedScript.cs` | The _edges_: GDAL's _arguments_, its _runs_, and the _script_ |
| `Report.cs` | The _log_'s _text_ |
| `Program.cs` | The _stages_ in _order_, and the exit _codes_ |

## The tests

The 38 _tests_ in `tests/Tessera.Etl.Tests` _check_ _five_ _things_:

1. **The kernel** (`ChainTests.cs`). `>>` and `All` _run_ _steps_ in _order_; a _step_ can _run_ _code_ _after_ the _rest_ of the _chain_; a _step_ that does not _call_ `next` _ends_ the _chain_.
2. **Each filter alone** (`FilterTests.cs`). _Rows_ in, _rows_ and _log_ out: what _each_ _keeps_, _fixes_ and _drops_ (US-36, _test_ 1).
3. **The stop rules** (`PipelineTests.cs`). A _table_ that _drops_ 2 of 10 _rows_ _stops_ _under_ the _default_ 10 _per_ _cent_, and _passes_ when its _own_ _rule_ _allows_ 25; a _table_'s _own_ _rules_ _win_ over the _file_'s (_test_ 2).
4. **The plan** (`PipelineTests.cs`). A _file_ with _five_ _faults_ _names_ all _five_, and _runs_ _nothing_.
5. **End to end** (`RunTests.cs`). GDAL _builds_ a ***geodatabase*** with _one_ _fault_ of _each_ _kind_; the _run_ _keeps_ 3 of 7 cafés, _swaps_ _one_ _back_, _repairs_ a _crossing_ park, and logs _each_ _row_; a _run_ _set_ to _stop_ _writes_ no _script_ and _still_ _writes_ its _log_ (_test_ 3).

The _first_ _four_ _need_ no GDAL and _run_ in _about_ a _second_.

## What changed in the rebuild

| | Before | Now |
|---|---|---|
| A step | `Filter`: _rows_ in, an outcome out | `Handler<Cleaning>`: _wraps_ the _rest_ of the _chain_ |
| A filter's own code | _Called_ `Judge.Rows` or `Judge.Changes`, which _dropped_ and logged | _Gives_ verdicts; `Steps.Filter` _drops_, logs and _stops_ for all |
| The stop rules | _Checked_ by the _cleaning_ _code_ _after_ the _chain_ | The _last_ _step_ of the _chain_ |
| Code around later steps | Not possible | A _step_ can _run_ _code_ _after_ `next`, such as _timing_ _each_ filter |

The _pipeline_ _file_, the _script_ and the _log_ did not _change_. On the Macau _places_, the rebuilt _pipeline_ _writes_ a _script_ _identical_ to the _old_ _one_, _byte_ for _byte_, and the _same_ _log_, both for a _run_ that _finishes_ and for _one_ _set_ to _stop_.
