# ADR 0004: Write the code in a functional style, with Option and Result

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The maintainer |
| Covers | Every ***line*** of the back ***end***, in C# 14 on .NET 10, in `nathanwh71/Tessera`; the ***map*** and the ***scripts*** ***follow*** the same ***style*** in TypeScript 6 |
| Related | ADR 0001, ***rule*** 5; ADR 0002, ***rules*** 1 and 2 |
| Replaces | The `main` ***commit*** `ecb21fb` of 12 ***October*** 2026, which ***dropped*** `Option<T>` for ***nullable*** ***types***; it ***stays*** on the `archive/phase-0` ***branch*** |

## Decision

Tessera's ***code*** is ***written*** in a ***functional*** ***style***: immutable ***values***, ***pure*** ***functions*** and ***referential*** transparency. ***Absence*** is an `Option<T>` and ***refusal*** a `Result<T, E>`, ***two*** ***small*** ***types*** of Tessera's own, each with `Map`, `Bind`, `Match`, `Select` and `SelectMany`, so the ***code*** ***reads*** ***like*** F#. ***Nullable*** ***types*** ***appear*** only at the ***edge*** of a module, where a ***library*** ***hands*** them in.

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

- C# 14 has no ***closed*** ***unions***, so `Match` ***takes*** the ***place*** of F#'s ***exhaustive*** `match`; the compiler ***cannot*** ***warn*** of a ***missing*** ***case*** in a `switch` on these ***types***.
- ***Two*** ***ways*** to ***say*** '***missing***' ***exist***, ***kept*** ***apart*** by a ***rule*** that ***review*** ***guards***.
- `Option` and `Result` ***need*** ***tests*** of their own, ***including*** FsCheck ***property*** ***tests*** of the ***laws*** `Map` and `Bind` must ***keep***.

## When to revisit

- C# ***settles*** ***closed*** ***unions***; `Option` and `Result` may then ***become*** ***unions***, and the compiler can ***check*** every ***case***.
- A ***second*** maintainer ***joins*** who ***reads*** ***idiomatic*** C# ***better*** than F#-***style*** C#.
