# ADR 0004: Write the code in a functional style, with Result

| | |
|---|---|
| Status | <span style="color:grey">Accepted</span> |
| Date | 13 <span style="color:grey">October</span> 2026 |
| Decider | The <span style="color:grey">maintainer</span> |
| Covers | Every <span style="color:grey">line</span> of the <span style="color:grey">back</span> <span style="color:grey">end</span>, in C# 14 on .NET 10, in `nathanwh71/Tessera`; the <span style="color:grey">map</span> and the <span style="color:grey">scripts</span> <span style="color:grey">follow</span> the <span style="color:grey">same</span> <span style="color:grey">style</span> in TypeScript 6 |
| Related | ADR 0001, <span style="color:grey">rule</span> 6; ADR 0002, <span style="color:grey">rules</span> 1 and 2 |

## Decision

Tessera's <span style="color:grey">code</span> is <span style="color:grey">written</span> in a ***functional style***: immutable <span style="color:grey">values</span>, ***pure functions*** and <span style="color:grey">referential</span> transparency. An <span style="color:grey">expected</span> <span style="color:grey">failure</span> is a `Result<T, E>`, from the CSharpFunctionalExtensions <span style="color:grey">library</span>, and <span style="color:grey">never</span> an exception. A <span style="color:grey">value</span> that may be <span style="color:grey">missing</span> is a ***nullable type***, `T?`, which the <span style="color:grey">compiler</span> <span style="color:grey">checks</span>.

## Why

- **Code the maintainer reads at a glance.** The <span style="color:grey">maintainer</span> <span style="color:grey">comes</span> from F#, and will <span style="color:grey">read</span> and <span style="color:grey">change</span> this <span style="color:grey">code</span> for <span style="color:grey">years</span>. Immutable <span style="color:grey">values</span> and `Result` ***pipelines*** <span style="color:grey">read</span> the <span style="color:grey">way</span> an F# <span style="color:grey">programmer</span> <span style="color:grey">thinks</span>.
- **A library, not code of our own.** CSharpFunctionalExtensions <span style="color:grey">gives</span> `Result` with `Map`, `Bind`, `MapError`, `Match`, `Ensure` and `Combine`, and `from … in … select` <span style="color:grey">chains</span> its <span style="color:grey">steps</span>. It is widely <span style="color:grey">used</span>, with 38.9 million <span style="color:grey">downloads</span>, so Tessera <span style="color:grey">need</span> not <span style="color:grey">write</span> or <span style="color:grey">test</span> a `Result` of its <span style="color:grey">own</span>.
- **The compiler watches for missing values.** With `Nullable` on and every <span style="color:grey">warning</span> an <span style="color:grey">error</span>, the <span style="color:grey">compiler</span> <span style="color:grey">makes</span> <span style="color:grey">each</span> <span style="color:grey">caller</span> <span style="color:grey">decide</span> what <span style="color:grey">happens</span> when a `T?` is <span style="color:grey">missing</span>.
- **Tests without a database.** A <span style="color:grey">pure</span> <span style="color:grey">function</span> <span style="color:grey">reads</span> <span style="color:grey">only</span> its ***parameters***, so FsCheck and xUnit <span style="color:grey">test</span> it with no <span style="color:grey">database</span>, no <span style="color:grey">network</span> and no <span style="color:grey">clock</span>.

## The three habits

**Immutability.** Every <span style="color:grey">value</span> is a <span style="color:grey">positional</span> `record` or a `readonly record struct`. Every <span style="color:grey">collection</span> is an `ImmutableArray` or an `ImmutableDictionary`. A `with` <span style="color:grey">expression</span> <span style="color:grey">makes</span> a <span style="color:grey">changed</span> <span style="color:grey">copy</span>; no <span style="color:grey">field</span> <span style="color:grey">changes</span> <span style="color:grey">after</span> <span style="color:grey">construction</span>.

**Pure functions.** <span style="color:grey">Decisions</span> <span style="color:grey">live</span> in `static` <span style="color:grey">functions</span> that <span style="color:grey">take</span> <span style="color:grey">values</span> and <span style="color:grey">return</span> <span style="color:grey">values</span>: <span style="color:grey">quoting</span> a <span style="color:grey">name</span>, <span style="color:grey">building</span> the ***tile query***'s <span style="color:grey">text</span>, <span style="color:grey">checking</span> a ***tile***'s <span style="color:grey">address</span>. An <span style="color:grey">expected</span> <span style="color:grey">failure</span> <span style="color:grey">returns</span> as a `Result`; no <span style="color:grey">function</span> <span style="color:grey">throws</span> for <span style="color:grey">one</span>.

**Referential transparency.** A <span style="color:grey">function</span> <span style="color:grey">reads</span> <span style="color:grey">only</span> its <span style="color:grey">parameters</span>. The <span style="color:grey">clock</span>, the <span style="color:grey">environment</span>, the <span style="color:grey">settings</span> and the <span style="color:grey">database</span> <span style="color:grey">reach</span> it as <span style="color:grey">values</span> <span style="color:grey">passed</span> in, <span style="color:grey">never</span> <span style="color:grey">through</span> `DateTime.Now`, a `static` <span style="color:grey">field</span> or a <span style="color:grey">hidden</span> <span style="color:grey">service</span>. The <span style="color:grey">same</span> <span style="color:grey">inputs</span> <span style="color:grey">give</span> the <span style="color:grey">same</span> <span style="color:grey">output</span>.

## Early returns where they read better

A <span style="color:grey">chain</span> of `Bind` <span style="color:grey">steps</span> <span style="color:grey">suits</span> <span style="color:grey">work</span> that <span style="color:grey">passes</span> <span style="color:grey">one</span> <span style="color:grey">value</span> along. A <span style="color:grey">route</span> that <span style="color:grey">refuses</span> for <span style="color:grey">several</span> <span style="color:grey">reasons</span> <span style="color:grey">reads</span> <span style="color:grey">better</span> <span style="color:grey">top</span> to <span style="color:grey">bottom</span>, <span style="color:grey">one</span> `if` and <span style="color:grey">one</span> `return` for <span style="color:grey">each</span> <span style="color:grey">refusal</span>. The <span style="color:grey">tile</span> <span style="color:grey">route</span> in `src/Tessera/Tiles/Tiles.cs` does this:

```csharp
if (catalogue() is not { } layers)
    return Problems.Unavailable("The list of layers is not ready yet.");
if (layers.Find(layer) is not { } found)
    return Problems.NotFound("No published layer has that name.");
if (TileAddress.Of(z, x, y, settings.MinZoom, settings.MaxZoom) is not { } tile)
    return Problems.NotFound("No tile has those coordinates.");
```

The <span style="color:grey">database</span>'s <span style="color:grey">answer</span> then <span style="color:grey">arrives</span> as a `Result`, and `Match` <span style="color:grey">turns</span> <span style="color:grey">each</span> <span style="color:grey">case</span> into an <span style="color:grey">answer</span>.

## Types that say what a value is

F# <span style="color:grey">makes</span> <span style="color:grey">illegal</span> <span style="color:grey">states</span> unrepresentable with its <span style="color:grey">types</span>, and Tessera <span style="color:grey">follows</span> it where the <span style="color:grey">gain</span> is <span style="color:grey">real</span>.

| Tool | Where Tessera uses it | What the compiler then refuses |
|---|---|---|
| A record in place of a long list of arguments | `TileAddress(Z, X, Y)` <span style="color:grey">names</span> the <span style="color:grey">three</span> <span style="color:grey">numbers</span> of a <span style="color:grey">tile</span> | <span style="color:grey">Arguments</span> <span style="color:grey">swapped</span> by <span style="color:grey">mistake</span>, which <span style="color:grey">compile</span> <span style="color:grey">because</span> they <span style="color:grey">share</span> a <span style="color:grey">type</span> |
| A branded type in TypeScript | The <span style="color:grey">map</span>'s `Level` and `MapZoom`: a ***web-map level*** and MapLibre's <span style="color:grey">zoom</span> <span style="color:grey">differ</span> by <span style="color:grey">one</span> | <span style="color:grey">One</span> <span style="color:grey">passed</span> where the <span style="color:grey">other</span> <span style="color:grey">belongs</span>, as F#'s <span style="color:grey">units</span> of <span style="color:grey">measure</span> would <span style="color:grey">refuse</span> |

<span style="color:grey">Wrap</span> <span style="color:grey">only</span> a <span style="color:grey">value</span> that <span style="color:grey">travels</span> <span style="color:grey">between</span> <span style="color:grey">functions</span> and could be mistaken for another; wrapping every <span style="color:grey">number</span> <span style="color:grey">adds</span> noise and <span style="color:grey">catches</span> <span style="color:grey">nothing</span>.

## The map and the scripts

The <span style="color:grey">map</span>, in TypeScript 6, <span style="color:grey">keeps</span> a `Result` <span style="color:grey">type</span> of its <span style="color:grey">own</span>, in `web/src/shared/result.ts`, whose <span style="color:grey">functions</span> <span style="color:grey">take</span> the <span style="color:grey">result</span> <span style="color:grey">last</span>, as F#'s do: `map`, `flatMap`, `fold` and `defaultWith`. `parseJson` <span style="color:grey">turns</span> `JSON.parse`'s <span style="color:grey">throw</span> into a `Result` <span style="color:grey">once</span>, so no <span style="color:grey">other</span> <span style="color:grey">code</span> <span style="color:grey">needs</span> a `try`.

JavaScript's pipe operator is a <span style="color:grey">proposal</span> at <span style="color:grey">stage</span> 2 of 4, and TypeScript <span style="color:grey">cannot</span> <span style="color:grey">read</span> it, so <span style="color:grey">two</span> <span style="color:grey">functions</span> in `web/src/shared/pipe.ts` <span style="color:grey">stand</span> in for F#'s operators: `pipe` for `|>`, and `flow` for `>>`. <span style="color:grey">Three</span> <span style="color:grey">rules</span> <span style="color:grey">keep</span> a <span style="color:grey">chain</span> readable: <span style="color:grey">steps</span> <span style="color:grey">run</span> in the <span style="color:grey">order</span> <span style="color:grey">written</span>, <span style="color:grey">each</span> <span style="color:grey">joined</span> <span style="color:grey">function</span> has a <span style="color:grey">name</span>, and a direct <span style="color:grey">call</span> <span style="color:grey">serves</span> where it <span style="color:grey">reads</span> <span style="color:grey">better</span>, which for `pipe` <span style="color:grey">means</span> a <span style="color:grey">chain</span> of <span style="color:grey">fewer</span> than <span style="color:grey">three</span> <span style="color:grey">steps</span>.

## Options considered

| Option | Why not |
|---|---|
| An `Option<T>` and a `Result<T, E>` of Tessera's own | <span style="color:grey">Two</span> <span style="color:grey">types</span> to <span style="color:grey">write</span> and <span style="color:grey">test</span>, for what a <span style="color:grey">library</span> and the <span style="color:grey">compiler</span>'s <span style="color:grey">nullable</span> <span style="color:grey">checks</span> <span style="color:grey">already</span> <span style="color:grey">give</span> |
| LanguageExt | A <span style="color:grey">large</span> <span style="color:grey">library</span> with a <span style="color:grey">style</span> of its <span style="color:grey">own</span>, for the <span style="color:grey">two</span> or <span style="color:grey">three</span> <span style="color:grey">functions</span> Tessera <span style="color:grey">needs</span> |
| Exceptions for expected failures | A <span style="color:grey">caller</span> <span style="color:grey">cannot</span> <span style="color:grey">see</span> from a <span style="color:grey">function</span>'s <span style="color:grey">type</span> that it may <span style="color:grey">fail</span> |

## Consequences

- <span style="color:grey">Absence</span> has <span style="color:grey">one</span> <span style="color:grey">form</span>, `T?`, and the <span style="color:grey">compiler</span> <span style="color:grey">checks</span> it; it <span style="color:grey">lacks</span> `Map` and `Bind`, so a <span style="color:grey">chain</span> that may <span style="color:grey">find</span> <span style="color:grey">nothing</span> <span style="color:grey">uses</span> an <span style="color:grey">early</span> <span style="color:grey">return</span> <span style="color:grey">instead</span>.
- C# 14 has no <span style="color:grey">closed</span> <span style="color:grey">unions</span>, so `Match` <span style="color:grey">takes</span> the <span style="color:grey">place</span> of F#'s <span style="color:grey">exhaustive</span> `match`.
- <span style="color:grey">Each</span> wrapper <span style="color:grey">type</span> <span style="color:grey">costs</span> a <span style="color:grey">little</span> <span style="color:grey">code</span> at the <span style="color:grey">edge</span>, where a <span style="color:grey">plain</span> <span style="color:grey">number</span> <span style="color:grey">arrives</span> and is wrapped <span style="color:grey">once</span>.

## When to revisit

- C# <span style="color:grey">settles</span> <span style="color:grey">closed</span> <span style="color:grey">unions</span>; a <span style="color:grey">refusal</span> or a <span style="color:grey">page</span> <span style="color:grey">state</span> may then <span style="color:grey">become</span> a <span style="color:grey">union</span>, and the <span style="color:grey">compiler</span> can <span style="color:grey">check</span> every `switch`.
- JavaScript's pipe operator <span style="color:grey">reaches</span> <span style="color:grey">stage</span> 3. Then <span style="color:grey">weigh</span> `x |> f(%)` <span style="color:grey">against</span> `pipe`. The operator <span style="color:grey">follows</span> the Hack <span style="color:grey">style</span>, with `%` for the <span style="color:grey">value</span>, not F#'s, so `pipe` may <span style="color:grey">still</span> <span style="color:grey">read</span> closer to F#.
- A <span style="color:grey">second</span> <span style="color:grey">maintainer</span> <span style="color:grey">joins</span> who <span style="color:grey">reads</span> <span style="color:grey">idiomatic</span> C# <span style="color:grey">better</span> than F#-style C#.
