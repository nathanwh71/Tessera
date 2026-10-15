# ADR 0004: Write the code in a functional style, with Result

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | Every ***line*** of the ***back*** ***end***, in C# 14 on .NET 10, in `nathanwh71/Tessera`; the ***map*** and the ***scripts*** ***follow*** the ***same*** ***style*** in TypeScript 6 |
| Related | ADR 0001, ***rule*** 6; ADR 0002, ***rules*** 1 and 2 |

## Decision

***Tessera***'s ***code*** is ***written*** in a ***functional*** ***style***: immutable ***values***, ***pure*** ***functions*** and ***referential*** transparency. An ***expected*** ***failure*** is a `Result<T, E>`, from the CSharpFunctionalExtensions ***library***, and ***never*** an exception. A ***value*** that may be ***missing*** is a ***nullable*** ***type***, `T?`, which the ***compiler*** ***checks***.

## Why

- **Code the maintainer reads at a glance.** The ***maintainer*** ***comes*** from F#, and will ***read*** and ***change*** this ***code*** for ***years***. Immutable ***values*** and `Result` ***pipelines*** ***read*** the ***way*** an F# ***programmer*** ***thinks***.
- **A library, not code of our own.** CSharpFunctionalExtensions ***gives*** `Result` with `Map`, `Bind`, `MapError`, `Match`, `Ensure` and `Combine`, and `from … in … select` ***chains*** its ***steps***. It is widely ***used***, with 38.9 million ***downloads***, so ***Tessera*** ***need*** not ***write*** or ***test*** a `Result` of its ***own***.
- **The compiler watches for missing values.** With `Nullable` on and every ***warning*** an ***error***, the ***compiler*** ***makes*** ***each*** ***caller*** ***decide*** what ***happens*** when a `T?` is ***missing***.
- **Tests without a database.** A ***pure*** ***function*** ***reads*** ***only*** its ***parameters***, so FsCheck and xUnit ***test*** it with no ***database***, no ***network*** and no ***clock***.

## The three habits

**Immutability.** Every ***value*** is a ***positional*** `record` or a `readonly record struct`. Every ***collection*** is an `ImmutableArray` or an `ImmutableDictionary`. A `with` ***expression*** ***makes*** a ***changed*** ***copy***; no ***field*** ***changes*** ***after*** ***construction***.

**Pure functions.** ***Decisions*** ***live*** in `static` ***functions*** that ***take*** ***values*** and ***return*** ***values***: ***quoting*** a ***name***, ***building*** the ***tile*** ***query***'s ***text***, ***checking*** a ***tile***'s ***address***. An ***expected*** ***failure*** ***returns*** as a `Result`; no ***function*** ***throws*** for ***one***.

**Referential transparency.** A ***function*** ***reads*** ***only*** its ***parameters***. The ***clock***, the ***environment***, the ***settings*** and the ***database*** ***reach*** it as ***values*** ***passed*** in, ***never*** ***through*** `DateTime.Now`, a `static` ***field*** or a ***hidden*** ***service***. The ***same*** ***inputs*** ***give*** the ***same*** ***output***.

## Early returns where they read better

A ***chain*** of `Bind` ***steps*** ***suits*** ***work*** that ***passes*** ***one*** ***value*** along. A ***route*** that ***refuses*** for ***several*** ***reasons*** ***reads*** ***better*** ***top*** to ***bottom***, ***one*** `if` and ***one*** `return` for ***each*** ***refusal***. The ***tile*** ***route*** in `src/Tessera/Tiles/Tiles.cs` does this:

```csharp
if (catalogue() is not { } layers)
    return Problems.Unavailable("The list of layers is not ready yet.");
if (layers.Find(layer) is not { } found)
    return Problems.NotFound("No published layer has that name.");
if (TileAddress.Of(z, x, y, settings.MinZoom, settings.MaxZoom) is not { } tile)
    return Problems.NotFound("No tile has those coordinates.");
```

The ***database***'s ***answer*** then ***arrives*** as a `Result`, and `Match` ***turns*** ***each*** ***case*** into an ***answer***.

## Types that say what a value is

F# ***makes*** ***illegal*** ***states*** unrepresentable with its ***types***, and ***Tessera*** ***follows*** it where the ***gain*** is ***real***.

| Tool | Where Tessera uses it | What the compiler then refuses |
|---|---|---|
| A record in place of a long list of arguments | `TileAddress(Z, X, Y)` ***names*** the ***three*** ***numbers*** of a ***tile*** | ***Arguments*** ***swapped*** by ***mistake***, which ***compile*** ***because*** they ***share*** a ***type*** |
| A branded type in TypeScript | The ***map***'s `Level` and `MapZoom`: a web-map ***level*** and MapLibre's ***zoom*** ***differ*** by ***one*** | ***One*** ***passed*** where the ***other*** ***belongs***, as F#'s ***units*** of ***measure*** would ***refuse*** |

***Wrap*** ***only*** a ***value*** that ***travels*** ***between*** ***functions*** and could be mistaken for another; wrapping every ***number*** ***adds*** noise and ***catches*** ***nothing***.

## The map and the scripts

The ***map***, in TypeScript 6, ***keeps*** a `Result` ***type*** of its ***own***, in `web/src/shared/result.ts`, whose ***functions*** ***take*** the ***result*** ***last***, as F#'s do: `map`, `flatMap`, `fold` and `defaultWith`. `parseJson` ***turns*** `JSON.parse`'s ***throw*** into a `Result` ***once***, so no ***other*** ***code*** ***needs*** a `try`.

JavaScript's pipe operator is a ***proposal*** at ***stage*** 2 of 4, and TypeScript ***cannot*** ***read*** it, so ***two*** ***functions*** in `web/src/shared/pipe.ts` ***stand*** in for F#'s operators: `pipe` for `|>`, and `flow` for `>>`. ***Three*** ***rules*** ***keep*** a ***chain*** readable: ***steps*** ***run*** in the ***order*** ***written***, ***each*** ***joined*** ***function*** has a ***name***, and a direct ***call*** ***serves*** where it ***reads*** ***better***, which for `pipe` ***means*** a ***chain*** of ***fewer*** than ***three*** ***steps***.

## Options considered

| Option | Why not |
|---|---|
| An `Option<T>` and a `Result<T, E>` of Tessera's own | ***Two*** ***types*** to ***write*** and ***test***, for what a ***library*** and the ***compiler***'s ***nullable*** ***checks*** ***already*** ***give*** |
| LanguageExt | A ***large*** ***library*** with a ***style*** of its ***own***, for the ***two*** or ***three*** ***functions*** ***Tessera*** ***needs*** |
| Exceptions for expected failures | A ***caller*** ***cannot*** ***see*** from a ***function***'s ***type*** that it may ***fail*** |

## Consequences

- ***Absence*** has ***one*** ***form***, `T?`, and the ***compiler*** ***checks*** it; it ***lacks*** `Map` and `Bind`, so a ***chain*** that may ***find*** ***nothing*** ***uses*** an ***early*** ***return*** ***instead***.
- C# 14 has no ***closed*** ***unions***, so `Match` ***takes*** the ***place*** of F#'s ***exhaustive*** `match`.
- ***Each*** wrapper ***type*** ***costs*** a ***little*** ***code*** at the ***edge***, where a ***plain*** ***number*** ***arrives*** and is wrapped ***once***.

## When to revisit

- C# ***settles*** ***closed*** ***unions***; a ***refusal*** or a ***page*** ***state*** may then ***become*** a ***union***, and the ***compiler*** can ***check*** every `switch`.
- JavaScript's pipe operator ***reaches*** ***stage*** 3. Then ***weigh*** `x |> f(%)` ***against*** `pipe`. The operator ***follows*** the Hack ***style***, with `%` for the ***value***, not F#'s, so `pipe` may ***still*** ***read*** closer to F#.
- A ***second*** ***maintainer*** ***joins*** who ***reads*** ***idiomatic*** C# ***better*** than F#-style C#.
