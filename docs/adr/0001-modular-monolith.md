# ADR 0001: Build the back end as a modular monolith

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | The ***Phase*** Oct ***back*** ***end***, in C# 14 on .NET 10, in `nathanwh71/Tessera` |
| Stories | US-00, US-01, US-03, US-24, US-07, S-01, S-06, US-35, US-23 (`docs/user-stories/user-stories.md`) |

## Decision

***Tessera***'s ***back*** ***end*** is ***one*** ***service***, ***built*** and ***shipped*** as ***one*** ***unit***, and ***divided*** ***inside*** into ***modules*** with ***fixed*** ***borders***. The ***service*** is ***one*** ***project***, `src/Tessera`, with a ***folder*** for ***each*** ***module***. ***Each*** ***module*** ***owns*** ***one*** ***part*** of the ***work*** the ***stories*** ***describe***, ***shows*** ***other*** ***modules*** a ***small*** ***public*** ***face***, and ***hides*** the ***rest***. ***Review*** ***guards*** the ***borders***.

This ***record*** ***covers*** the ***back*** ***end*** ***alone***. The ***map***'s ***front*** ***end***, in TypeScript, ***keeps*** its ***own*** ***design*** (ADR 0006).

## Context

***Five*** ***facts*** ***shape*** the ***decision***.

- **One maintainer.** ***One*** ***person*** ***builds***, ***runs*** and ***repairs*** ***Tessera***, and ***reads*** ***one*** ***project*** more easily than ***many***.
- **One machine.** The ***service*** ***runs*** in Ubuntu 24.04 ***inside*** WSL2, on ***one*** ***Windows*** 11 ***machine***, ***behind*** Nginx on ***port*** 80 (US-35, ADR 0008).
- **One program.** The ***same*** ***service*** ***serves*** the ***tiles*** (US-07) and the ***map***'s ***files*** (US-23), and ***ships*** them in ***one*** ***release***; `npm start` ***runs*** ***two*** ***copies*** of it ***behind*** Nginx (N-29).
- **One gate to the database.** S-01 ***requires*** that every SQL ***text*** ***lives*** in ***one*** ***file*** and that ***one*** ***class*** ***sends*** it.
- **One place for the design.** The ***stories*** ***leave*** the ***design*** to the ***team*** on ***purpose***; this ***record*** ***holds*** it.

## Options considered

| Option | For | Against |
|---|---|---|
| Modular monolith (chosen) | ***One*** ***build***, ***one*** ***release*** and ***one*** ***process*** to ***watch***. ***Each*** ***story***'s ***code*** ***lives*** in ***one*** ***module***. S-01's ***rule*** ***follows*** from the ***borders***. | The ***borders*** ***need*** ***thought*** and ***review*** to ***hold***. |
| A project for each module | The ***compiler*** ***guards*** the ***borders***. | ***Nine*** ***projects*** and their ***references***, ***too*** ***many*** for ***one*** ***maintainer*** to ***follow***. |
| Layered monolith (routes, services, data) | ***Familiar*** and ***quick*** to ***start***. | ***Each*** ***story*** ***spreads*** ***across*** every ***layer***. ***Nothing*** ***stops*** a ***route*** from ***calling*** the ***database***, so S-01 ***needs*** a ***rule*** of its ***own***. |
| Separate services | The ***network*** ***enforces*** the ***borders***. | ***Several*** ***releases*** on ***one*** ***machine***, and ***network*** ***calls*** ***between*** ***parts***, for ***one*** ***maintainer*** who ***gains*** ***nothing*** from them. |

## The modules

| Folder | Owns | Stories |
|---|---|---|
| `Settings` | ***Reading*** `tessera.json` and the ***environment***, and ***refusing*** to ***start*** when a ***value*** is ***wrong*** (ADR 0003) | US-00 |
| `Sql` | Every ***query***'s ***text***, in `Queries.cs`; the ***one*** ***pool*** and the ***one*** ***class***, `Database`, that ***sends*** ***queries*** ***through*** Npgsql with Dapper | S-01, US-03 |
| `Catalogue` | The ***list*** of ***granted*** ***tables*** and ***views***, ***read*** at start-up and ***again*** every `Catalogue.RefreshSeconds` | US-03, N-23 |
| `Seeding` | The `seed` ***command***: DbUp ***runs*** ***each*** ***new*** ***script*** in `db/seed` ***once***, as `tessera_publisher` (ADR 0009) | US-24 |
| `Tiles` | The ***tile*** ***route***, the ***tile*** ***query***, and the ***headers*** that ***let*** a ***browser*** and Nginx ***keep*** a ***tile*** | US-07, N-18 |
| `Map` | ***Serving*** the ***front*** ***end***'s ***built*** ***files***, and ***writing*** the ***area*** into its ***page*** | US-23 |

***Three*** ***files*** ***stand*** ***beside*** the ***folders***. `Problems.cs` ***shapes*** every ***problem*** ***document***. `Service.cs` ***puts*** the ***modules*** ***together***. `Program.cs` is the ***program***'s ***starting*** ***point***.

A ***module*** ***joins*** the ***list*** when a ***story*** ***needs*** ***one***, and ***leaves*** when ***none*** does.

**The seed is a command of the service.** The ***same*** ***program*** that ***serves*** ***tiles*** ***runs*** the ***seed*** when ***called*** with `seed`, then ***stops***. Its ***checks*** and its ***tests*** ***live*** in C#, ***beside*** the ***rest*** of the ***back*** ***end***.

**Each program signs in as its own role.** The ***service*** ***signs*** in as `tessera_reader`, which ***reads*** ***granted*** ***tables*** ***alone*** (US-03). The ***seed*** ***signs*** in as `tessera_publisher`, the ***data*** ***publisher***'s ***role***, which ***owns*** the schemas and their ***tables***. It ***creates*** and ***fills*** them; the ***publisher*** ***grants*** them when ***ready*** (US-24, ***criterion*** 5).

| Variable | Read by | Holds |
|---|---|---|
| `DATABASE_URL` | The ***service***, at every ***start*** | `postgresql://tessera_reader:<password>@<host>:<port>/<database>` |
| `TESSERA_PUBLISHER_DATABASE_URL` | `seed`, ***only*** when it ***runs*** | `postgresql://tessera_publisher:<password>@<host>:<port>/<database>` |
| `TESSERA_READER_PASSWORD` | The ***role*** ***script***, on the ***database***'s ***first*** ***start*** | `tessera_reader`'s ***password*** |
| `TESSERA_PUBLISHER_PASSWORD` | The ***role*** ***script***, on the ***database***'s ***first*** ***start*** | `tessera_publisher`'s ***password*** |

***Each*** URL ***carries*** the ***machine***'s ***name***, ***address*** and ***port***, as US-00 ***requires***; `Settings` ***turns*** it into a ***connection*** ***string*** for the ***database*** ***client***. The ***service*** ***never*** ***reads*** `TESSERA_PUBLISHER_DATABASE_URL`, so it ***never*** ***holds*** the ***publisher***'s ***rights*** ***while*** it ***serves*** ***tiles***.

**`npm start` runs the seed at every start.** The ***script*** ***starts*** the ***database***, ***waits*** until it ***answers***, and ***runs*** `seed`. DbUp ***runs*** ***only*** the ***scripts*** it has not ***run*** ***before***, so a ***start*** with ***nothing*** ***new*** ***changes*** ***nothing***.

## Rules at the borders

1. **A small public face.** ***Each*** ***module*** ***shows*** ***only*** the ***types*** ***other*** ***modules*** ***need***; ***everything*** ***else*** is `internal` or `private`.
2. **One direction.** ***Modules*** ***depend*** on ***one*** another in ***one*** ***direction***, with no ***circle***.
3. **One gate to SQL.** Every SQL ***text*** ***lives*** in `Queries.cs`, and ***only*** `Database` ***sends*** it (S-01, ***criterion*** 2). DbUp, in `Seeding`, ***opens*** ***connections*** of its ***own*** to ***run*** the ***seed*** ***scripts***; that is the ***one*** ***other*** ***door*** (ADR 0009).
4. **One shape for errors.** Every ***problem*** ***document*** ***takes*** its ***shape*** from `Problems.cs`; no ***route*** ***writes*** an ***error*** ***body*** of its ***own*** (US-07).
5. **Values, not shared state.** ***Modules*** ***pass*** immutable ***values*** to ***one*** another, and ***report*** an ***expected*** ***failure*** as a `Result`, ***never*** as a ***shared*** ***object*** or an exception (ADR 0004).

***Review*** ***guards*** all ***five***. A ***search*** at ***each*** ***review*** ***finds*** SQL ***text*** ***nowhere*** ***outside*** `Queries.cs` and the ***seed*** ***scripts***.

## Consequences

**What gets easier**

- ***Each*** ***story*** ***changes*** ***one*** or ***two*** ***modules***, so its ***tests*** ***stay*** ***small***.
- ***One*** ***project*** is ***quick*** to ***open***, ***build*** and ***read***.
- ***One*** ***release*** ***keeps*** US-35's set-up ***simple***: ***one*** ***process*** ***behind*** Nginx.
- A ***module*** that ***later*** ***needs*** its ***own*** ***machine*** can ***leave*** along a ***border*** that ***already*** ***exists***.

**What gets harder**

- ***Each*** ***story*** ***begins*** with a ***decision***: which ***module*** ***owns*** the ***new*** ***code***.
- The ***compiler*** ***cannot*** ***see*** the ***borders***, so a careless ***change*** can ***cross*** ***one***; ***review*** must ***catch*** it.

## When to revisit

The ***modular*** monolith ***stands*** until ***one*** of ***four*** ***events*** ***happens***. ***Each*** ***reopens*** the ***decision***.

- **A part needs its own machine.** If ***tiles*** ***draw*** so ***many*** ***requests*** that the ***tile*** ***work*** ***needs*** a ***second*** ***machine*** ***while*** the ***rest*** ***stays*** ***put***, `Tiles` may ***leave*** as a ***separate*** ***service***.
- **A part needs its own release.** If the ***map*** must ***ship*** ***changes*** ***weekly*** ***while*** the ***tile*** API ***changes*** ***twice*** a ***year***, ***one*** ***release*** for both ***starts*** to ***hurt***.
- **A second maintainer joins.** If ***two*** ***people*** ***each*** ***own*** ***part*** of the ***back*** ***end***, a ***project*** for ***each*** ***module***, or ***separate*** ***services***, may ***suit*** them ***better***.
- **A border breaks in review more than once.** A ***project*** for ***each*** ***module*** then ***lets*** the ***compiler*** ***guard*** it.

In ***each*** ***case***, the ***module*** ***borders*** ***mark*** where the ***service*** would ***divide***.
