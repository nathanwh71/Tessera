# ADR 0004: Write the code in a functional style, with Option and Result

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The maintainer |
| Covers | Every ***line*** of the back ***end***, in C# 14 on .NET 10, in `nathanwh71/Tessera`; the ***map*** and the ***scripts*** ***follow*** the same ***style*** in TypeScript 6 |
| Related | ADR 0001, ***rule*** 5; ADR 0002, ***rules*** 1 and 2 |
| Matches | ADR 0004 of `nathanwh71/JTessera`, the Java ***build's*** ***record*** of the ***same*** ***style***, which ***takes*** these ***types*** from Vavr where this ***build*** ***writes*** its ***own*** |
| Carried out | The ***three*** ***habits***, `Option` and `Result`, ***since*** the ***first*** ***build***. The ***types*** that ***say*** what a ***value*** is, and the ***map's*** `pipe` and `flow`: ***decided*** on 13 ***October*** 2026, and not ***yet*** in this ***build's*** ***code*** |
| Replaces | The ***commit*** `ecb21fb` of 12 ***October*** 2026, which ***dropped*** `Option<T>` for ***nullable*** ***types***; it ***stays*** in the ***archived*** ***phase***-0 repository |
| Amended | On 14 ***October*** 2026, in the ***change*** that ***halved*** the ***code***: `Result<T, E>` ***now*** ***comes*** from CSharpFunctionalExtensions, and a ***value*** that may be ***missing*** is a ***nullable*** ***type***; ***see*** ***below*** |

## Decision

Tessera's ***code*** is ***written*** in a ***functional*** ***style***: immutable ***values***, ***pure*** ***functions*** and ***referential*** transparency. ***Absence*** is an `Option<T>` and ***refusal*** a `Result<T, E>`, ***two*** ***small*** ***types*** of Tessera's own, each with `Map`, `Bind`, `Match`, `Select` and `SelectMany`, so the ***code*** ***reads*** ***like*** F#. ***Nullable*** ***types*** ***appear*** only at the ***edge*** of a module, where a ***library*** ***hands*** them in.


## Amended on 14 October 2026

The ***maintainer*** ***asked*** to ***halve*** the ***code*** with ***libraries***, and ***chose*** CSharpFunctionalExtensions for `Result`: its ***latest*** ***release*** ***came*** in March 2026, and it has 38.9 million ***downloads***. ***Three*** ***things*** ***changed***.

- **Refusal is the library's `Result<T, E>`.** It ***keeps*** `Map`, `Bind`, `MapError`, `Match` and `Ensure`, and `from … in … select` ***still*** ***chains*** ***steps***. `Combine` ***joins*** a ***list*** of ***results*** and ***keeps*** the ***first*** ***refusal***.
- **Absence is a nullable type, `T?`.** C# ***checks*** every `T?` with `Nullable` on and every ***warning*** an ***error***, so the ***compiler*** ***still*** ***makes*** ***each*** ***caller*** ***decide*** what ***happens*** when a ***value*** is ***missing***. `Option` and `ToOption` have ***gone***, and so has the ***rule*** that ***kept*** `T?` at a ***module***'s ***edge***.
- **Early returns where they read better.** The ***seed***'s ***checks*** and the ***tile*** ***route*** ***now*** ***read*** ***top*** to ***bottom***, ***one*** `if` and ***one*** `return` for ***each*** ***refusal***.

The ***habits*** ***stand***: immutable ***records*** and ***collections***, ***pure*** ***functions*** for every ***decision***, and an ***expected*** ***failure*** ***returned*** as a `Result`, ***never*** thrown. The ***closed*** ***record*** ***sets***, `Page` and `ProblemStatus`, were ***never*** ***built***, and the ***architecture*** ***test*** that ***guarded*** `Match` ***went*** with the merge of the ***projects*** (ADR 0001).

## Why

- **Code the maintainer reads at a glance.** The maintainer ***comes*** from F#, and will ***read*** and ***change*** this ***code*** for ***years***. `Option` and `Result` ***pipelines*** ***read*** the ***way*** an F# ***programmer*** ***thinks***.
- **One way to chain steps.** `Option` and `Result` ***share*** ***one*** ***set*** of ***functions***, so a ***chain*** of ***steps*** that may each ***find*** ***nothing*** or ***refuse*** ***reads*** the same.
- **Tests without a database.** A ***pure*** ***function*** ***reads*** only its parameters, so FsCheck and xUnit ***test*** it with no database, no ***network*** and no ***clock***.

## The three habits

**Immutability.** Every ***value*** is a ***positional*** `record` or a `readonly record struct`. Every ***collection*** is an `ImmutableArray` or an `ImmutableDictionary`. A `with` ***expression*** ***makes*** a ***changed*** ***copy***; no ***field*** ***changes*** after ***construction***.

**Pure functions.** ***Decisions*** ***live*** in `static` ***functions*** that ***take*** ***values*** and ***return*** ***values***: ***checking*** a GeoJSON ***feature***, ***quoting*** a ***name***, ***building*** the tile query's ***text***. An ***expected*** ***failure*** ***returns*** as a `Result`; no ***function*** ***throws*** for ***one***.

**Referential transparency.** A ***function*** ***reads*** only its parameters. The ***clock***, the ***environment***, the ***settings*** and the database ***reach*** it as ***values*** ***passed*** in, ***never*** through `DateTime.Now`, a `static` ***field*** or a ***hidden*** ***service***. The same ***inputs*** ***give*** the same ***output***.

## Option and Result

| Function | Option<T> | Result<T, E> |
|---|---|---|
| `Map` | ***Changes*** the ***value*** if ***present*** | ***Changes*** the ***value*** if ***accepted*** |
| `Bind` | ***Chains*** a ***step*** that may ***find*** ***nothing*** | ***Chains*** a ***step*** that may ***refuse*** |
| `Match` | ***Handles*** both ***cases*** and ***returns*** ***one*** ***value*** | ***Handles*** both ***cases*** and ***returns*** ***one*** ***value*** |
| `Select`, `SelectMany` | ***Let*** `from … in … select` ***chain*** ***steps***, ***like*** an F# ***computation*** ***expression*** | The same |

The tile ***route*** in `src/Tessera.Tiles/Tiles.cs` ***chains*** ***two*** ***steps*** this ***way***:

```csharp
var request =
    from layer in catalogue.Find(layerId).OrRefuse(Problems.NotFound($"No layer is named '{layerId}'."))
    from tile in TileAddress.Of(z, x, y).MapError(Problems.BadRequest)
    select TileQuery.For(layer, tile, settings);
```

## Types that say what a value is

F# ***makes*** ***illegal*** ***states*** unrepresentable with its ***types***, and Tessera ***follows*** it in both ***builds***. ***Three*** ***tools*** ***serve***, ***each*** with a ***place*** in this ***build's*** ***code***:

| Tool | Where this ***build*** ***uses*** it | What the compiler then ***refuses*** |
|---|---|---|
| A closed set in place of a flag: an `enum`, or an abstract record whose only cases are its own sealed nested records, as `Result` is built | `MapSettings(bool Preview)` ***becomes*** a ***page*** that is `Shown`, `Off` or `Missing`; `Problems.Answer(int status, …)` ***takes*** a `ProblemStatus` of the ***seven*** ***codes*** Tessera ***answers*** with | A ***page*** both off and ***shown***; a ***status*** Tessera ***never*** ***answers*** with |
| A record in place of a long list of arguments | Any ***call*** that ***takes*** ***several*** ***values*** of ***one*** ***type***, which a ***reader*** would ***tell*** ***apart*** by their ***place*** ***alone***; the Java ***build's*** `PoolSettings` ***names*** the ***five*** ***numbers*** of a ***pool*** and what it may do | ***Arguments*** swapped by ***mistake***, which compile ***because*** they ***share*** a ***type*** |
| A single-case type: a `readonly record struct` round one value | `Zoom`, for `TileAddress.Z` and the ***settings***' ***range*** of ***levels*** | A ***column*** or a ***row*** ***passed*** where a ***level*** ***belongs*** |

C# 14 has no ***closed*** ***unions***, so a ***closed*** ***record*** ***set*** ***gains*** a `Match` ***method***, as `Result` has, and ***code*** ***takes*** it ***apart*** ***through*** `Match`: the compiler then ***requires*** a ***function*** for every ***case***. An ArchUnitNET ***test***, `Only_match_takes_option_result_and_page_apart`, ***fails*** the ***build*** if any ***code*** ***outside*** a ***type*** ***names*** ***one*** of its ***cases***, as a `switch` or an `is` would (***added*** 14 ***October*** 2026). Wrap ***only*** a ***value*** that ***travels*** ***between*** ***functions*** and could be mistaken for another; wrapping every ***number*** ***adds*** noise and ***catches*** ***nothing***.

## The map and the scripts

The ***map*** is the ***same*** in both ***builds***, in TypeScript 6. It ***keeps*** a `Result` ***type*** of its ***own***, in `web/src/shared/result.ts`, whose ***functions*** ***take*** the ***result*** ***last***, as F#'s do: `map`, `flatMap`, `fold` and `defaultWith`. `parseJson` ***turns*** `JSON.parse`'s ***throw*** into a `Result` ***once***, so no other ***code*** ***needs*** a `try`.

JavaScript's pipe operator is a ***proposal*** at ***stage*** 2 of 4, and TypeScript ***cannot*** ***read*** it, so ***two*** ***functions*** in `web/src/shared/pipe.ts` ***stand*** in for F#'s operators: `pipe` for `|>`, and `flow` for `>>`. ***Three*** ***rules*** ***keep*** a ***chain*** readable: ***steps*** ***run*** in the ***order*** ***written***, ***each*** ***joined*** ***function*** has a ***name***, and a direct ***call*** ***serves*** where it ***reads*** ***better***, which for `pipe` ***means*** a ***chain*** of ***fewer*** than ***three*** ***steps***. A web-map ***level*** and MapLibre's zoom differ by ***one***, so ***each*** has a ***branded*** ***type***, `Level` and `MapZoom`, and the compiler ***refuses*** ***one*** where the other ***belongs***, as F#'s ***units*** of ***measure*** would.

## The rule at the edge

Dapper, System.Text.Json and ASP.NET Core ***speak*** ***nullable*** ***types***. They ***stay*** in the ***thin*** ***layer*** at each module's ***edge***, which ***turns*** `T?` into `Option<T>` with `ToOption()` the ***moment*** a ***value*** ***arrives***.

1. **No `T?` crosses a module's public face.** The ***pure*** ***core*** ***sees*** `Option` ***alone***.
2. **The compiler keeps watching the edge.** `Nullable` ***stays*** on, with every ***warning*** as an ***error***, so the ***edge*** ***handles*** every `null`.
3. **Review guards rule 1.** No compiler ***check*** ***covers*** it; a ***reviewer*** ***reads*** each module's ***public*** ***types***.

## Options considered

| Option | Why not |
|---|---|
| ***Nullable*** ***types*** ***alone***, with `Result` | ***Built*** in and ***checked***, but ***absence*** ***loses*** `Map` and `Bind`, so ***chains*** ***read*** ***less*** ***like*** F#. The ***commit*** of 12 ***October*** ***chose*** this; this ***record*** ***reverses*** it |
| A ***library***, such as LanguageExt or CSharpFunctionalExtensions | A ***large*** ***dependency*** for ***two*** ***small*** ***types*** the maintainer can ***write*** and ***test*** in an ***afternoon*** |

## Consequences

- C# 14 has no ***closed*** ***unions***, so `Match` ***takes*** the ***place*** of F#'s ***exhaustive*** `match`; the compiler ***cannot*** ***warn*** of a ***missing*** ***case*** in a `switch` on these ***types***. The ***architecture*** ***test*** ***closes*** that ***gap***: a `switch` on a ***case*** ***outside*** its ***type*** ***fails*** the ***build***.
- ***Two*** ***ways*** to ***say*** '***missing***' ***exist***, ***kept*** ***apart*** by a ***rule*** that ***review*** ***guards***.
- `Option` and `Result` ***need*** ***tests*** of their own, ***including*** FsCheck ***property*** ***tests*** of the ***laws*** `Map` and `Bind` must ***keep***.
- ***Each*** ***closed*** ***record*** ***set*** ***needs*** its ***own*** `Match`, a ***few*** ***lines*** the compiler ***cannot*** ***write*** for it.
- ***Small*** wrapper ***types***, such as `Zoom`, ***cost*** a ***little*** ***code*** at ***each*** ***edge***, where a ***plain*** ***number*** ***arrives*** and is wrapped ***once***.

## When to revisit

- C# ***settles*** ***closed*** ***unions***; `Option`, `Result` and the ***closed*** ***record*** ***sets*** may then ***become*** ***unions***, and the compiler can ***check*** every `switch` ***without*** `Match`.
- JavaScript's pipe operator ***reaches*** ***stage*** 3. Then weigh `x |> f(%)` ***against*** `pipe`. The operator ***follows*** the Hack ***style***, with `%` for the ***value***, not F#'s, so `pipe` may ***still*** ***read*** closer to F#.
- A ***second*** maintainer ***joins*** who ***reads*** ***idiomatic*** C# ***better*** than F#-***style*** C#.
