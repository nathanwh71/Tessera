# Tessera

***Tessera*** is a ***small*** ***web*** ***service*** that ***serves*** a ***map*** of ***places*** as ***tiles***. It ***reads*** ***tables*** from a ***database***, ***cuts*** them into ***map*** ***tiles*** on ***request***, and ***answers*** every ***machine*** in the ***office*** on ***port*** 80, ***through*** Nginx. This ***build***, ***Phase*** Oct, ***serves*** the 900 ***places*** of Macau in ***nine*** ***layers***, and ***shows*** them on a ***map*** at `/`.

A **tile** is ***one*** ***square*** of a ***map*** at ***one*** ***zoom*** ***level***. A ***web*** ***map*** is ***made*** of ***thousands*** of them, and the ***browser*** ***asks*** for ***only*** the ***ones*** on the ***screen***. ***Tessera*** ***makes*** ***each*** ***tile*** from the ***database*** the ***first*** ***time*** a ***map*** ***asks*** for it; Nginx ***keeps*** it for a ***minute*** and ***hands*** the ***same*** ***copy*** to everyone who ***asks*** in that ***time***.

## What this build holds, story by story

The thirty ***stories*** in `docs/user-stories/user-stories.md` ***fix*** what ***Tessera*** must do. ***Each*** ***stands*** as ***follows***:

| Story | State |
|---|---|
| US-00 Walking skeleton | ***Built*** and ***tested*** |
| US-01 One database | ***Built*** and ***tested*** |
| US-03 Publish by grant | ***Built*** and ***tested*** |
| US-24 Places in nine layers | ***Built*** and ***tested*** |
| US-07 Serve a tile | ***Built*** and ***tested*** |
| S-01 SQL injection | ***Built*** and ***tested*** |
| S-06 Malformed URLs | ***Built*** and ***tested***: Nginx ***refuses*** an ***address*** it ***cannot*** ***read***, and a disguised ***one*** ***matches*** no ***route***; ***each*** ***answer*** is a ***problem*** ***document*** |
| US-35 Office on port 80 | ***Script*** ***written*** and ***tested***: it ***checks*** for WSL2's ***mirrored*** ***networking*** and ***prints*** the firewall ***steps***. It ***needs*** a ***first*** ***run*** on the ***office*** ***machine*** |
| US-23 and UI-09 to UI-19, the map | ***Built*** and ***tested***: the ***map*** in `web/` (ADR 0006), and its ***browser*** ***tests*** ***pass*** ***through*** Nginx |
| N-18 to N-20, tiles that cost less | ***Built*** and ***tested***: a ***tile*** ***carries*** `max-age` and a ***weak*** `ETag`, and Nginx ***keeps*** it, ***answers*** 304, and ***packs*** it with gzip (ADR 0008) |
| N-21 to N-28, steady when the database is not | ***Built*** and ***tested***: 503 with `Retry-After`, ***grants*** ***seen*** ***within*** `Catalogue.RefreshSeconds`, ***one*** ***query*** for a ***crowd***, `/health`, and ***queries*** ***served*** in the ***order*** they ***arrive***. ***While*** the ***database*** is ***down***, Nginx ***serves*** every ***tile*** it ***holds***. The ***service*** and the ***database*** ***start*** in ***any*** ***order*** |
| US-36 Clean places | ***Built*** and ***tested***: ***eight*** filters, ***stop*** ***rules*** and a ***log*** for ***each*** ***run*** of `npm run etl` (ADR 0009) |
| S-07, S-30 and N-29, Nginx guards the door | ***Built*** and ***tested***: Nginx ***answers*** the ***office***'s ***network*** ***alone***, ***caps*** ***each*** ***machine*** at 100 ***tiles*** a ***second*** with a burst of 300 (ADR 0007), and ***shares*** ***requests*** ***between*** ***two*** ***copies*** of the ***service***, either of which can ***carry*** them all (ADR 0008) |

## What you need

- The .NET 10 SDK, 10.0.100 or ***later***: the ***tools*** that ***build*** and ***run*** C# 14
- Docker Engine, which ***runs*** the ***database*** and Nginx in ***containers***: ***sealed*** ***boxes*** with their ***own*** ***files***
- Node.js 26, which ***runs*** the ***scripts*** and ***builds*** the ***map*** (ADR 0002)
- GDAL's `ogr2ogr` and `ogrinfo`, for `npm run etl`, `npm run inspect` and the ***pipeline***'s ***tests*** ***alone***; ***without*** them, the ***pipeline*** ***runs*** GDAL's Docker ***image***

## The commands

| Command | What it does |
|---|---|
| `npm run build` | ***Builds*** the ***map***, then the ***service*** with the ***map*** ***inside*** it (US-00 c1) |
| `npm test` | ***Runs*** every ***test***: the ***scripts***', the ***map***'s, the ***service***'s ***two*** ***suites***, the ***pipeline***'s, and the ***browser*** ***tests*** ***through*** Nginx (US-00 c3) |
| `dotnet build` | ***Builds*** the ***service***, the ***pipeline*** and the ***three*** ***test*** ***suites*** ***alone***; ***needs*** `web/dist` from a ***map*** ***build*** |
| `dotnet run --project tests/Tessera.UnitTests` | ***Runs*** the ***unit*** and ***property*** ***tests***: no ***database***, a ***second*** or ***two*** |
| `dotnet run --project tests/Tessera.IntegrationTests` | ***Starts*** the ***packaged*** ***database*** in Docker and ***tests*** ***through*** it and ***through*** ***real*** HTTP (US-00 c3) |
| `npm install` | ***Installs*** the ***scripts***' ***tools***: zx and TypeScript |
| `npm start` | ***Starts*** the ***database***, ***runs*** ***any*** ***new*** ***seed*** ***script***, ***starts*** ***two*** ***copies*** of the ***service*** and Nginx in ***front*** of them, then ***waits*** for `/health` to ***say*** `UP`; it ***starts*** ***again*** ***any*** ***copy*** that ***stops***, until Ctrl+C (US-00 c2, N-25, N-29, ADR 0009) |
| `npm run inspect -- SOURCE [--split FIELD]` | ***Describes*** a ***source***'s ***layers***, ***rows***, ***shapes***, ***systems*** and ***fields***, and ***prints*** a ***starting*** ***pipeline*** ***file***; `--split` ***makes*** ***one*** ***table*** for ***each*** ***value*** of a ***field*** (ADR 0009) |
| `npm run etl -- PIPELINE.json` | ***Cleans*** a ***file*** of ***places*** into the ***next*** ***seed*** ***script*** in `db/seed`, and logs ***each*** ***row***'s fate in `db/seed-logs` (US-24, ADR 0009) |
| `dotnet run --project tests/Tessera.Etl.Tests` | ***Runs*** the ***pipeline***'s ***tests***, ***one*** of them on a geodatabase GDAL ***builds*** |
| `npm run publish` | ***Grants*** the ***nine*** ***layers*** to the ***service***, which ***serves*** them ***within*** `Catalogue.RefreshSeconds`, 30 by ***default*** (US-24 c5, N-23) |
| `npm run office-port` | ***Checks*** that WSL2 ***uses*** ***mirrored*** ***networking***, and ***prints*** the ***steps*** that ***open*** ***port*** 80 to the ***office*** (US-35) |
| `npm run e2e` | ***Runs*** the ***map***'s ***browser*** ***tests***, then the Nginx ***checks***, ***against*** a ***database*** and an Nginx of their ***own*** |

***Copy*** `.env.example` to `.env` and ***give*** ***each*** ***password*** a ***value*** ***before*** `npm start`. Git ***ignores*** `.env`, so no ***password*** ***reaches*** the repository.

## Where the settings live

***Machines***, ***ports*** and ***passwords*** ***come*** from ***environment*** variables: ***values*** the ***operating*** ***system*** ***hands*** a ***program*** when it ***starts***. The ***service*** ***refuses*** to ***start*** ***without*** its ***database***'s ***address*** and ***names*** every ***problem*** it ***finds*** (US-00 c4); it ***starts*** ***whether*** or not the ***database*** ***answers*** yet, and ***answers*** 503 until it does (N-23). Every ***other*** ***setting*** ***lives*** in `tessera.json`, in ***sections***, with ***each*** ***time***'s ***unit*** in its ***name***: the ***area***'s ***name***, ***box*** and ***home*** ***view***, the ***map*** ***preview***, ***each*** ***tile***'s ***limits*** and `Tiles.CacheSeconds`, the ***database*** ***pool***'s ***limits***, and how ***often*** the ***list*** of ***layers*** is ***reread***. ***Any*** ***environment*** variable ***overrides*** the ***key*** it ***names***, with `__` for ***each*** ***step*** ***down***, such as `Tiles__CacheSeconds=0` (ADR 0003).

| Variable | Read by | Holds |
|---|---|---|
| `TESSERA_HTTPPORT` | `npm start`, for Nginx | The ***port*** the ***office*** ***reaches***: 80 in the ***office*** |
| `TESSERA_OFFICE_NETWORK` | `npm start`, for Nginx | The ***office***'s ***network***, such as `192.168.10.0/24`; Nginx ***answers*** it and this ***machine*** ***alone*** (S-07) |
| `TESSERA_SERVICE_PORT` | `npm start` | ***Optional***: the ***first*** ***copy***'s ***port***, 7800 by ***default***; the ***second*** ***copy*** ***takes*** the ***next*** ***port*** (N-29) |
| `DATABASE_URL` | The ***service*** | `postgresql://tessera_reader:<password>@<host>:<port>/tessera` |
| `TESSERA_PUBLISHER_DATABASE_URL` | `seed` | `postgresql://tessera_publisher:<password>@<host>:<port>/tessera` |
| `TESSERA_DB_PASSWORD`, `TESSERA_READER_PASSWORD`, `TESSERA_PUBLISHER_PASSWORD` | `npm start`, which ***writes*** the ***two*** URLs ***above*** from them | The ***three*** ***roles***' ***passwords*** |
| `TESSERA_DB_HOST`, `TESSERA_DB_PORT`, `TESSERA_DB_NAME` | `npm start`; `npm run publish` ***reads*** `TESSERA_DB_NAME` ***alone*** | ***Optional***: where the ***database*** ***answers***, 127.0.0.1, 5432 and `tessera` by ***default*** |

## Adding places

The ***places*** ***reach*** the ***database*** as SQL ***scripts*** in `db/seed`, which DbUp ***runs*** ***once*** ***each***, in the ***order*** of their ***names*** (ADR 0009). A **pipeline file** ***turns*** a ***file*** of ***places*** into the ***next*** ***script***, ***cleaning*** it on the ***way***: GDAL extracts ***each*** ***table***, filters ***clean*** its ***rows***, and GDAL ***writes*** the SQL. To ***add*** ***places***:

1. Inspect the ***data***, such as `npm run inspect -- data/parks.gdb`, and ***save*** the ***starting*** ***pipeline*** ***file*** it ***prints*** ***beside*** the ***data***, such as `data/parks.pipeline.json`. Then ***set*** ***each*** ***table***'s schema and ***name***, and its filters. It ***names*** the ***source***, the ***script*** to ***write***, and ***each*** ***table*** with its filters. GDAL ***reads*** ***file*** geodatabases, GeoJSON, shapefiles, GeoPackages, CSV and more, and ***stores*** every ***shape*** in longitude and latitude.
2. ***Run*** it: `npm run etl -- data/parks.pipeline.json`. It ***writes*** the ***script***, such as `db/seed/0002-parks.sql`, and a ***log*** of what ***each*** filter did to ***each*** ***row***, in `db/seed-logs/`.
3. ***Read*** the ***log*** and the ***script***, then ***commit*** both. ***Never*** ***change*** a ***script*** ***once*** it has ***run***: DbUp would not ***run*** it ***again***. ***Put*** ***new*** or ***corrected*** ***data*** in a ***new*** ***script***, with the ***next*** ***number***.
4. ***Run*** `npm start`, or `Tessera seed` as `tessera_publisher`, then `npm run publish`. A ***script*** that ***replaces*** a ***table*** ***takes*** its ***grant*** with it, so the ***table*** ***waits*** for the ***publish***.

A ***table*** of a geodatabase ***names*** its ***feature*** ***class*** as `layer`; a ***table*** ***cut*** from ***one*** ***file*** ***names*** its ***rows*** with `where`, as `data/macau.pipeline.json` does for the ***nine*** Macau ***layers***. A ***pipeline*** ***file*** for a geodatabase, `data/parks.gdb`, ***looks*** ***like*** this:

```json
{
  "source": "data/parks.gdb",
  "output": "db/seed/0002-parks.sql",
  "stopWhen": { "droppedOverPercent": 5 },
  "tables": [
    { "table": "food.cafes", "layer": "Cafes", "filters": [
      { "filter": "dropEmptyShapes", "onFail": "stop" },
      { "filter": "swapCoordinates" },
      { "filter": "insideArea", "onFail": "stop" },
      { "filter": "tidyText" },
      { "filter": "requireFields", "fields": ["id", "name"] },
      { "filter": "dropDuplicateIds", "field": "id", "onFail": "stop" },
      { "filter": "dropNearDuplicates", "field": "name", "metres": 25 } ] },
    { "table": "outdoors.parks", "layer": "Parks", "lines": true, "filters": [ { "filter": "repairShapes" } ] }
  ]
}
```

| Filter | What it does |
|---|---|
| `dropEmptyShapes` | A ***row*** with no ***shape***, or an ***empty*** ***one***, ***fails*** |
| `repairShapes` | An invalid ***shape***, such as an ***area*** whose ***edge*** ***crosses*** ***itself***, is ***repaired***; ***one*** that ***cannot*** be ***repaired*** ***fails*** |
| `swapCoordinates` | A ***shape*** ***outside*** the ***area*** that ***falls*** ***inside*** it with longitude and latitude ***swapped*** is ***swapped*** ***back*** |
| `insideArea` | A ***shape*** that does not ***touch*** the ***area***'s ***box*** ***fails*** |
| `tidyText` | ***Text*** is ***put*** into ***one*** Unicode ***form***, ***runs*** of ***spaces*** ***become*** ***one***, ***ends*** are trimmed, and ***empty*** ***text*** ***becomes*** ***null***; `fields` ***limits*** it to ***some*** ***columns*** |
| `requireFields` | A ***row*** ***missing*** ***any*** of `fields`, or ***holding*** ***only*** ***spaces*** there, ***fails*** |
| `dropDuplicateIds` | A ***row*** that ***repeats*** an earlier ***row***'s `field` ***fails*** |
| `dropNearDuplicates` | A ***row*** whose `field` ***matches*** an earlier ***row***'s, ***within*** `metres` of it, ***fails*** |

Filters ***run*** in the ***order*** ***listed***, ***each*** on the ***rows*** the ***one*** ***before*** ***kept***. A ***row*** that ***fails*** is ***dropped***, or, with `"onFail": "stop"`, ***ends*** the ***whole*** ***run***. A ***table*** ***also*** ***stops*** the ***run*** when more than `droppedOverPercent` of its ***rows*** were ***dropped***, 10 by ***default***, or ***fewer*** than `rowsUnder` ***remain***, 1 by ***default***; `stopWhen` ***sets*** them for the ***whole*** ***file*** or for ***one*** ***table***. A ***stopped*** ***run*** ***writes*** no ***script***, but ***always*** ***writes*** its ***log***. The ***area*** ***comes*** from `tessera.json`, or from the ***pipeline***'s ***own*** `area`. `lines: true` ***stores*** ***lines*** and ***areas*** as many-part ***shapes***, so ***one*** ***table*** ***holds*** both ***kinds***.

**Adding a filter.** ***Each*** filter is ***one*** ***file*** in `tools/Tessera.Etl/Filters/`. It ***holds*** the filter's ***name***, what it does, a ***record*** of its ***own*** ***settings***, its ***checks***, and a `Create` ***function*** that ***returns*** the filter: a ***function*** from a ***table***'s ***rows*** to the ***rows*** it ***keeps*** and a ***log*** of what it did. A ***new*** filter is ***one*** ***new*** ***file***, ***modelled*** on `InsideArea.cs`, and ***one*** ***line*** in `Filters/All.cs`. ***Each*** filter ***reads*** ***only*** its ***own*** ***settings***, so the ***pipeline*** ***file*** ***refuses*** a ***setting*** that ***belongs*** to another filter, or ***one*** that is ***missing***.

**Filters in code.** Filters ***join*** with `>>`, which ***runs*** the ***left*** ***one***, then the ***right*** ***one*** on the ***rows*** it ***kept***, and ***stops*** at the ***first*** ***stop***:

```csharp
var cleanCafes =
    DropEmptyShapes.Create(OnFail.Stop)
    >> SwapCoordinates.Create(macau)
    >> InsideArea.Create(macau, OnFail.Stop)
    >> TidyText.Create()
    >> RequireFields.Create(["id", "name"]);
```

The ***pipeline*** ***file*** ***builds*** the ***same*** ***chain*** from its ***list***. A filter may ***also*** ***add*** or ***remove*** a ***column***: the ***script*** ***takes*** its ***columns*** from the ***cleaned*** ***rows***.

## How the database keeps the service safe

***Tessera*** ***follows*** pg_tileserv's ***rule***: the ***database*** ***decides*** what the ***map*** ***shows***. The ***service*** ***signs*** in as `tessera_reader`, a ***role*** that may ***only*** ***read***, and a ***table*** ***becomes*** a ***layer*** ***only*** when that ***role*** is ***granted*** it. The ***seed*** ***signs*** in as `tessera_publisher`, which ***owns*** the ***tables*** but ***never*** ***grants*** them on its ***own***; ***publishing*** is the ***publisher***'s ***own*** ***act***.

***Two*** ***attacks*** are ***closed*** by ***design***. Every ***value*** from a ***request*** ***travels*** ***beside*** the ***query*** as a ***parameter***, ***never*** ***inside*** its ***text***, and every ***name*** ***enters*** the ***text*** ***quoted***, ***through*** ***one*** ***function***, `Queries.Quote`; so no ***request*** can smuggle in a ***command*** (SQL injection, S-01). Nginx ***refuses*** an ***address*** it ***cannot*** ***read***, and the ***service*** ***matches*** ***routes*** on the ***address*** as ***sent***, so a disguised ***address*** ***reaches*** no ***route*** (a ***malformed*** URL, S-06).

## What is where

The ***service*** is ***one*** ***program***, with ***each*** ***module*** a ***project*** of its ***own*** in a ***folder*** of `src/Tessera`; the ***compiler*** ***keeps*** ***each*** ***module*** to the ***neighbours*** its ***project*** ***file*** ***names*** (ADR 0001). It ***borrows*** what ***libraries*** do ***well***: ASP.NET Core ***reads*** its ***settings***, ***writes*** its ***problem*** ***documents*** and ***answers*** `/health`; Dapper and Npgsql ***talk*** to the ***database***; CSharpFunctionalExtensions ***gives*** `Result` (ADR 0004); DbUp ***runs*** the ***seed*** ***scripts*** (ADR 0009); Nginx ***keeps***, ***packs*** and ***guards*** (ADR 0008).

| Folder | What it holds |
|---|---|
| `src/Tessera/Settings` | Every ***setting***, its ***default*** and its ***limits*** |
| `src/Tessera/Sql` | Every SQL ***text***, in `Queries.cs`, and the ***code*** ***through*** which the ***service*** ***talks*** to the ***database*** |
| `src/Tessera/Catalogue` | The ***list*** of ***layers***: every ***table*** ***granted*** to the ***service***, ***reread*** every `Catalogue.RefreshSeconds` |
| `src/Tessera/Seeding` | The `seed` ***command***: DbUp ***runs*** ***each*** ***new*** ***script*** in `db/seed` ***once*** |
| `src/Tessera/Tiles` | The ***tile*** ***route*** and its ***answers*** |
| `src/Tessera/Map` | ***Serving*** the ***map***'s ***page***, with the ***area*** ***written*** in, and its ***files*** |
| `src/Tessera/Problems` | The ***shape*** of every ***problem*** ***document*** |
| `tools/Tessera.Etl` | The ***pipeline*** that ***cleans*** a ***file*** of ***places*** into a ***seed*** ***script***; the ***service*** ***never*** ***runs*** it (ADR 0009) |
| `src/Tessera` | `Program.cs`, the ***two*** ***commands***; `Service.cs`, the ***service*** ***put*** ***together*** and `/health`; `Directory.Build.props`, the ***rules*** every ***module*** ***project*** ***shares*** |
| `nginx/` | Nginx's ***file***, which `npm start` and the ***browser*** ***tests*** ***run*** (ADR 0008) |
| `web/` | The ***map***: TypeScript, React, MapLibre GL JS, react-elmish, Valibot and Tailwind CSS (ADR 0006) |
| `tests/` | ***Unit***, ***property*** and ***integration*** ***tests*** of the ***service***, and the ***pipeline***'s ***tests*** |
| `db/` | The ***packaged*** ***database***: PostgreSQL 18 with PostGIS 3.6, the ***role*** ***script***, the ***seed*** ***scripts*** in `db/seed`, and, ***beside*** ***each*** ***script*** `npm run etl` ***writes***, its ***log*** in `db/seed-logs` |
| `data/` | The ***seed***'s ***source***: 900 ***places*** in longitude and latitude, and `macau.pipeline.json`, which ***cleans*** them into a ***seed*** ***script*** |
| `scripts/` | The ***scripts***, in TypeScript with zx (ADR 0002): `start.ts`, `publish.ts`, `inspect.ts`, `office-port.ts`, the ***test*** and ***build*** ***runners***, and the Nginx ***checks*** in `scripts/lib/edge.ts` |
| `docs/` | The ***stories*** and the ***architecture*** ***decision*** ***records*** |

## The documents

- `docs/user-stories/user-stories.md`: the thirty ***stories*** of ***Phase*** Oct, in ***build*** ***order***
- `docs/adr/0001-modular-monolith.md`: why the ***service*** is ***one*** ***program*** with ***modules***
- `docs/adr/0002-scripts-in-typescript-with-zx.md`: why the ***scripts*** are TypeScript on Node.js 26
- `docs/adr/0003-settings-file.md`: why ***settings*** ***live*** in `tessera.json` and the ***environment***, and every ***key***
- `docs/adr/0004-functional-style.md`: why the ***code*** is ***functional***, with `Result` and ***nullable*** ***types***
- `docs/adr/0005-cluster-in-the-browser.md`: why the ***map*** clusters the ***places*** ***itself***, from the ***tiles*** it has ***loaded***
- `docs/adr/0006-the-map-and-the-service.md`: why the ***map*** ***lives*** in `web/`, and the ***contract*** ***between*** the ***map*** and the ***service***
- `docs/adr/0007-limits-in-nginx.md`: why Nginx ***answers*** the ***office*** ***alone*** and ***caps*** ***each*** ***machine***, and why the ***service*** ***keeps*** no ***resilience*** ***library***
- `docs/adr/0008-nginx-in-front.md`: why Nginx ***stands*** in ***front***, and what it does in ***place*** of the ***service***
- `docs/adr/0009-seed-with-dbup.md`: why the ***places*** ***arrive*** as SQL ***scripts*** that DbUp ***runs*** ***once***
- `docs/guides/reading-the-code.md`: ***fifteen*** ***steps*** ***through*** the backend and the ***map***, in the ***order*** a ***tile*** ***travels***
- `docs/guides/reading-the-harder-files.md`: the ***one*** ***idea*** ***behind*** ***each*** of `Settings.cs`, `Database.cs` and `Catalogue.cs`, for ***readers*** ***new*** to ASP.NET Core


## Comments in the code

Every ***comment*** is ***written*** for a ***school*** ***student*** of ***computer*** ***science***. It ***says*** why the ***code*** is as it is: the ***reason***, the ***rule***, the ***limit*** and the ***story*** ***behind*** it, such as `US-24 c6`. A ***type*** or ***method*** ***carries*** ***one*** ***short*** ***comment***; the ***long*** ***story*** ***lives*** in `docs/`.
