# Tessera

Tessera is a ***small*** ***web*** ***service*** that ***serves*** a ***map*** of ***places*** as tiles. It ***reads*** ***tables*** from a database, ***cuts*** them into ***map*** tiles on ***request***, and ***answers*** every ***machine*** in the ***office*** on ***port*** 80. This ***build***, Phase Oct, ***serves*** the 900 ***places*** of Macau in ***nine*** ***layers***; the ***service*** is ***finished*** and ***tested***, and the ***map*** that ***shows*** the tiles ***comes*** ***next***.

A **tile** is ***one*** ***square*** of a ***map*** at ***one*** zoom ***level***. A ***web*** ***map*** is ***made*** of ***thousands*** of them, and the browser ***asks*** for only the ***ones*** on the ***screen***. Tessera ***makes*** each tile from the database the ***moment*** a ***map*** ***asks*** for it, so a ***change*** in the ***data*** ***shows*** on the ***map*** at once.

## What this build holds, story by story

The ***sixteen*** ***stories*** in `docs/user-stories/user-stories.md` ***fix*** what Tessera must do. Their ***state*** on 13 ***October*** 2026:

| Story | State |
|---|---|
| US-00 Walking skeleton | ***Built*** and ***tested*** |
| US-01 One database | ***Built*** and ***tested*** |
| US-03 Publish by ***grant*** | ***Built*** and ***tested*** |
| US-24 Places in ***nine*** ***layers*** | ***Built*** and ***tested*** |
| US-07 Serve a tile | ***Built*** and ***tested*** |
| S-01 SQL injection | ***Built*** and ***tested*** |
| S-06 Malformed URLs | ***Built*** and ***tested***, with ***one*** ***gap***: the ***web*** server ***refuses*** `%00` and a ***raw*** ***control*** ***character*** ***itself***, with an ***empty*** 400 in ***place*** of a ***problem*** ***document*** |
| US-35 Office on ***port*** 80 | ***Script*** ***written***; it ***needs*** a ***first*** ***run*** on the ***office*** ***machine*** |
| US-23 and UI-09 to UI-17, the ***map*** | Not ***started*** |

## What you need

- The .NET 10 SDK, 10.0.100 or ***later***: the ***tools*** that ***build*** and ***run*** C# 14
- Docker Engine, which ***runs*** the database in a ***container***: a ***sealed*** ***box*** with its own ***files***
- Node.js 26, which ***runs*** the ***scripts*** (ADR 0002)

## The commands

| Command | What it does |
|---|---|
| `dotnet build` | ***Builds*** the ***service*** and every ***test*** (US-00 c1) |
| `dotnet run --project tests/Tessera.UnitTests` | ***Runs*** the ***unit*** and ***property*** ***tests***: no database, a ***second*** or ***two*** |
| `dotnet run --project tests/Tessera.ArchitectureTests` | ***Checks*** the ***rules*** at the module ***borders*** (ADR 0001) |
| `dotnet run --project tests/Tessera.IntegrationTests` | ***Starts*** the ***packaged*** database in Docker and ***tests*** through it and through ***real*** HTTP (US-00 c3) |
| `npm install` | ***Installs*** the ***scripts***' ***tools***: zx and TypeScript |
| `npm start` | ***Starts*** the database, ***imports*** the ***places*** if the ***tables*** are ***empty***, then ***starts*** the ***service*** (US-00 c2) |
| `npm run publish` | ***Grants*** the ***nine*** ***layers*** to the ***service***, which ***serves*** them after a restart (US-24 c5) |
| `npm run office-port` | ***Prints*** the ***steps*** that ***open*** ***port*** 80 to the ***office*** (US-35) |

***Copy*** `.env.example` to `.env` and ***give*** each ***password*** a ***value*** before `npm start`. Git ***ignores*** `.env`, so no ***password*** ***reaches*** the repository.

## Where the settings live

***Machines***, ***ports*** and ***passwords*** ***come*** from ***environment*** variables: ***values*** the ***operating*** ***system*** ***hands*** a ***program*** when it ***starts***. The ***service*** ***refuses*** to ***start*** without them and ***names*** each ***one*** it ***lacks*** (US-00 c4). Every other ***setting*** ***lives*** in `tessera.json`: the ***area***'s ***name***, ***box*** and ***home*** ***view***, the ***map*** preview, and the tile's ***cap*** on ***features*** and buffer (ADR 0003).

| Variable | Read by | Holds |
|---|---|---|
| `TESSERA_HTTP_PORT` | The ***service*** | The ***port*** it ***answers*** on: 80 in the ***office*** |
| `DATABASE_URL` | The ***service*** | `postgresql://tessera_reader:<password>@<host>:<port>/tessera` |
| `TESSERA_PUBLISHER_DATABASE_URL` | `import` | `postgresql://tessera_publisher:<password>@<host>:<port>/tessera` |
| `TESSERA_CONFIG` | The ***service*** | ***Optional***: a ***path*** to `tessera.json` ***elsewhere*** |

## How the database keeps the service safe

Tessera ***follows*** pg_tileserv's ***rule***: the database ***decides*** what the ***map*** ***shows***. The ***service*** ***signs*** in as `tessera_reader`, a ***role*** that may only ***read***, and a ***table*** ***becomes*** a ***layer*** only when that ***role*** is ***granted*** it. The ***import*** ***signs*** in as `tessera_publisher`, which ***owns*** the ***tables*** but ***never*** ***grants*** them on its own; ***publishing*** is the publisher's own ***act***.

***Two*** ***attacks*** are ***closed*** by ***design***. Every ***value*** from a ***request*** ***travels*** beside the query as a parameter, ***never*** ***inside*** its ***text***, and every ***name*** ***enters*** the ***text*** ***quoted***, through ***one*** ***function***; so no ***request*** can smuggle in a ***command*** (SQL injection, S-01). A ***request*** with a disguised ***address***, such as `%2e%2e` for `..`, is ***refused*** before any ***route*** ***reads*** it (a malformed URL, S-06).

## What is where

The ***service*** is ***one*** ***program*** ***cut*** into ***seven*** modules, each a ***project*** of its own, so the compiler ***guards*** the ***walls*** ***between*** them (ADR 0001).

| Folder | What it holds |
|---|---|
| `src/Tessera.Kernel` | `Option` and `Result`, the ***two*** ***types*** every module ***uses*** for ***missing*** ***values*** and ***refusals*** (ADR 0004) |
| `src/Tessera.Settings` | ***Reading*** `tessera.json` and the ***environment*** |
| `src/Tessera.Sql` | The ***one*** query builder and the ***only*** ***code*** that ***talks*** to the database |
| `src/Tessera.Catalogue` | The ***list*** of ***layers***: every ***table*** ***granted*** to the ***service*** |
| `src/Tessera.Import` | The `import` ***command***: ***checks*** the GeoJSON ***seed*** and ***loads*** the ***nine*** ***tables*** |
| `src/Tessera.Tiles` | The tile ***route*** and the tile query |
| `src/Tessera.Http` | The ***screen*** for disguised ***addresses***, and every ***problem*** ***document*** |
| `src/Tessera.Map` | ***Serving*** the ***map***'s ***files***, once the ***map*** ***exists*** |
| `src/Tessera.Service` | The ***program***, which ***puts*** the modules ***together*** |
| `tests/` | ***Unit***, ***property***, ***architecture*** and integration ***tests*** |
| `db/` | The ***packaged*** database: PostgreSQL 18 with PostGIS 3.6, and the ***role*** ***script*** |
| `data/` | The ***seed***: 900 ***places*** in longitude and latitude, as `nathanwh71/JTessera` ***holds*** them |
| `scripts/` | The ***scripts***, in TypeScript with zx (ADR 0002) |
| `docs/` | The ***stories*** and the ***architecture*** ***decision*** ***records*** |

## The documents

- `docs/user-stories/user-stories.md`: the ***sixteen*** ***stories*** of Phase Oct, in ***build*** ***order***
- `docs/adr/0001-modular-monolith.md`: why the ***service*** is ***one*** ***program*** in ***seven*** modules
- `docs/adr/0002-scripts-in-typescript-with-zx.md`: why the ***scripts*** are TypeScript on Node.js 26
- `docs/adr/0003-settings-file.md`: why ***settings*** ***live*** in `tessera.json` and the ***environment***
- `docs/adr/0004-functional-style.md`: why the ***code*** is ***functional***, with `Option` and `Result`
- `docs/adr/0006-one-map-for-both-builds.md`: why the Java and .NET ***builds*** ***serve*** ***one*** ***map*** from a repository of its own

The ***phase***-0 ***build*** and its ***documents*** ***stay*** in the ***archived*** ***phase***-0 repository, whose ***last*** ***commit*** is `ecb21fb`.

## Comments in the code

Every ***comment*** is ***written*** for a ***school*** ***student*** of ***computer*** ***science***. It ***explains*** each ***technical*** ***word*** the ***first*** ***time*** a ***file*** ***uses*** it, and it ***says*** why the ***code*** is as it is: the ***reason***, the ***rule***, the ***limit*** and the ***story*** ***behind*** it, such as `US-24 c6`.
