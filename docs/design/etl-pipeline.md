# Designing the ETL pipeline from scratch

**The short answer.** The ***pipeline*** is _one_ _value_, a _chain_ of _steps_, _run_ on _each_ _table_ _between_ _two_ _edges_ that _touch_ the _world_. GDAL **extracts** a _table_ into _rows_. A _chain_ of _handlers_, in Giraffe's _shape_, **transforms** them: _each_ ***filter***, then the ***stop rules***. GDAL **loads** the _rows_ that _survive_ as SQL. A _report_ _writes_ the _log_ on every _run_, _stopped_ or not. _Everything_ _between_ the _two_ _edges_ is _pure_, so the _tests_ _check_ the _cleaning_ with _values_ _alone_. A _small_ _generic_ kernel, `Handler<TContext>`, does the _chaining_, and the ETL's context, `Run`, _carries_ a _table_'s _rows_, its _log_ and _any_ _reason_ to _stop_. The _design_ _meets_ the _six_ _criteria_ of US-36 and _keeps_ _today_'s ***pipeline file*** as it is. Its _core_ _types_ are _built_ and _tested_ in a sketch; the _whole_ would _come_ to _about_ 800 _lines_.

## What the pipeline must do

US-36 and ADR 0009 _set_ _six_ _requirements_, and _each_ has _one_ _home_ in the _design_:

| Requirement | Where the design meets it |
|---|---|
| c1: each table runs its filters in order, each on the rows the one before kept | The _chain_: `a >> b` _hands_ `b` the _rows_ `a` _kept_ |
| c2: a failing row is dropped, or with `"onFail": "stop"` ends the run, writing no script | `Filter` _turns_ verdicts into _dropped_ _rows_ or a _stop_ |
| c3: a table that drops over `droppedOverPercent` (10) or keeps under `rowsUnder` (1) stops | `StopWhen`, the _last_ _step_ of every _table_'s _chain_ |
| c4: every run writes a log: counts, and a line for each row fixed, dropped or stopped at | The _log_ _travels_ in the context, so a _stopped_ _run_ _still_ _holds_ it |
| c5: a misspelt filter, a missing setting or an unknown key: name every fault, run nothing | _Reading_ the _pipeline_ _file_ _returns_ every _fault_ at _once_, _before_ _any_ GDAL _step_ |
| c6: a geodatabase table reads its feature class, with curves straightened, heights dropped and coded values as descriptions | The Extract _edge_'s GDAL _options_ |

## The shape: four stages, effects at the edges

```
pipeline file ──read──► Plan ─────────────────────────────────────────────┐
                                                                          │
source ──Extract (GDAL)──► rows ──Transform (pure chain)──► Run ──Load (GDAL)──► SQL script
                                                             │
                                                             └──Report──► log file (every run)
```

_Only_ Extract, _Load_ and _Report_ _touch_ _files_ or _run_ _programs_. _Reading_ the _plan_ and the _whole_ of _Transform_ are ***pure functions***: they _read_ their _inputs_ and _change_ _nothing_ _else_ (ADR 0004). So the _cleaning_, the _stop_ _rules_ and the _plan_'s _checks_ _run_ in _tests_ with no GDAL and no _files_.

## The values

```csharp
public sealed record Row(int Number, Geometry? Shape, ImmutableDictionary<string, object?> Values);

public enum Act { Kept, Fixed, Dropped, Stopped }
public sealed record Verdict(Act Act, Row Row, string Detail = "");   // a filter's word on one row
public sealed record Event(int Row, string Step, Act Act, string Detail);   // one line of the log
public sealed record Stop(string Step, string Reason);

/// The context every step reads and hands on: one table's rows, its log, and any reason to stop.
public sealed record Run(TablePlan Table, int Read, ImmutableArray<Row> Rows,
                         ImmutableArray<Event> Log, Stop? Stop = null);
```

`Read` _keeps_ the _number_ of _rows_ extracted, which the _stop_ _rules_ _need_ _after_ the filters have _dropped_ _some_. _Each_ _value_ is a read-only _record_, and _each_ _step_ _returns_ a _changed_ _copy_ with `with`.

## The kernel: Giraffe's handler, without the web

Giraffe's `HttpFunc` and `HttpHandler` _become_ _two_ _generic_ delegates. The ETL instantiates them with `Run`, but the kernel _knows_ _nothing_ of the ETL:

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

    public static TContext Run<TContext>(this Handler<TContext> handler, TContext context) =>
        handler(c => c)(context);

    // A step that applies only when the test holds; otherwise the context passes straight on.
    public static Handler<TContext> When<TContext>(Func<TContext, bool> test, Handler<TContext> handler) =>
        next => context => test(context) ? handler(next)(context) : next(context);

    public static Handler<TContext> All<TContext>(IEnumerable<Handler<TContext>> handlers) =>
        handlers.Aggregate((Handler<TContext>)(next => next), (chain, h) => chain >> h);
}
```

_Two_ _choices_ _differ_ from Giraffe, on _purpose_:

- **No `Task`.** The filters compute and _never_ _wait_, so the _chain_ is _plain_ _functions_. _Only_ the _edges_ _wait_ on GDAL.
- **No `option`.** In Giraffe, `None` _means_ 'this handler does not apply', which `choose` _needs_. Here `When` _covers_ that _need_ _without_ a _null_. A _stop_ is not `None` either: a _step_ that _stops_ _returns_ the context **without calling `next`**, with the _reason_ _inside_. That is Giraffe's _early_ _return_, and it _keeps_ the _reason_ and the _log_ for the _report_.

## Steps: filters and stop rules

A filter is _written_ as a _judge_: a _function_ from a _table_'s _rows_ to _one_ verdict for _each_ _row_, in _order_. _One_ _function_ _lifts_ _any_ _judge_ into a _step_:

```csharp
public static Handler<Run> Filter(string name, Func<ImmutableArray<Row>, ImmutableArray<Verdict>> judge, OnFail onFail) =>
    next => run =>
    {
        var verdicts = judge(run.Rows);
        var failed = verdicts.FirstOrDefault(v => v.Act == Act.Dropped);
        if (failed is not null && onFail == OnFail.Stop)          // c2: stop, and call nothing after
            return run with
            {
                Log = run.Log.Add(new(failed.Row.Number, name, Act.Stopped, failed.Detail)),
                Stop = new(name, $"row {failed.Row.Number}: {failed.Detail}"),
            };
        return next(run with                                       // c1: the next step sees what is kept
        {
            Rows = [.. verdicts.Where(v => v.Act != Act.Dropped).Select(v => v.Row)],
            Log = run.Log.AddRange(verdicts.Where(v => v.Act != Act.Kept)
                                           .Select(v => new Event(v.Row.Number, name, v.Act, v.Detail))),
        });
    };
```

_Most_ filters _judge_ _one_ _row_ at a _time_, so `EachRow(Func<Row, Verdict>)` _turns_ a _row_ _judge_ into a _table_ _judge_. The _two_ duplicate filters _judge_ a _row_ _against_ the _rows_ _before_ it, so they _write_ a _table_ _judge_ _directly_.

The _stop_ _rules_ are a _step_ _too_, the _last_ in every _chain_, so a _table_'s _whole_ life is _one_ _value_:

```csharp
public static Handler<Run> StopWhen(double droppedOverPercent, int rowsUnder) => next => run =>
{
    var dropped = run.Read == 0 ? 0 : 100.0 * (run.Read - run.Rows.Length) / run.Read;
    return dropped > droppedOverPercent ? run with { Stop = new("stopWhen", $"dropped {dropped:0.#} % of {run.Read} rows") }
         : run.Rows.Length < rowsUnder ? run with { Stop = new("stopWhen", $"kept {run.Rows.Length} rows") }
         : next(run);
};
```

A _table_'s _chain_ is then, for _example_:

```csharp
var cafes = Filter("dropEmptyShapes", EachRow(DropEmptyShapes.Judge), OnFail.Stop)
         >> When(run => run.Table.Lines, Filter("repairShapes", EachRow(RepairShapes.Judge), OnFail.Drop))
         >> Filter("swapCoordinates", EachRow(SwapCoordinates.Judge(macau)), OnFail.Drop)
         >> Filter("dropDuplicateIds", DropDuplicateIds.Judge("id"), OnFail.Stop)
         >> StopWhen(droppedOverPercent: 10, rowsUnder: 1);
```

## The eight filters

_Each_ filter is _one_ _file_ in `Filters/`: its _name_, a _record_ of its _settings_, and its _judge_. The _judge_ is a _pure_ _function_, so _each_ filter's _tests_ _need_ _only_ _rows_.

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

A _new_ filter is _one_ _new_ _file_ and _one_ _line_ in the _list_ of _kinds_ _below_. _Nothing_ _else_ _changes_.

## From the pipeline file to the chains

The _pipeline_ _file_ _keeps_ _today_'s _format_, so `data/macau.pipeline.json` _runs_ _unchanged_. _Reading_ it is _pure_ and _returns_ either a _plan_ or **every** _fault_ it _found_ (c5):

```csharp
public sealed record FilterKind(string Name, Func<JsonObject, Area?, Result<Handler<Run>, ImmutableArray<string>>> Make);

public static Result<Plan, ImmutableArray<string>> Read(string json, Area? area);
```

_Each_ _kind_ _reads_ its _own_ _settings_ with `System.Text.Json`, _set_ to _refuse_ _unknown_ _keys_, so a _misspelt_ _key_ or a _missing_ _setting_ _comes_ _back_ as a _named_ _fault_. `Read` _gathers_ the _faults_ of every _table_ and every filter _before_ it _answers_. _Only_ a _plan_ with no _faults_ _reaches_ GDAL. _Each_ _table_'s _list_ of filters _folds_ into _one_ _chain_ with `Pipes.All`, and `StopWhen` _goes_ _last_, with the _table_'s _own_ _rule_ or the _file_'s.

## The edges

| Edge | What it runs | Its result |
|---|---|---|
| Extract | `ogr2ogr -f GeoJSON` for _each_ _table_, with `-t_srs EPSG:4326`, `-nlt CONVERT_TO_LINEAR`, `-dim XY` and `-resolveDomains`, which _writes_ a _coded_ _value_'s _description_ (c6) | The _table_'s _rows_, or the _reason_ GDAL _failed_ |
| Load | `ogr2ogr -f PGDump` on the _cleaned_ _rows_, then the _tables_ _joined_ into the _next_ _numbered_ _script_ | The _script_'s _text_ |
| Report | _Writes_ the _log_ _beside_ the _scripts_, in `db/seed-logs/` | The _log_ _file_, on every _run_ (c4) |

`Program.cs` _runs_ the _stages_ in _order_ and _ends_ with an exit _code_: 0 when it _wrote_ the _script_, 1 when a _table_ _stopped_ or GDAL _failed_, 2 when the _command_ or the _pipeline_ _file_ is _wrong_. Extract and _Load_ _use_ the _machine_'s _own_ GDAL, or GDAL's Docker _image_ when it has _none_, as _today_.

## The files

| File | Holds | About |
|---|---|---|
| `Pipes/Handler.cs` | The _generic_ kernel: the _two_ delegates, `>>`, `Run`, `When`, `All` | 30 _lines_ |
| `Model.cs` | `Row`, `Verdict`, `Event`, `Stop`, `Run` | 40 _lines_ |
| `Steps.cs` | `Filter`, `EachRow`, `StopWhen` | 50 _lines_ |
| `Filters/*.cs` | The _eight_ filters, _one_ _file_ _each_ | 250 _lines_ |
| `Plan.cs` | The _pipeline_ _file_'s _format_, the _kinds_, and `Read` | 150 _lines_ |
| `Gdal.cs` | Extract and _Load_ | 100 _lines_ |
| `Report.cs` | The _log_'s _text_ | 50 _lines_ |
| `Program.cs` | The _stages_ in _order_, and the exit _codes_ | 80 _lines_ |
| **All** | | **about 800 lines** |

## The tests

1. **The kernel.** `>>` _runs_ _steps_ in _order_; `When` skips a _step_ when its _test_ _fails_; a _step_ that does not _call_ `next` _ends_ the _chain_.
2. **Each filter alone.** _Rows_ in, verdicts out: what _each_ _keeps_, _fixes_ and _fails_ (US-36, _test_ 1).
3. **The stop rules.** A _table_ that _drops_ 2 of 10 _rows_ _stops_ _under_ the _default_ 10 _per_ _cent_, and _passes_ when its _own_ _rule_ _allows_ 25 (_test_ 2).
4. **The plan.** A _file_ with _three_ _faults_ _names_ all _three_, and _runs_ _nothing_.
5. **End to end.** GDAL _builds_ a ***geodatabase*** with _one_ _fault_ of _each_ _kind_; the _run_ _keeps_ 3 of 7 cafés, _swaps_ _one_ _back_, _repairs_ a _crossing_ park, and logs _each_ _row_; a _run_ _set_ to _stop_ _writes_ no _script_ and _still_ _writes_ its _log_ (_test_ 3).

The _first_ _four_ _need_ no GDAL and _run_ in _about_ a _second_.

## Compared with today's pipeline

_Today_'s _pipeline_ _already_ _meets_ US-36, so this _design_ is a _choice_, not a _fix_. _Four_ _things_ _differ_:

| | Today | This design |
|---|---|---|
| A step | `Filter`: _rows_ in, an outcome out | `Handler<Run>`: _wraps_ the _rest_ of the _chain_ |
| The stop rules | _Checked_ by the _cleaning_ _code_ _after_ the _chain_ | The _last_ _step_ of the _chain_ |
| A filter for some shapes only | _Each_ filter _checks_ the _shape_ _itself_ | `When` _decides_, _outside_ the filter |
| Code around later steps | Not possible | A _step_ can _run_ _code_ _after_ `next`, such as _timing_ _each_ filter |

The _handler_ _shape_ _adds_ _one_ _idea_, a _step_ that _receives_ the _rest_ of the _chain_, and in _return_ _makes_ _stops_, conditions and wrapping the _same_ _kind_ of _thing_. If the _pipeline_ will _grow_, with more filters, _timing_ or _branches_ by _table_, this _design_ repays the rebuild. If it will _stay_ at _eight_ filters, _today_'s _code_ _serves_ as _well_.

The kernel and the _steps_ _above_ are _built_ and _tested_ in a sketch: a _chain_ of `tidyText`, `requireFields` and `StopWhen` _keeps_ 9 of 10 _rows_ _under_ the _default_ _rule_, _stops_ at _row_ 2 when `requireFields` is _set_ to _stop_, and _stops_ at 10 _per_ _cent_ when the _rule_ _allows_ _only_ 5.
