# ADR 0001: Build the back end as a modular monolith

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The maintainer |
| Covers | The Phase Oct back ***end***, in C# 14 on .NET 10, in `nathanwh71/Tessera` |
| Stories | US-00, US-01, US-03, US-24, US-07, S-01, S-06, US-35, US-23 (`docs/user-stories/user-stories.md`) |
| Amended | On 14 ***October*** 2026, in the ***change*** that ***halved*** the ***code***, the ***nine*** ***projects*** merged into ***one***, `src/Tessera`, with a ***folder*** for ***each*** ***module***; ***see*** ***below*** |

## Decision

Tessera's back ***end*** is ***one*** ***service***, ***built*** and ***shipped*** as ***one*** ***unit***, and ***divided*** ***inside*** into modules with ***fixed*** ***borders***. Each module ***owns*** ***one*** ***part*** of the ***work*** the ***stories*** ***describe***, ***shows*** other modules a ***small*** ***public*** ***face***, and ***hides*** the ***rest***. Each module is a ***project*** of its own, so the compiler ***guards*** the ***borders***, and ***architecture*** ***tests*** ***guard*** the ***rules*** it ***cannot*** ***see***; a ***break*** ***fails*** the ***build***.

This ***record*** ***covers*** the back ***end*** ***alone***. The ***map***'s ***front*** ***end***, in TypeScript, ***keeps*** its own ***design***.


## Amended on 14 October 2026

The ***service*** is ***now*** ***one*** ***project***, `src/Tessera`, with a ***folder*** for ***each*** ***module***: ***Settings***, Sql, Catalogue, ***Import***, ***Tiles*** and ***Map***. The ***maintainer*** ***found*** ***nine*** ***projects*** ***too*** ***many*** to ***follow***. Kernel and Http have ***gone***: CSharpFunctionalExtensions ***gives*** `Result` (ADR 0004), and ASP.NET Core ***writes*** every ***problem*** ***document***, ***shaped*** by `Problems.cs`. Nginx, in ***front***, ***now*** does the ***work*** of the ***tile*** ***cache*** and the ***address*** ***screen*** (ADR 0008).

With ***one*** ***project***, the ***compiler*** no ***longer*** ***guards*** the ***borders***, and the ***architecture*** ***tests*** have ***gone***. The ***rules*** ***below*** ***stand*** as a ***guide*** for ***review***: Sql ***alone*** ***talks*** to the ***database***, and every SQL ***text*** ***lives*** in `Queries.cs`; ***modules*** ***pass*** immutable ***values*** and ***report*** a ***refusal*** as a `Result`.

## Context

***Five*** ***facts*** ***shape*** the ***decision***.

- **One maintainer.** ***One*** ***person*** ***builds***, ***runs*** and ***repairs*** Tessera.
- **One machine.** The ***service*** ***runs*** in Ubuntu 24.04 ***inside*** WSL2, on ***one*** Windows 11 ***machine***, and ***answers*** on ***port*** 80 (US-35).
- **One process.** The same ***service*** ***serves*** the tiles (US-07) and the ***map***'s ***files*** (US-23), and ***ships*** them in ***one*** ***release***.
- **One gate to the database.** S-01 ***requires*** that ***one*** query builder ***sends*** every query, and that an ***automated*** ***check*** ***fails*** the ***build*** if any other ***code*** ***sends*** SQL. That ***check*** ***needs*** a ***border*** to ***test***.
- **One place for the design.** The ***stories*** ***leave*** the ***design*** to the ***team*** on ***purpose***; this ***record*** ***holds*** it.

## Options considered

| Option | For | Against |
|---|---|---|
| Modular monolith (***chosen***) | ***One*** ***build***, ***one*** ***release*** and ***one*** ***process*** to ***watch***. Each ***story***'s ***code*** ***lives*** in ***one*** module. S-01's ***check*** ***follows*** from the ***borders***. | The ***borders*** ***need*** ***thought*** and ***tests*** to ***hold***. |
| Layered monolith (***routes***, ***services***, ***data***) | ***Familiar*** and ***quick*** to ***start***. | Each ***story*** ***spreads*** ***across*** every ***layer***. ***Nothing*** ***stops*** a ***route*** from ***calling*** the database, so S-01 ***needs*** a ***rule*** of its own. |
| ***Separate*** ***services*** | The ***network*** ***enforces*** the ***borders***. | ***Several*** ***releases*** on ***one*** ***machine***, and ***network*** ***calls*** ***between*** ***parts***, for ***one*** maintainer who ***gains*** ***nothing*** from them. |

## The modules, first cut

| Module | Owns | Stories |
|---|---|---|
| Settings | ***Reading*** every ***machine***'s ***name***, ***address*** and ***port*** from ***environment*** variables; ***refusing*** to ***start*** when ***one*** is ***missing*** | US-00 |
| Sql | Every query's ***text***, as ***named*** ***functions***; the ***one*** query builder ***behind*** them; the ***one*** ***function*** that ***quotes*** ***names***; and the ***only*** ***use*** of the database ***client***, Npgsql with Dapper | S-01, US-03 (S-02) |
| Catalogue | The ***list*** of ***granted*** ***tables*** and ***views***, ***built*** at ***start***-up | US-03 |
| Import | The `import` ***command***: ***reading*** and ***checking*** the GeoJSON ***seed***, and ***replacing*** the ***rows*** of the ***nine*** ***tables*** in ***one*** transaction | US-24 |
| Tiles | The tile ***route***, the tile query, and the ***kept*** ***copies*** with their ***tags*** | US-07, N-18 to N-20 |
| Http | ***Screening*** ***raw*** ***request*** ***targets***, and ***writing*** every ***problem*** ***document*** | S-06, US-07 |
| Map | ***Serving*** the ***front*** ***end***'s ***built*** ***files*** | US-23 |

***Two*** more ***projects*** ***stand*** ***beside*** the ***seven***, and ***neither*** is a module. `Tessera.Kernel` ***holds*** `Option`, `Result` and the ***few*** ***values*** every module ***shares***; it ***references*** ***nothing***, so every module can ***reference*** it. `Tessera.Service` is the ***program***: it ***puts*** the modules ***together***, and no module ***references*** it.

The ***list*** is a ***first*** ***cut***. A module ***joins*** it when a ***story*** ***needs*** ***one***, and ***leaves*** when ***none*** ***does***.

**The import is a command of the service.** The same ***program*** that ***serves*** tiles ***runs*** the ***import*** when ***called*** with `import` and the ***seed***'s ***path***, then ***stops***. ***Four*** ***reasons*** ***settle*** it, ***against*** a ***script*** that the PostgreSQL/PostGIS ***container*** ***runs*** on its ***first*** ***start***:

- **US-24's checks belong in C#.** ***Refusing*** a ***file*** in another ***system***, a ***position*** ***outside*** the ***box*** or a ***feature*** with the ***wrong*** ***layer***, and ***naming*** the ***feature***, ***read*** ***clearly*** as `Result` ***values***, and ***unit*** ***tests*** ***prove*** them in ***seconds***.
- **Criterion 6 works as written.** The ***command*** ***runs*** ***whenever*** it is ***called***, so a newer ***file*** ***replaces*** the ***rows***; the ***container*** ***runs*** its ***scripts*** only on an ***empty*** ***data*** ***folder***.
- **S-01's gate stays whole.** The ***import***'s SQL ***passes*** ***through*** Sql ***like*** every other query; a ***container*** ***script*** ***sends*** SQL the ***build*** ***cannot*** ***check***.
- **One language, one style.** The ***import*** ***follows*** the same ***rules*** and ***tests*** as the ***rest*** of the back ***end***.

**The import signs in as its own role.** The ***service*** ***signs*** in as `tessera_reader`, which ***reads*** ***granted*** ***tables*** ***alone*** (US-03). The ***import*** ***signs*** in as `tessera_publisher`, the ***data*** publisher's ***role***, which ***owns*** the ***four*** schemas and the ***nine*** ***tables***. It ***creates*** and ***fills*** them, and ***grants*** them only when the publisher ***chooses*** (US-24, ***criterion*** 5). Both ***names*** ***match*** the Java ***build*** in `nathanwh71/JTessera`, whose ***role*** ***script*** ***makes*** both ***roles***.

| Variable | Read by | Holds |
|---|---|---|
| `DATABASE_URL` | The ***service***, at every ***start*** | `postgresql://tessera_reader:<password>@<host>:<port>/<database>` |
| `TESSERA_PUBLISHER_DATABASE_URL` | `import`, only when it ***runs*** | `postgresql://tessera_publisher:<password>@<host>:<port>/<database>` |
| `TESSERA_READER_PASSWORD` | The ***role*** ***script***, on the database's ***first*** ***start*** | `tessera_reader`'s ***password*** |
| `TESSERA_PUBLISHER_PASSWORD` | The ***role*** ***script***, on the database's ***first*** ***start*** | `tessera_publisher`'s ***password*** |

Each URL ***carries*** the ***machine***'s ***name***, ***address*** and ***port***, as US-00 ***requires***; Settings ***turns*** it into a ***connection*** ***string*** for the database ***client***. The ***service*** ***never*** ***reads*** `TESSERA_PUBLISHER_DATABASE_URL`, so it ***never*** ***holds*** the publisher's ***rights*** while it ***serves*** tiles.

**A start-up script runs the import when the tables are empty.** The ***script*** ***starts*** the ***container*** and ***waits*** until the database ***answers***. It then ***runs*** `import` only when the ***nine*** ***tables*** are ***empty***: ***missing***, or ***holding*** no ***rows***. ***Later*** ***starts*** ***leave*** the ***places*** ***alone***; to ***load*** a newer ***seed***, the publisher ***runs*** `import` by ***hand*** (US-24, ***criterion*** 6).

## Rules at the borders

1. **A small public face.** Each module ***shows*** only the ***types*** other modules ***need***; ***everything*** ***else*** is `internal`.
2. **One direction.** Modules ***depend*** on ***one*** another in ***one*** ***direction***, with no ***circle***.
3. **One gate to SQL.** Only Sql ***uses*** the database ***client*** (S-01, ***criterion*** 2).
4. **One shape for errors.** Only Http ***writes*** a ***problem*** ***document***; no ***route*** ***writes*** an ***error*** ***body*** of its own (US-07).
5. **Values, not shared state.** Modules ***pass*** immutable ***values*** to ***one*** another, and ***report*** ***failure*** as a `Result`, ***never*** as a ***shared*** ***object*** or a ***null***. `Result` and `Option` are ***two*** ***small*** ***types*** of Tessera's own, in a ***project*** every module ***references***; no ***library*** ***supplies*** them.

## How the build guards the borders

Each module ***sits*** in a ***project*** of its own, so the compiler ***guards*** the ***borders***. .NET has no ***equal*** of Spring Modulith; the compiler and ***one*** ***test*** ***project***, ***written*** with ArchUnitNET, do its ***work*** ***between*** them.

| Rule | Guard |
|---|---|
| 1. A small public face | The compiler: an `internal` ***type*** ***stays*** ***inside*** its ***project*** |
| 2. One direction | The compiler: .NET ***refuses*** a ***circle*** of ***project*** ***references***, so the ***build*** ***fails*** |
| 3. One gate to SQL | The compiler: only Sql ***references*** the database ***client***'s ***packages***, Npgsql and Dapper, each ***marked*** `PrivateAssets="compile;contentfiles;analyzers;build"`. The ***mark*** ***stops*** the ***packages*** ***reaching*** the compiler of any ***project*** that ***references*** Sql, while their ***runtime*** ***files*** ***still*** ***reach*** the ***finished*** ***service***; `PrivateAssets="all"` would ***stop*** those too, and the ***service*** would ***fail*** at ***start***. The query builder is `internal` to Sql, so only the ***named*** query ***functions*** in `Queries.cs` can ***make*** a query, and the compiler ***proves*** that every SQL ***text*** ***lives*** there; the builder ***hands*** Dapper ***fixed*** ***text*** and ***bound*** parameters ***alone***. An ArchUnitNET ***test*** ***repeats*** the ***check***, as S-01's ***criterion*** 2 ***names*** ***one*** |
| 4. One shape for errors | An ArchUnitNET ***test***, ***since*** any ***project*** that ***serves*** HTTP can ***write*** a ***body*** |
| 5. Values, not shared state | ***Review***, and the ***coding*** ***style***: immutable ***collections***, and ***nullable*** ***types*** at a module's ***edge*** ***alone*** (ADR 0004) |

## Consequences

**What gets easier**

- Each ***story*** ***changes*** ***one*** or ***two*** modules, so its ***tests*** ***stay*** ***small***.
- S-01's ***architecture*** ***test*** is ***one*** ***rule*** ***among*** the ***border*** ***rules***, not a ***special*** ***case***.
- ***One*** ***release*** ***keeps*** US-35's ***set***-up ***simple***: ***one*** ***process*** ***behind*** ***port*** 80.
- A module that ***later*** ***needs*** its own ***machine*** can ***leave*** along a ***border*** that ***already*** ***exists***.

**What gets harder**

- Each ***story*** ***begins*** with a ***decision***: which module ***owns*** the new ***code***.
- A ***wrong*** ***cut*** ***costs*** ***work***: ***moving*** a ***type*** ***between*** modules ***means*** ***moving*** its ***tests*** too.
- ***Nine*** ***projects*** and their ***references*** are more to ***keep*** than ***one***.
- The ***architecture*** ***tests*** are ***code*** to ***keep***, and a ***rule*** ***neither*** the compiler nor a ***test*** ***checks*** ***goes*** ***unchecked***.

## When to revisit

The ***modular*** monolith ***stands*** until ***one*** of ***three*** ***events*** ***happens***. Each ***reopens*** the ***decision***.

- **A part needs its own machine.** If tiles ***draw*** so ***many*** ***requests*** that the tile ***work*** ***needs*** a ***second*** ***machine*** while the ***rest*** ***stays*** ***put***, Tiles may ***leave*** as a ***separate*** ***service***.
- **A part needs its own release.** If the ***map*** must ***ship*** ***changes*** ***weekly*** while the tile API ***changes*** ***twice*** a ***year***, ***one*** ***release*** for both ***starts*** to ***hurt***.
- **A second maintainer joins.** If ***two*** ***people*** each ***own*** ***part*** of the back ***end***, ***separate*** ***services*** may ***suit*** them ***better*** than ***one*** ***shared*** codebase.

In each ***case***, the module ***borders*** ***mark*** where the ***service*** would ***divide***.
