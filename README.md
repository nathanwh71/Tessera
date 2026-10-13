# Tessera

> **The live build, since 14 October 2026.** The maintainer ***chose*** this .NET ***build*** as the ***one*** to ***carry*** on; the Java ***build***, `nathanwh71/JTessera`, is ***archived*** and ***kept*** to ***read***. The ***map*** in `web/` ***now*** ***lives*** here (ADR 0006, as ***amended***).

Tessera is a ***small*** ***web*** ***service*** that ***serves*** a ***map*** of ***places*** as tiles. It ***reads*** ***tables*** from a database, ***cuts*** them into ***map*** tiles on ***request***, and ***answers*** every ***machine*** in the ***office*** on ***port*** 80. This ***build***, Phase Oct, ***serves*** the 900 ***places*** of Macau in ***nine*** ***layers***, and ***shows*** them on a ***map*** at `/`.

A **tile** is ***one*** ***square*** of a ***map*** at ***one*** zoom ***level***. A ***web*** ***map*** is ***made*** of ***thousands*** of them, and the browser ***asks*** for only the ***ones*** on the ***screen***. Tessera ***makes*** each tile from the database the ***moment*** a ***map*** ***asks*** for it, so a ***change*** in the ***data*** ***shows*** on the ***map*** at once.

## What this build holds, story by story

The ***nineteen*** ***stories*** in `docs/user-stories/user-stories.md` ***fix*** what Tessera must do. Their ***state*** on 14 ***October*** 2026:

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
| US-23 and UI-09 to UI-17, the ***map*** | ***Built*** and ***tested***: the ***map*** ***copied*** from the Java ***build*** (ADR 0006), and its 20 browser ***tests*** ***pass*** ***against*** this ***service*** |
| N-18 to N-20, tiles that ***cost*** ***less*** | ***Built*** and ***tested***: a tile ***carries*** `max-age` and a ***weak*** `ETag`, a ***kept*** ***copy*** ***answers*** without a query, and tiles and the ***map***'s ***files*** ***travel*** gzip-packed |

## What you need

- The .NET 10 SDK, 10.0.100 or ***later***: the ***tools*** that ***build*** and ***run*** C# 14
- Docker Engine, which ***runs*** the database in a ***container***: a ***sealed*** ***box*** with its own ***files***
- Node.js 26, which ***runs*** the ***scripts*** and ***builds*** the ***map*** (ADR 0002)

## The commands

| Command | What it does |
|---|---|
| `npm run build` | ***Builds*** the ***map***, then the ***service*** with the ***map*** ***inside*** it (US-00 c1) |
| `npm test` | ***Runs*** every ***test***: the ***scripts***', the ***map***'s, and the ***service***'s ***three*** ***suites*** (US-00 c3) |
| `dotnet build` | ***Builds*** the ***service*** and every ***test*** ***alone***; ***needs*** `web/dist` from a ***map*** ***build*** |
| `dotnet run --project tests/Tessera.UnitTests` | ***Runs*** the ***unit*** and ***property*** ***tests***: no database, a ***second*** or ***two*** |
| `dotnet run --project tests/Tessera.ArchitectureTests` | ***Checks*** the ***rules*** at the module ***borders*** (ADR 0001) |
| `dotnet run --project tests/Tessera.IntegrationTests` | ***Starts*** the ***packaged*** database in Docker and ***tests*** through it and through ***real*** HTTP (US-00 c3) |
| `npm install` | ***Installs*** the ***scripts***' ***tools***: zx and TypeScript |
| `npm start` | ***Starts*** the database, ***imports*** the ***places*** if the ***tables*** are ***empty***, then ***starts*** the ***service*** (US-00 c2) |
| `npm run publish` | ***Grants*** the ***nine*** ***layers*** to the ***service***, which ***serves*** them after a restart (US-24 c5) |
| `npm run office-port` | ***Prints*** the ***steps*** that ***open*** ***port*** 80 to the ***office*** (US-35) |
| `cd web && TESSERA_BASE_URL=http://localhost:8080 npx playwright test` | ***Runs*** the ***map***'s browser ***tests*** ***against*** a ***running*** ***service*** |

***Copy*** `.env.example` to `.env` and ***give*** each ***password*** a ***value*** before `npm start`. Git ***ignores*** `.env`, so no ***password*** ***reaches*** the repository.

## Where the settings live

***Machines***, ***ports*** and ***passwords*** ***come*** from ***environment*** variables: ***values*** the ***operating*** ***system*** ***hands*** a ***program*** when it ***starts***. The ***service*** ***refuses*** to ***start*** without them and ***names*** each ***one*** it ***lacks*** (US-00 c4). Every other ***setting*** ***lives*** in `tessera.json`: the ***area***'s ***name***, ***box*** and ***home*** ***view***, the ***map*** preview, the tile's ***cap*** on ***features*** and buffer, how ***long*** and how ***much*** the ***service*** and the browsers ***keep*** tiles, the database ***pool***'s ***limits***, and how ***often*** the ***list*** of ***layers*** is ***reread***. The ***file*** ***uses*** pg_tileserv's ***own*** ***key*** ***names***, such as `CacheTTL` and `DbPoolMaxConns`, and any ***key*** ***yields*** to `TESSERA_` and its ***name***, such as `TESSERA_CACHETTL` (ADR 0003).

| Variable | Read by | Holds |
|---|---|---|
| `TESSERA_HTTPPORT` | The ***service*** | The ***port*** it ***answers*** on: 80 in the ***office***; it ***overrides*** `HttpPort`, as pg_tileserv's `TS_HTTPPORT` does |
| `DATABASE_URL` | The ***service*** | `postgresql://tessera_reader:<password>@<host>:<port>/tessera` |
| `TESSERA_PUBLISHER_DATABASE_URL` | `import` | `postgresql://tessera_publisher:<password>@<host>:<port>/tessera` |
| `TESSERA_CONFIG` | The ***service*** | ***Optional***: a ***path*** to `tessera.json` ***elsewhere*** |
| `TESSERA_DB_PASSWORD`, `TESSERA_READER_PASSWORD`, `TESSERA_PUBLISHER_PASSWORD` | `npm start`, which ***writes*** the ***two*** URLs ***above*** from them | The ***three*** ***roles***' ***passwords*** |
| `TESSERA_DB_HOST`, `TESSERA_DB_PORT`, `TESSERA_DB_NAME` | `npm start`, `npm run publish` | ***Optional***: where the database ***answers***, 127.0.0.1, 5432 and `tessera` by ***default*** |

## How the database keeps the service safe

Tessera ***follows*** pg_tileserv's ***rule***: the database ***decides*** what the ***map*** ***shows***. The ***service*** ***signs*** in as `tessera_reader`, a ***role*** that may only ***read***, and a ***table*** ***becomes*** a ***layer*** only when that ***role*** is ***granted*** it. The ***import*** ***signs*** in as `tessera_publisher`, which ***owns*** the ***tables*** but ***never*** ***grants*** them on its own; ***publishing*** is the publisher's own ***act***.

***Two*** ***attacks*** are ***closed*** by ***design***. Every ***value*** from a ***request*** ***travels*** beside the query as a parameter, ***never*** ***inside*** its ***text***, and every ***name*** ***enters*** the ***text*** ***quoted***, through ***one*** ***function***; so no ***request*** can smuggle in a ***command*** (SQL injection, S-01). A ***request*** with a disguised ***address***, such as `%2e%2e` for `..`, is ***refused*** before any ***route*** ***reads*** it (a malformed URL, S-06).

## What is where

The ***service*** is ***one*** ***program*** ***cut*** into ***seven*** modules, each a ***project*** of its own, so the compiler ***guards*** the ***walls*** ***between*** them (ADR 0001).

| Folder | What it holds |
|---|---|
| `src/Tessera.Kernel` | `Option` and `Result`, the ***two*** ***types*** every module ***uses*** for ***missing*** ***values*** and ***refusals*** (ADR 0004) |
| `src/Tessera.Settings` | ***Reading*** `tessera.json` and the ***environment*** |
| `src/Tessera.Sql` | Every query's ***text***, the ***one*** query builder ***behind*** it, and the ***only*** ***code*** that ***talks*** to the database |
| `src/Tessera.Catalogue` | The ***list*** of ***layers***: every ***table*** ***granted*** to the ***service*** |
| `src/Tessera.Import` | The `import` ***command***: ***checks*** the GeoJSON ***seed*** and ***loads*** the ***nine*** ***tables*** |
| `src/Tessera.Tiles` | The tile ***route***, the tile query, and the ***kept*** ***copies*** with their ***tags*** |
| `src/Tessera.Http` | The ***screen*** for disguised ***addresses***, and every ***problem*** ***document*** |
| `src/Tessera.Map` | ***Serving*** the ***map***'s ***page***, with the ***area*** ***written*** in, and its ***files*** |
| `web/` | The ***map***: TypeScript, React, MapLibre GL JS, react-elmish and Tailwind CSS, ***copied*** from the Java ***build*** (ADR 0006) |
| `src/Tessera.Service` | The ***program***, which ***puts*** the modules ***together*** |
| `tests/` | ***Unit***, ***property***, ***architecture*** and integration ***tests*** |
| `db/` | The ***packaged*** database: PostgreSQL 18 with PostGIS 3.6, and the ***role*** ***script*** |
| `data/` | The ***seed***: 900 ***places*** in longitude and latitude, as `nathanwh71/JTessera` ***holds*** them |
| `scripts/` | The ***scripts***, in TypeScript with zx (ADR 0002) |
| `docs/` | The ***stories*** and the ***architecture*** ***decision*** ***records*** |

## The documents

- `docs/user-stories/user-stories.md`: the ***nineteen*** ***stories*** of Phase Oct, in ***build*** ***order***
- `docs/adr/0001-modular-monolith.md`: why the ***service*** is ***one*** ***program*** in ***seven*** modules
- `docs/adr/0002-scripts-in-typescript-with-zx.md`: why the ***scripts*** are TypeScript on Node.js 26
- `docs/adr/0003-settings-file.md`: why ***settings*** ***live*** in `tessera.json` and the ***environment***
- `docs/adr/0004-functional-style.md`: why the ***code*** is ***functional***, with `Option` and `Result`
- `docs/adr/0006-one-map-for-both-builds.md`: the ***contract*** ***between*** the ***map*** and a ***service***, and why the ***map*** ***now*** ***lives*** here
- `docs/changes/`: what each ***day***'s ***work*** ***changed*** and why, ***newest*** ***last***

The ***phase***-0 ***build*** and its ***documents*** ***stay*** in the ***archived*** ***phase***-0 repository, whose ***last*** ***commit*** is `ecb21fb`.

## Comments in the code

Every ***comment*** is ***written*** for a ***school*** ***student*** of ***computer*** ***science***. It ***explains*** each ***technical*** ***word*** the ***first*** ***time*** a ***file*** ***uses*** it, and it ***says*** why the ***code*** is as it is: the ***reason***, the ***rule***, the ***limit*** and the ***story*** ***behind*** it, such as `US-24 c6`.
