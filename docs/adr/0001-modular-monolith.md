# ADR 0001: Build the back end as a modular monolith, with the compiler guarding its borders

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 15 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | The ***Phase*** Oct ***back*** ***end***, in C# 14 on .NET 10, in `nathanwh71/Tessera` |
| Stories | US-00, US-01, US-03, US-24, US-07, S-01, S-06, US-35, US-23 (`docs/user-stories/user-stories.md`) |

## Decision

***Tessera***'s ***back*** ***end*** is ***one*** ***service***, ***built*** and ***shipped*** as ***one*** ***unit***, and ***divided*** ***inside*** into ***modules*** with ***fixed*** ***borders***. ***Each*** ***module*** is a ***project*** of its ***own***, in a ***folder*** of `src/Tessera`, and ***names*** ***only*** the ***modules*** it may ***use***. The ***compiler*** ***guards*** the ***borders***: a ***module*** that ***reaches*** into ***one*** it does not ***name***, or a ***circle*** of ***modules***, ***fails*** the ***build***.

This ***record*** ***covers*** the ***back*** ***end*** ***alone***. The ***map***'s ***front*** ***end***, in TypeScript, ***keeps*** its ***own*** ***design*** (ADR 0006).

## Context

***Five*** ***facts*** ***shape*** the ***decision***.

- **One maintainer.** ***One*** ***person*** ***builds***, ***runs*** and ***repairs*** ***Tessera***, and should not have to ***remember*** which ***module*** may ***use*** which.
- **One machine.** The ***service*** ***runs*** in Ubuntu 24.04 ***inside*** WSL2, on ***one*** ***Windows*** 11 ***machine***, ***behind*** Nginx on ***port*** 80 (US-35, ADR 0008).
- **One program.** The ***same*** ***service*** ***serves*** the ***tiles*** (US-07) and the ***map***'s ***files*** (US-23), and ***ships*** them in ***one*** ***release***; `npm start` ***runs*** ***two*** ***copies*** of it ***behind*** Nginx (N-29).
- **One gate to the database.** S-01 ***requires*** that every SQL ***text*** ***lives*** in ***one*** ***file*** and that ***one*** ***module*** ***sends*** it.
- **One place for the design.** The ***stories*** ***leave*** the ***design*** to the ***team*** on ***purpose***; this ***record*** ***holds*** it.

## Options considered

| Option | For | Against |
|---|---|---|
| A project for each module, in the folders of one service (chosen) | The ***compiler*** ***guards*** every ***border***, and the ***folders*** ***read*** as ***one*** ***program***. A ***module***'s ***file*** ***names*** its every ***neighbour***. | ***Eight*** ***project*** ***files*** to ***keep***, ***each*** a ***few*** ***lines***. |
| One project, a folder for each module | ***One*** ***file*** to ***build***. | ***Nothing*** but ***review*** ***guards*** the ***borders***; ***two*** ***circles*** of ***modules*** ***grew*** unnoticed. |
| An architecture test that reads the code | ***One*** ***project***, and a ***failing*** ***test*** for a ***break***. | A ***text*** ***test*** misses ***some*** ***routes*** ***round*** it; a ***test*** ***library***, such as ArchUnitNET, ***adds*** a ***dependency***. |
| Separate services | The ***network*** ***enforces*** the ***borders***. | ***Several*** ***releases*** on ***one*** ***machine***, and ***network*** ***calls*** ***between*** ***parts***, for ***one*** ***maintainer*** who ***gains*** ***nothing*** from them. |

## The modules

| Project | Owns | May use | Stories |
|---|---|---|---|
| `Tessera.Problems` | The ***shape*** of every ***problem*** ***document*** | ***Nothing*** | US-07 |
| `Tessera.Settings` | ***Reading*** `tessera.json` and the ***environment***, and ***refusing*** to ***start*** when a ***value*** is ***wrong*** (ADR 0003) | ***Nothing*** | US-00 |
| `Tessera.Sql` | Every ***query***'s ***text***, in `Queries.cs`; the ***database***'s URL; the ***one*** ***pool*** and the ***one*** ***class***, `Database`, that ***sends*** ***queries*** ***through*** Npgsql with Dapper | ***Settings*** | S-01, US-03 |
| `Tessera.Catalogue` | The ***list*** of ***granted*** ***tables*** and ***views***, ***read*** at start-up and ***again*** every `Catalogue.RefreshSeconds` | Sql | US-03, N-23 |
| `Tessera.Seeding` | The `seed` ***command***: DbUp ***runs*** ***each*** ***new*** ***script*** in `db/seed` ***once***, as `tessera_publisher` (ADR 0009) | Sql | US-24 |
| `Tessera.Tiles` | The ***tile*** ***route***, the ***tile*** ***query***'s ***inputs***, and the ***headers*** that ***let*** a ***browser*** and Nginx ***keep*** a ***tile*** | Catalogue, ***Settings***, Sql, ***Problems*** | US-07, N-18 |
| `Tessera.Map` | ***Serving*** the ***front*** ***end***'s ***built*** ***files***, and ***writing*** the ***area*** into its ***page*** | ***Settings***, ***Problems*** | US-23 |

`Tessera`, the ***program***, ***holds*** `Program.cs` and `Service.cs`. It ***names*** every ***module***, ***puts*** them ***together***, and is the ***only*** ***project*** that ***ships***. No ***module*** ***names*** it.

A ***module*** ***joins*** the ***list*** when a ***story*** ***needs*** ***one***, and ***leaves*** when ***none*** does.

**The seed is a command of the service.** The ***same*** ***program*** that ***serves*** ***tiles*** ***runs*** the ***seed*** when ***called*** with `seed`, then ***stops***. Its ***checks*** and its ***tests*** ***live*** in C#, ***beside*** the ***rest*** of the ***back*** ***end***.

**Each program signs in as its own role.** The ***service*** ***signs*** in as `tessera_reader`, which ***reads*** ***granted*** ***tables*** ***alone*** (US-03). The ***seed*** ***signs*** in as `tessera_publisher`, the ***data*** ***publisher***'s ***role***, which ***owns*** the schemas and their ***tables***. It ***creates*** and ***fills*** them; the ***publisher*** ***grants*** them when ***ready*** (US-24, ***criterion*** 5).

| Variable | Read by | Holds |
|---|---|---|
| `DATABASE_URL` | The ***service***, at every ***start*** | `postgresql://tessera_reader:<password>@<host>:<port>/<database>` |
| `TESSERA_PUBLISHER_DATABASE_URL` | `seed`, ***only*** when it ***runs*** | `postgresql://tessera_publisher:<password>@<host>:<port>/<database>` |
| `TESSERA_READER_PASSWORD` | The ***role*** ***script***, on the ***database***'s ***first*** ***start*** | `tessera_reader`'s ***password*** |
| `TESSERA_PUBLISHER_PASSWORD` | The ***role*** ***script***, on the ***database***'s ***first*** ***start*** | `tessera_publisher`'s ***password*** |

***Each*** URL ***carries*** the ***machine***'s ***name***, ***address*** and ***port***, as US-00 ***requires***; `Sql` ***turns*** it into a ***connection*** ***string*** for the ***database*** ***client***. The ***service*** ***never*** ***reads*** `TESSERA_PUBLISHER_DATABASE_URL`, so it ***never*** ***holds*** the ***publisher***'s ***rights*** ***while*** it ***serves*** ***tiles***.

**`npm start` runs the seed at every start.** The ***script*** ***starts*** the ***database***, ***waits*** until it ***answers***, and ***runs*** `seed`. DbUp ***runs*** ***only*** the ***scripts*** it has not ***run*** ***before***, so a ***start*** with ***nothing*** ***new*** ***changes*** ***nothing***.

## Rules at the borders, and what guards each

| Rule | Guard |
|---|---|
| 1. **A small public face.** Each module shows only the types other modules need. | The ***compiler***: an `internal` ***type*** ***stays*** ***inside*** its ***project***. |
| 2. **One direction.** Modules depend on one another in one direction, with no circle. | The ***compiler***: .NET ***refuses*** a ***circle*** of ***project*** ***references***. |
| 3. **Named neighbours alone.** A module uses only the modules its project file names. | The ***compiler***: `DisableTransitiveProjectReferences`, ***set*** in `src/Tessera/Directory.Build.props`, ***stops*** a ***module*** ***seeing***, ***say***, ***Settings*** ***through*** Sql. |
| 4. **One gate to SQL.** Every SQL text lives in `Queries.cs`, and only `Database` sends it (S-01, criterion 2). | The ***compiler***: ***only*** `Tessera.Sql` ***names*** Npgsql and Dapper, ***each*** ***marked*** `PrivateAssets="compile;contentfiles;analyzers;build"`, so no ***project*** that ***names*** Sql can ***use*** them; their ***files*** ***still*** ***reach*** the ***finished*** ***service***. `Tessera.Seeding` ***reaches*** the ***database*** ***through*** DbUp, the ***one*** ***other*** ***door*** (ADR 0009). |
| 5. **One shape for errors.** Every problem document takes its shape from `Tessera.Problems`; no route writes an error body of its own (US-07). | ***Review***. |
| 6. **Values, not shared state.** Modules pass immutable values to one another, and report an expected failure as a `Result`, never as a shared object or an exception (ADR 0004). | ***Review***. |

The ***tile*** ***query*** ***shows*** ***rule*** 2 at ***work***. `Queries.Tile` ***takes*** a `TileSource`, a ***record*** of `Sql`'s ***own***, and ***plain*** ***numbers*** for the ***tile***, so `Sql` ***needs*** ***nothing*** of `Catalogue`'s or `Tiles`'. `Catalogue`'s `Layer` ***hands*** over its `Source`.

## Consequences

**What gets easier**

- A ***break*** of ***rules*** 2, 3 or 4 ***fails*** the ***build*** at ***once***, with the ***compiler***'s ***own*** ***message***.
- ***Each*** ***module***'s ***project*** ***file*** ***lists*** its ***neighbours***, so the ***design*** ***reads*** from ***eight*** ***short*** ***files***.
- ***Each*** ***story*** ***changes*** ***one*** or ***two*** ***modules***, so its ***tests*** ***stay*** ***small***.
- ***One*** ***release*** ***keeps*** US-35's set-up ***simple***: ***one*** ***program*** ***behind*** Nginx.
- A ***module*** that ***later*** ***needs*** its ***own*** ***machine*** can ***leave*** along a ***border*** that ***already*** ***exists***.

**What gets harder**

- ***Each*** ***story*** ***begins*** with a ***decision***: which ***module*** ***owns*** the ***new*** ***code***.
- A ***new*** ***use*** of ***one*** ***module*** by another ***needs*** a ***line*** in a ***project*** ***file***, and a ***moment***'s ***thought*** ***about*** ***whether*** it ***belongs***.
- The ***release*** ***holds*** ***one*** ***file*** for ***each*** ***module***, ***eight*** in all, ***beside*** the ***program***.

## When to revisit

The ***modular*** monolith ***stands*** until ***one*** of ***three*** ***events*** ***happens***. ***Each*** ***reopens*** the ***decision***.

- **A part needs its own machine.** If ***tiles*** ***draw*** so ***many*** ***requests*** that the ***tile*** ***work*** ***needs*** a ***second*** ***machine*** ***while*** the ***rest*** ***stays*** ***put***, `Tessera.Tiles` may ***leave*** as a ***separate*** ***service***.
- **A part needs its own release.** If the ***map*** must ***ship*** ***changes*** ***weekly*** ***while*** the ***tile*** API ***changes*** ***twice*** a ***year***, ***one*** ***release*** for both ***starts*** to ***hurt***.
- **A second maintainer joins.** If ***two*** ***people*** ***each*** ***own*** ***part*** of the ***back*** ***end***, ***separate*** ***services*** may ***suit*** them ***better***.

In ***each*** ***case***, the ***module*** ***borders*** ***mark*** where the ***service*** would ***divide***.
