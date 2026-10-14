# Tessera

> **The live build, since 14 October 2026.** The ***maintainer*** ***chose*** this .NET ***build*** as the ***one*** to ***carry*** on; the Java ***build***, `nathanwh71/JTessera`, is ***archived*** and ***kept*** to ***read***. The ***map*** in `web/` ***now*** ***lives*** here (ADR 0006, as ***amended***).

***Tessera*** is a ***small*** ***web*** ***service*** that ***serves*** a ***map*** of ***places*** as ***tiles***. It ***reads*** ***tables*** from a ***database***, ***cuts*** them into ***map*** ***tiles*** on ***request***, and ***answers*** every ***machine*** in the ***office*** on ***port*** 80, ***through*** Nginx. This ***build***, ***Phase*** Oct, ***serves*** the 900 ***places*** of Macau in ***nine*** ***layers***, and ***shows*** them on a ***map*** at `/`.

A **tile** is ***one*** ***square*** of a ***map*** at ***one*** ***zoom*** ***level***. A ***web*** ***map*** is ***made*** of ***thousands*** of them, and the ***browser*** ***asks*** for ***only*** the ***ones*** on the ***screen***. ***Tessera*** ***makes*** ***each*** ***tile*** from the ***database*** the ***first*** ***time*** a ***map*** ***asks*** for it; Nginx ***keeps*** it for a ***minute*** and ***hands*** the ***same*** ***copy*** to everyone who ***asks*** in that ***time***.

## What this build holds, story by story

The twenty-six ***stories*** in `docs/user-stories/user-stories.md` ***fix*** what ***Tessera*** must do. Their ***state*** on 14 ***October*** 2026:

| Story | State |
|---|---|
| US-00 Walking skeleton | ***Built*** and ***tested*** |
| US-01 One database | ***Built*** and ***tested*** |
| US-03 Publish by grant | ***Built*** and ***tested*** |
| US-24 Places in nine layers | ***Built*** and ***tested*** |
| US-07 Serve a tile | ***Built*** and ***tested*** |
| S-01 SQL injection | ***Built*** and ***tested*** |
| S-06 Malformed URLs | ***Built*** and ***tested***: Nginx ***refuses*** an ***address*** it ***cannot*** ***read***, and a disguised ***one*** ***matches*** no ***route***; ***each*** ***answer*** is a ***problem*** ***document*** |
| US-35 Office on port 80 | ***Script*** ***written***: Nginx ***answers*** on ***port*** 80. It ***needs*** a ***first*** ***run*** on the ***office*** ***machine*** |
| US-23 and UI-09 to UI-17, the map | ***Built*** and ***tested***: the ***map*** ***copied*** from the Java ***build*** (ADR 0006), and its ***browser*** ***tests*** ***pass*** ***through*** Nginx |
| N-18 to N-20, tiles that cost less | ***Built*** and ***tested***: a ***tile*** ***carries*** `max-age` and a ***weak*** `ETag`, and Nginx ***keeps*** it, ***answers*** 304, and ***packs*** it with gzip (ADR 0008) |
| N-21 to N-28, the Java way | ***Built*** and ***tested***, as ***amended*** on 14 ***October*** 2026: 503 with `Retry-After`, ***grants*** ***seen*** ***within*** `Catalogue.RefreshSeconds`, ***one*** ***query*** for a ***crowd***, `/health`, and ***queries*** ***served*** in the ***order*** they ***arrive***. ***While*** the ***database*** is ***down***, Nginx ***serves*** every ***tile*** it ***holds***. The ***service*** and the ***database*** ***start*** in ***any*** ***order*** |

## What you need

- The .NET 10 SDK, 10.0.100 or ***later***: the ***tools*** that ***build*** and ***run*** C# 14
- Docker Engine, which ***runs*** the ***database*** and Nginx in ***containers***: ***sealed*** ***boxes*** with their ***own*** ***files***
- Node.js 26, which ***runs*** the ***scripts*** and ***builds*** the ***map*** (ADR 0002)
- GDAL's `ogr2ogr`, for `npm run convert` ***alone***; ***without*** it, the converter ***runs*** GDAL's Docker ***image***

## The commands

| Command | What it does |
|---|---|
| `npm run build` | ***Builds*** the ***map***, then the ***service*** with the ***map*** ***inside*** it (US-00 c1) |
| `npm test` | ***Runs*** every ***test***: the ***scripts***', the ***map***'s, the ***service***'s ***two*** ***suites***, and the ***browser*** ***tests*** ***through*** Nginx (US-00 c3) |
| `dotnet build` | ***Builds*** the ***service*** and both ***test*** ***suites*** ***alone***; ***needs*** `web/dist` from a ***map*** ***build*** |
| `dotnet run --project tests/Tessera.UnitTests` | ***Runs*** the ***unit*** and ***property*** ***tests***: no ***database***, a ***second*** or ***two*** |
| `dotnet run --project tests/Tessera.IntegrationTests` | ***Starts*** the ***packaged*** ***database*** in Docker and ***tests*** ***through*** it and ***through*** ***real*** HTTP (US-00 c3) |
| `npm install` | ***Installs*** the ***scripts***' ***tools***: zx and TypeScript |
| `npm start` | ***Starts*** the ***database***, ***runs*** ***any*** ***new*** ***seed*** ***script***, ***starts*** the ***service*** and Nginx in ***front*** of it, then ***waits*** for `/health` to ***say*** `UP` (US-00 c2, N-25, ADR 0009) |
| `npm run convert -- SOURCE OUTPUT schema.table[:WHERE] ...` | ***Turns*** a ***file*** of ***places*** into a ***seed*** ***script*** in `db/seed`, with ***one*** ***table*** for ***each*** `schema.table` (US-24, ADR 0009) |
| `npm run publish` | ***Grants*** the ***nine*** ***layers*** to the ***service***, which ***serves*** them ***within*** `Catalogue.RefreshSeconds`, 30 by ***default*** (US-24 c5, N-23) |
| `npm run office-port` | ***Prints*** the ***steps*** that ***open*** ***port*** 80 to the ***office*** (US-35) |
| `npm run e2e` | ***Runs*** the ***map***'s ***browser*** ***tests***, then the Nginx ***checks***, ***against*** a ***database*** and an Nginx of their ***own*** |

***Copy*** `.env.example` to `.env` and ***give*** ***each*** ***password*** a ***value*** ***before*** `npm start`. Git ***ignores*** `.env`, so no ***password*** ***reaches*** the repository.

## Where the settings live

***Machines***, ***ports*** and ***passwords*** ***come*** from ***environment*** variables: ***values*** the ***operating*** ***system*** ***hands*** a ***program*** when it ***starts***. The ***service*** ***refuses*** to ***start*** ***without*** its ***database***'s ***address*** and ***names*** every ***problem*** it ***finds*** (US-00 c4); it ***starts*** ***whether*** or not the ***database*** ***answers*** yet, and ***answers*** 503 until it does (N-23). Every ***other*** ***setting*** ***lives*** in `tessera.json`, in ***sections***, with ***each*** ***time***'s ***unit*** in its ***name***: the ***area***'s ***name***, ***box*** and ***home*** ***view***, the ***map*** ***preview***, ***each*** ***tile***'s ***limits*** and `Tiles.CacheSeconds`, the ***database*** ***pool***'s ***limits***, and how ***often*** the ***list*** of ***layers*** is ***reread***. ***Any*** ***environment*** variable ***overrides*** the ***key*** it ***names***, with `__` for ***each*** ***step*** ***down***, such as `Tiles__CacheSeconds=0` (ADR 0003).

| Variable | Read by | Holds |
|---|---|---|
| `TESSERA_HTTPPORT` | `npm start`, for Nginx | The ***port*** the ***office*** ***reaches***: 80 in the ***office*** |
| `TESSERA_SERVICE_PORT` | `npm start` | ***Optional***: the ***port*** the ***service*** ***answers*** Nginx on, 7800 by ***default*** |
| `DATABASE_URL` | The ***service*** | `postgresql://tessera_reader:<password>@<host>:<port>/tessera` |
| `TESSERA_PUBLISHER_DATABASE_URL` | `seed` | `postgresql://tessera_publisher:<password>@<host>:<port>/tessera` |
| `TESSERA_DB_PASSWORD`, `TESSERA_READER_PASSWORD`, `TESSERA_PUBLISHER_PASSWORD` | `npm start`, which ***writes*** the ***two*** URLs ***above*** from them | The ***three*** ***roles***' ***passwords*** |
| `TESSERA_DB_HOST`, `TESSERA_DB_PORT`, `TESSERA_DB_NAME` | `npm start`, `npm run publish` | ***Optional***: where the ***database*** ***answers***, 127.0.0.1, 5432 and `tessera` by ***default*** |

## Adding places

The ***places*** ***reach*** the ***database*** as SQL ***scripts*** in `db/seed`, which DbUp ***runs*** ***once*** ***each***, in the ***order*** of their ***names*** (ADR 0009). To ***add*** a ***file*** of ***places***:

1. ***Convert*** it: `npm run convert -- data/parks.shp db/seed/0002-parks.sql outdoors.parks`. GDAL ***reads*** GeoJSON, shapefiles, GeoPackages, CSV and more, and ***stores*** every ***shape*** in longitude and latitude.
2. ***Read*** the ***script***, then ***commit*** it. ***Never*** ***change*** a ***script*** ***once*** it has ***run***: DbUp would not ***run*** it ***again***. ***Put*** ***new*** or ***corrected*** ***data*** in a ***new*** ***script***, with the ***next*** ***number***.
3. ***Run*** `npm start`, or `Tessera seed` as `tessera_publisher`, then `npm run publish`. A ***script*** that ***replaces*** a ***table*** ***takes*** its ***grant*** with it, so the ***table*** ***waits*** for the ***publish***.

A filter ***after*** a colon ***splits*** ***one*** ***file*** into ***several*** ***tables***. The Macau ***seed*** ***came*** from ***one*** ***command***:

```
npm run convert -- data/macau-pois.geojson db/seed/0001-macau-places.sql \
  "food.cafes:layer = 'cafes'" "food.restaurants:layer = 'restaurants'" "food.markets:layer = 'markets'" \
  "outdoors.parks:layer = 'parks'" "outdoors.playgrounds:layer = 'playgrounds'" \
  "culture.museums:layer = 'museums'" "culture.libraries:layer = 'libraries'" \
  "transport.trains:layer = 'trains'" "transport.buses:layer = 'buses'"
```

## How the database keeps the service safe

***Tessera*** ***follows*** pg_tileserv's ***rule***: the ***database*** ***decides*** what the ***map*** ***shows***. The ***service*** ***signs*** in as `tessera_reader`, a ***role*** that may ***only*** ***read***, and a ***table*** ***becomes*** a ***layer*** ***only*** when that ***role*** is ***granted*** it. The ***import*** ***signs*** in as `tessera_publisher`, which ***owns*** the ***tables*** but ***never*** ***grants*** them on its ***own***; ***publishing*** is the ***publisher***'s ***own*** ***act***.

***Two*** ***attacks*** are ***closed*** by ***design***. Every ***value*** from a ***request*** ***travels*** ***beside*** the ***query*** as a ***parameter***, ***never*** ***inside*** its ***text***, and every ***name*** ***enters*** the ***text*** ***quoted***, ***through*** ***one*** ***function***, `Queries.Quote`; so no ***request*** can smuggle in a ***command*** (SQL injection, S-01). Nginx ***refuses*** an ***address*** it ***cannot*** ***read***, and the ***service*** ***matches*** ***routes*** on the ***address*** as ***sent***, so a disguised ***address*** ***reaches*** no ***route*** (a ***malformed*** URL, S-06).

## What is where

The ***service*** is ***one*** ***program*** in ***one*** ***project***, with a ***folder*** for ***each*** ***module*** (ADR 0001). It ***borrows*** what ***libraries*** do ***well***: ASP.NET Core ***reads*** its ***settings***, ***writes*** its ***problem*** ***documents*** and ***answers*** `/health`; Dapper and Npgsql ***talk*** to the ***database***; CSharpFunctionalExtensions ***gives*** `Result` (ADR 0004); DbUp ***runs*** the ***seed*** ***scripts*** (ADR 0009); Nginx ***keeps***, ***packs*** and ***guards*** (ADR 0008).

| Folder | What it holds |
|---|---|
| `src/Tessera/Settings` | Every ***setting***, its ***default*** and its ***limits*** |
| `src/Tessera/Sql` | Every SQL ***text***, in `Queries.cs`, and the ***only*** ***code*** that ***talks*** to the ***database*** |
| `src/Tessera/Catalogue` | The ***list*** of ***layers***: every ***table*** ***granted*** to the ***service***, ***reread*** every `Catalogue.RefreshSeconds` |
| `src/Tessera/Seeding` | The `seed` ***command***: DbUp ***runs*** ***each*** ***new*** ***script*** in `db/seed` ***once*** |
| `src/Tessera/Tiles` | The ***tile*** ***route*** and its ***answers*** |
| `src/Tessera/Map` | ***Serving*** the ***map***'s ***page***, with the ***area*** ***written*** in, and its ***files*** |
| `src/Tessera` | `Program.cs`, the ***two*** ***commands***; `Service.cs`, the ***service*** ***put*** ***together*** and `/health`; `Problems.cs` |
| `nginx/` | Nginx's ***file***, which `npm start` and the ***browser*** ***tests*** ***run*** (ADR 0008) |
| `web/` | The ***map***: TypeScript, React, MapLibre GL JS, react-elmish and Tailwind CSS, ***copied*** from the Java ***build*** (ADR 0006) |
| `tests/` | ***Unit***, ***property*** and ***integration*** ***tests*** |
| `db/` | The ***packaged*** ***database***: PostgreSQL 18 with PostGIS 3.6, the ***role*** ***script***, and the ***seed*** ***scripts*** in `db/seed` |
| `data/` | The ***seed***'s ***source***: 900 ***places*** in longitude and latitude, as `nathanwh71/JTessera` ***holds*** them |
| `scripts/` | The ***scripts***, in TypeScript with zx (ADR 0002), and the Nginx ***checks*** in `scripts/lib/edge.ts` |
| `docs/` | The ***stories*** and the ***architecture*** ***decision*** ***records*** |

## The documents

- `docs/user-stories/user-stories.md`: the twenty-six ***stories*** of ***Phase*** Oct, in ***build*** ***order***
- `docs/adr/0001-modular-monolith.md`: why the ***service*** is ***one*** ***program*** with ***modules***
- `docs/adr/0002-scripts-in-typescript-with-zx.md`: why the ***scripts*** are TypeScript on Node.js 26
- `docs/adr/0003-settings-file.md`: why ***settings*** ***live*** in `tessera.json` and the ***environment***, and every ***key***
- `docs/adr/0004-functional-style.md`: why the ***code*** is ***functional***, with `Result` and ***nullable*** ***types***
- `docs/adr/0006-one-map-for-both-builds.md`: the ***contract*** ***between*** the ***map*** and a ***service***, and why the ***map*** ***now*** ***lives*** here
- `docs/adr/0007-resilience-library-with-the-first-limit.md`: why no ***resilience*** ***library*** yet, and when ***one*** ***arrives***
- `docs/adr/0008-nginx-in-front.md`: why Nginx ***stands*** in ***front***, and what it does in ***place*** of the ***service***
- `docs/adr/0009-seed-with-dbup.md`: why the ***places*** ***arrive*** as SQL ***scripts*** that DbUp ***runs*** ***once***
- `docs/changes/`: what ***each*** ***day***'s ***work*** ***changed*** and why, ***newest*** ***last***

The phase-0 ***build*** and its ***documents*** ***stay*** in the ***archived*** phase-0 repository, whose ***last*** ***commit*** is `ecb21fb`.

## Comments in the code

Every ***comment*** is ***written*** for a ***school*** ***student*** of ***computer*** ***science***. It ***says*** why the ***code*** is as it is: the ***reason***, the ***rule***, the ***limit*** and the ***story*** ***behind*** it, such as `US-24 c6`. A ***type*** or ***method*** ***carries*** ***one*** ***short*** ***comment***; the ***long*** ***story*** ***lives*** in `docs/`.
