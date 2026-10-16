# ADR 0004: Write the code in a functional style, with Result

| | |
|---|---|
| Status | _Accepted_; _amended_ 16 _October_ 2026: Tessera _keeps_ a `Result` of its _own_ (ADR 0010) |
| Date | 13 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | Every _line_ of the _back_ _end_, in C# 14 on .NET 10, in `nathanwh71/Tessera`; the _map_ and the _scripts_ _follow_ the _same_ _style_ in TypeScript 6 |
| Related | ADR 0001, _rule_ 6; ADR 0002, _rules_ 1 and 2 |

## Decision

Tessera's _code_ is _written_ in a ***functional style***: immutable _values_, ***pure functions*** and _referential_ transparency. An _expected_ _failure_ is a `Result<T, E>`, from `src/Tessera/Common/Result.cs`, and _never_ an exception. A _value_ that may be _missing_ is a ***nullable type***, `T?`, which the _compiler_ _checks_.

## Why

- **Code the maintainer reads at a glance.** The _maintainer_ _comes_ from F#, and will _read_ and _change_ this _code_ for _years_. Immutable _values_ and `Result` ***pipelines*** _read_ the _way_ an F# _programmer_ _thinks_.
- **A small type of our own.** Tessera _used_ CSharpFunctionalExtensions until 16 _October_ 2026, and _called_ _nine_ of its _members_: `Success`, `Failure`, `IsSuccess`, `IsFailure`, `Value`, `Error`, `Map`, `Match` and `GetValueOrDefault`. _About_ sixty _lines_ in `Common/Result.cs` _give_ the _same_ _nine_, with _tests_, and a _reader_ _finds_ the _whole_ _type_ in _one_ _file_. The ETL _compiles_ the _same_ _file_, so the repository has _one_ `Result`.
- **The compiler watches for missing values.** With `Nullable` on and every _warning_ an _error_, the _compiler_ _makes_ _each_ _caller_ _decide_ what _happens_ when a `T?` is _missing_.
- **Tests without a database.** A _pure_ _function_ _reads_ _only_ its ***parameters***, so FsCheck and xUnit _test_ it with no _database_, no _network_ and no _clock_.

## The three habits

**Immutability.** Every _value_ is a _positional_ `record` or a `readonly record struct`. Every _collection_ is an `ImmutableArray` or an `ImmutableDictionary`. A `with` _expression_ _makes_ a _changed_ _copy_; no _field_ _changes_ _after_ _construction_.

**Pure functions.** _Decisions_ _live_ in `static` _functions_ that _take_ _values_ and _return_ _values_: _quoting_ a _name_, _building_ the ***tile query***'s _text_, _checking_ a ***tile***'s _address_. An _expected_ _failure_ _returns_ as a `Result`; no _function_ _throws_ for _one_.

**Referential transparency.** A _function_ _reads_ _only_ its _parameters_. The _clock_, the _environment_, the _settings_ and the _database_ _reach_ it as _values_ _passed_ in, _never_ _through_ `DateTime.Now`, a `static` _field_ or a _hidden_ _service_. The _same_ _inputs_ _give_ the _same_ _output_.

## Early returns where they read better

A _chain_ of `Bind` _steps_ _suits_ _work_ that _passes_ _one_ _value_ along. A _route_ that _refuses_ for _several_ _reasons_ _reads_ _better_ _top_ to _bottom_, _one_ `if` and _one_ `return` for _each_ _refusal_. The _tile_ _route_ in `src/Tessera/Tiles/Tiles.cs` does this:

```csharp
if (catalogue() is not { } layers)
    return Problems.Unavailable("The list of layers is not ready yet.");
if (layers.Find(layer) is not { } found)
    return Problems.NotFound("No published layer has that name.");
if (TileAddress.Of(z, x, y, settings.MinZoom, settings.MaxZoom) is not { } tile)
    return Problems.NotFound("No tile has those coordinates.");
```

The _database_'s _answer_ then _arrives_ as a `Result`, and `Match` _turns_ _each_ _case_ into an _answer_.

## Types that say what a value is

F# _makes_ _illegal_ _states_ unrepresentable with its _types_, and Tessera _follows_ it where the _gain_ is _real_.

| Tool | Where Tessera uses it | What the compiler then refuses |
|---|---|---|
| A record in place of a long list of arguments | `TileAddress(Z, X, Y)` _names_ the _three_ _numbers_ of a _tile_ | _Arguments_ _swapped_ by _mistake_, which _compile_ _because_ they _share_ a _type_ |
| A branded type in TypeScript | The _map_'s `Level` and `MapZoom`: a ***web-map level*** and MapLibre's _zoom_ _differ_ by _one_ | _One_ _passed_ where the _other_ _belongs_, as F#'s _units_ of _measure_ would _refuse_ |

_Wrap_ _only_ a _value_ that _travels_ _between_ _functions_ and could be mistaken for another; wrapping every _number_ _adds_ noise and _catches_ _nothing_.

## The map and the scripts

The _map_, in TypeScript 6, _keeps_ a `Result` _type_ of its _own_, in `web/src/shared/result.ts`, whose _functions_ _take_ the _result_ _last_, as F#'s do: `map`, `flatMap`, `fold` and `defaultWith`. `parseJson` _turns_ `JSON.parse`'s _throw_ into a `Result` _once_, so no _other_ _code_ _needs_ a `try`.

JavaScript's pipe operator is a _proposal_ at _stage_ 2 of 4, and TypeScript _cannot_ _read_ it, so _two_ _functions_ in `web/src/shared/pipe.ts` _stand_ in for F#'s operators: `pipe` for `|>`, and `flow` for `>>`. _Three_ _rules_ _keep_ a _chain_ readable: _steps_ _run_ in the _order_ _written_, _each_ _joined_ _function_ has a _name_, and a direct _call_ _serves_ where it _reads_ _better_, which for `pipe` _means_ a _chain_ of _fewer_ than _three_ _steps_.

## Options considered

| Option | Why not |
|---|---|
| CSharpFunctionalExtensions | _Used_ until 16 _October_ 2026; Tessera _called_ _nine_ of its _many_ _members_, and a _package_ to track _cost_ more than sixty _lines_ of its _own_ |
| An `Option<T>` of Tessera's own | The _compiler_'s _nullable_ _checks_ _already_ _give_ it |
| LanguageExt | A _large_ _library_ with a _style_ of its _own_, for the _two_ or _three_ _functions_ Tessera _needs_ |
| Exceptions for expected failures | A _caller_ _cannot_ _see_ from a _function_'s _type_ that it may _fail_ |

## Consequences

- _Absence_ has _one_ _form_, `T?`, and the _compiler_ _checks_ it; it _lacks_ `Map` and `Bind`, so a _chain_ that may _find_ _nothing_ _uses_ an _early_ _return_ _instead_.
- C# 14 has no _closed_ _unions_, so `Match` _takes_ the _place_ of F#'s _exhaustive_ `match`.
- _Each_ wrapper _type_ _costs_ a _little_ _code_ at the _edge_, where a _plain_ _number_ _arrives_ and is wrapped _once_.

## When to revisit

- C# _settles_ _closed_ _unions_; a _refusal_ or a _page_ _state_ may then _become_ a _union_, and the _compiler_ can _check_ every `switch`.
- JavaScript's pipe operator _reaches_ _stage_ 3. Then _weigh_ `x |> f(%)` _against_ `pipe`. The operator _follows_ the Hack _style_, with `%` for the _value_, not F#'s, so `pipe` may _still_ _read_ closer to F#.
- A _second_ _maintainer_ _joins_ who _reads_ _idiomatic_ C# _better_ than F#-style C#.
