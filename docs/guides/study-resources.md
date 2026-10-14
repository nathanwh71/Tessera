# Study resources for building Tessera

**The short answer.** To ***build*** the backend, ***learn*** ***four*** ***things***: ASP.NET Core's minimal APIs and ***options***, Npgsql with Dapper, PostGIS's ***tile*** ***functions***, and Nginx's ***cache***. To ***build*** the ***map***, ***learn*** ***four*** more: TypeScript, React with the Elm ***architecture***, MapLibre GL JS, and Vite with its ***test*** ***tools***. ***One*** ***idea*** ***joins*** both ***halves***: a ***pure*** ***core*** ***inside*** a ***thin*** ***shell***, with ***failures*** ***carried*** as ***values***. ***Read*** the essential ***resources*** in the ***order*** of `docs/guides/reading-the-code.md`; ***turn*** to the ***optional*** ***ones*** when a ***step*** ***raises*** a ***question***. 'The story map' at the ***end*** ***names*** the ***resources*** for ***each*** of the 26 ***stories***.

**How to read the tables.**
- **ID** ***names*** ***each*** ***resource***, so 'Where to start' can refer to it.
- **Kind** ***says*** what it is: ***Book***, Docs, Article, Video, Repo or ***Standard***.
- **Steps** ***names*** the ***steps*** of `docs/guides/reading-the-code.md` the ***resource*** ***serves***; 'The story map' ***names*** the ***stories***.
- Every ***link*** was ***checked*** on 14 ***October*** 2026. ***Books*** ***name*** their ***edition***; a ***library*** or the ***publisher***'s ***site*** ***gives*** the ***current*** ***one***.

## 1 · Ideas both halves share

### Essential

| ID | Resource | Kind | Why it helps | Steps |
|---|---|---|---|---|
| F1 | Scott Wlaschin, *Domain ***Modeling*** ***Made*** Functional* (Pragmatic Bookshelf, 2018) | ***Book*** | ***Types*** as the ***model***, and ***failures*** as ***values***: the ***ideas*** ***behind*** `Result` in both ***halves*** (in F#, your ***own*** ***language***) | 5, 7, 11, 14 |
| F2 | Scott Wlaschin, 'Railway Oriented Programming', <https://fsharpforfunandprofit.com/rop/> | Article, Video | The two-track `Result` that `Database.Run` and `areaOf` ***follow*** | 7, 14 |
| F3 | Gary Bernhardt, 'Boundaries' (2012), <https://www.destroyallsoftware.com/talks/boundaries> | Video | The ***pure*** ***core*** ***inside*** a ***thin*** ***shell***: `map.ts` ***against*** `canvas-hooks.ts`, `Queries.cs` ***against*** `Database.cs` | 7, 11, 13 |
| F4 | Vladimir Khorikov, *Unit ***Testing*** Principles, Practices, and Patterns* (Manning, 2020) | ***Book*** | Which ***tests*** to ***write***, and why a ***pure*** ***core*** ***needs*** no mocks; Khorikov ***also*** ***wrote*** CSharpFunctionalExtensions | 15 |

### Optional

| ID | Resource | Kind | Why it helps | Steps |
|---|---|---|---|---|
| F5 | Scott Wlaschin, 'Functional Programming Design Patterns' (NDC London, 2014), <https://fsharpforfunandprofit.com/fppatterns/> | Video | ***Functions*** as ***building*** ***blocks***, `map` and `bind` in an ***hour*** | 11, 14 |
| F6 | Rich Hickey, 'Simple Made Easy' (2011), <https://www.infoq.com/presentations/Simple-Made-Easy/> | Video | Why ***values*** that ***never*** ***change*** ***make*** a ***program*** easier to ***reason*** ***about*** | 6, 11 |
| F7 | Alexis King, 'Parse, don't validate', <https://lexi-lambda.github.io/blog/2019/11/05/parse-don-t-validate/> | Article | Why `TileAddress.Of` and `areaOf` ***return*** a ***value***, not a yes or no | 5, 14 |
| F8 | ***Mark*** Seemann, *Code That ***Fits*** in Your Head* (Addison-Wesley, 2021) | ***Book*** | ***Small***, readable C#, with ***tests*** ***first***; its ***examples*** are an ASP.NET Core ***service*** | 1, 2, 15 |

## 2 · The backend: C#, ASP.NET Core and the database

### Essential

| ID | Resource | Kind | Why it helps | Steps |
|---|---|---|---|---|
| B1 | Andrew ***Lock***, *ASP.NET Core in Action* (Manning, ***third*** ***edition***, 2023), ***code*** at <https://github.com/andrewlock/asp-dot-net-core-in-action-3e> | ***Book***, Repo | Minimal APIs, ***dependency*** injection, configuration and ***options***, from ***first*** principles | 1, 2, 3, 5 |
| B2 | Microsoft, APIs overview (minimal APIs), <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview> | Docs | `MapGet` and `MapMethods`, as `Tiles.cs` and `Map.cs` ***use*** them | 5, 8 |
| B3 | Microsoft, ***Options*** ***pattern*** in ASP.NET Core, <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options> | Docs | `BindConfiguration`, `ValidateDataAnnotations` and `ValidateOnStart`, all of `Settings.cs` | 3 |
| B4 | Microsoft, ***Handle*** ***errors*** in ASP.NET Core, <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling> | Docs | `AddProblemDetails`, `UseExceptionHandler` and `UseStatusCodePages`, ***behind*** `Problems.cs` | 2, 5 |
| B5 | Npgsql, Basic usage, <https://www.npgsql.org/doc/basic-usage.html> | Docs | `NpgsqlDataSource` and its ***pool***, the ***heart*** of `Database.cs` | 7 |
| B6 | DapperLib/Dapper, <https://github.com/DapperLib/Dapper> | Repo | How ***one*** ***line*** ***turns*** ***rows*** into ***records***, and why ***values*** ***travel*** as ***parameters*** | 6, 7 |
| B7 | vkhorikov/CSharpFunctionalExtensions, <https://github.com/vkhorikov/CSharpFunctionalExtensions> | Repo | `Result<T, E>`, `Map`, `Bind` and `Match`, as the backend ***uses*** them | 3, 7 |
| B8 | PostGIS documentation, `ST_AsMVT`, <https://postgis.net/docs/ST_AsMVT.html>, and its ***neighbours*** `ST_AsMVTGeom` and `ST_TileEnvelope` | Docs | The ***three*** ***functions*** that ***build*** a ***tile*** ***inside*** the ***database*** | 7 |
| B9 | NGINX, 'A Guide to Caching with NGINX', <https://blog.nginx.org/blog/nginx-caching-guide> | Article | `proxy_cache`, `proxy_cache_lock` and `proxy_cache_use_stale`, ***line*** by ***line*** as `nginx/default.conf.template` ***uses*** them | 4 |

### Optional

| ID | Resource | Kind | Why it helps | Steps |
|---|---|---|---|---|
| B10 | Enrico Buonanno, *Functional ***Programming*** in C#* (Manning, ***second*** ***edition***, 2022) | ***Book*** | ***Functional*** C# at ***length***: immutable ***records***, `Option`, `Either` and ***pipelines*** | 3, 7 |
| B11 | Microsoft, ***Background*** ***tasks*** with ***hosted*** ***services***, <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services> | Docs | `BackgroundService`, ***behind*** `LiveCatalogue` | 6 |
| B12 | Microsoft, ***Health*** ***checks*** in ASP.NET Core, <https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks> | Docs | `IHealthCheck`, `MapHealthChecks` and the ***response*** writer ***behind*** `/health` | 2 |
| B13 | Regina Obe and Leo Hsu, *PostGIS in Action* (Manning, ***third*** ***edition***, 2021) | ***Book*** | Geometry ***types***, SRIDs and spatial indexes, from ***first*** principles | 7, 9 |
| B14 | GDAL, `ogr2ogr`, <https://gdal.org/en/stable/programs/ogr2ogr.html>, and the PostgreSQL SQL Dump ***driver***, <https://gdal.org/en/stable/drivers/vector/pgdump.html> | Docs | Every ***option*** `npm run convert` ***passes*** | 9 |
| B15 | DbUp/DbUp, <https://github.com/DbUp/DbUp>, and DbUp/dbup-postgresql, <https://github.com/DbUp/dbup-postgresql> | Repo | How DbUp ***runs*** ***each*** ***script*** ***once***, and journals it | 9 |
| B16 | Michael Nygard, *Release It!* (Pragmatic Bookshelf, ***second*** ***edition***, 2018) | ***Book*** | ***Timeouts***, ***pools*** and ***health*** ***checks*** ***under*** ***failure***, as N-22 to N-26 ***use*** them | 5, 7 |
| B17 | Mapbox Vector ***Tile*** specification, <https://github.com/mapbox/vector-tile-spec> | ***Standard*** | What a ***tile*** ***holds***, ***byte*** by ***byte*** | 5, 7 |
| B18 | Dane Springmeyer, 'Vector tiles for fast custom maps' (FOSS4G, 2014), <https://av.tib.eu/en/media/31638> | Video | Why ***maps*** ***moved*** from ***pictures*** to vector ***tiles*** | 5 |

### Repositories to read beside Tessera

| ID | Repository | Why read it |
|---|---|---|
| G1 | CrunchyData/pg_tileserv, <https://github.com/CrunchyData/pg_tileserv> | The ***service*** ***Tessera*** ***follows***: its ***tile*** SQL, its `DATABASE_URL`, and ***publishing*** by ***grant*** |
| G2 | maplibre/martin, <https://github.com/maplibre/martin> | A ***fast*** ***tile*** ***server*** in Rust, for a ***second*** ***view*** of the ***same*** ***job*** |
| G3 | dotnet/eShop, <https://github.com/dotnet/eShop> | Microsoft's ***reference*** ***service***: minimal APIs, ***health*** ***checks*** and ***settings*** at larger ***scale*** |
| G4 | testcontainers/testcontainers-dotnet, <https://github.com/testcontainers/testcontainers-dotnet> | The ***library*** that ***starts*** the ***packaged*** ***database*** for every ***integration*** ***test*** |
| G5 | npgsql/npgsql, <https://github.com/npgsql/npgsql> | The ***driver*** ***itself***, when the ***pool***'s ***behaviour*** ***needs*** a ***source*** to ***settle*** it |

## 3 · The frontend: TypeScript, React and MapLibre

### Essential

| ID | Resource | Kind | Why it helps | Steps |
|---|---|---|---|---|
| M1 | The TypeScript Handbook, <https://www.typescriptlang.org/docs/handbook/intro.html> | Docs | ***Unions***, narrowing, `readonly` and ***generics***, from the ***language***'s ***own*** ***team*** | 10 to 14 |
| M2 | The Elm ***Architecture***, <https://guide.elm-lang.org/architecture/> | Docs | ***Model***, ***message*** and `update`: the ***pattern*** of `map.ts`, ***close*** to F#'s Elmish | 11 |
| M3 | atheck/react-elmish, <https://github.com/atheck/react-elmish> | Repo | `useElmish`, `UpdateMap` and `cmd`, as `App.tsx` and `map.ts` ***use*** them | 11 |
| M4 | React, ***Learn*** React, <https://react.dev/learn>, chiefly 'Escape Hatches' | Docs | ***Components***, ***effects*** and refs, the ***tools*** of `canvas-hooks.ts` | 10, 13 |
| M5 | MapLibre GL JS documentation and ***examples***, <https://maplibre.org/maplibre-gl-js/docs/> | Docs | ***Sources***, ***layers***, symbol placement and clustering | 12, 13 |
| M6 | MapLibre ***Style*** Specification, <https://maplibre.org/maplibre-style-spec/> | ***Standard*** | Every ***key*** `buildStyle` ***writes***, and the ***expressions*** ***inside*** them | 12 |

### Optional

| ID | Resource | Kind | Why it helps | Steps |
|---|---|---|---|---|
| M7 | Dan Vanderkam, *Effective TypeScript* (O'Reilly, ***second*** ***edition***, 2024) | ***Book*** | 83 ***rules*** for ***safe*** ***types***, such as ***branded*** ***types*** and `never` ***checks*** | 11, 14 |
| M8 | Valibot, Introduction, <https://valibot.dev/guides/introduction/> | Docs | ***Checking*** incoming ***data*** ***against*** a ***description*** of its ***shape***, as `areaOf` does | 14 |
| M9 | Vite, <https://vite.dev/guide/>, and Vitest, <https://vitest.dev/> | Docs | How the ***page*** is ***built*** into ***three*** ***files***, and how its ***unit*** ***tests*** ***run*** | 10, 15 |
| M10 | Playwright, <https://playwright.dev/docs/intro> | Docs | The ***browser*** ***tests*** in `web/e2e/` | 15 |
| M11 | ***Material*** ***Design*** 3, <https://m3.material.io/>, and Tailwind CSS, <https://tailwindcss.com/docs> | Docs | The ***colours***, ***shapes*** and motion in `design/`, and the ***classes*** in `styles.css` | 14 |
| M12 | elmish/elmish, <https://github.com/elmish/elmish> | Repo | The F# original of react-elmish, in a ***language*** you ***already*** ***read*** | 11 |
| M13 | fabian-hiller/valibot, <https://github.com/fabian-hiller/valibot> | Repo | Valibot's ***source***, ***small*** ***enough*** to ***read*** in an ***evening*** | 14 |
| M14 | maplibre/maplibre-gl-js, <https://github.com/maplibre/maplibre-gl-js> | Repo | MapLibre's ***own*** ***code*** and ***examples***, when the docs ***leave*** a ***question*** ***open*** | 13 |

## 4 · For single stories

These ***resources*** ***serve*** ***one*** or ***two*** ***stories*** ***each***, so the ***groups*** ***above*** ***leave*** them out.

| ID | Resource | Kind | Why it helps | Stories |
|---|---|---|---|---|
| X1 | PostgreSQL 18, 'Privileges', <https://www.postgresql.org/docs/18/ddl-priv.html> | Docs | `GRANT`, `REVOKE` and `USAGE`: ***publishing*** by ***grant*** | US-03, N-23 |
| X2 | RFC 7946, The GeoJSON ***Format***, <https://www.rfc-editor.org/rfc/rfc7946> | ***Standard*** | The ***format*** of the Macau ***seed***, and why it is in longitude and latitude | US-24 |
| X3 | OpenStreetMap Wiki, 'Slippy map tilenames', <https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames> | Article | The z/x/y ***grid***, and the ***sums*** ***behind*** a ***tile***'s ***address*** | US-07, UI-09, UI-19 |
| X4 | OWASP, 'SQL Injection Prevention Cheat Sheet', <https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html> | Article | ***Values*** as ***parameters*** and ***names*** ***quoted***, the ***two*** ***rules*** of S-01 | S-01 |
| X5 | RFC 9457, ***Problem*** ***Details*** for HTTP APIs, <https://www.rfc-editor.org/rfc/rfc9457> | ***Standard*** | The ***shape*** of every ***error*** ***answer*** | US-07, S-06, N-22 |
| X6 | RFC 3986, URI ***Generic*** ***Syntax***, <https://www.rfc-editor.org/rfc/rfc3986> | ***Standard*** | Percent-encoding and dot ***segments***, which Nginx ***refuses*** | S-06 |
| X7 | W3C, How to ***Meet*** WCAG (***Quick*** ***Reference***), <https://www.w3.org/WAI/WCAG22/quickref/> | ***Standard*** | ***Contrast***, ***names*** and focus, the ***rules*** ***behind*** the ***map***'s ***controls*** | UI-12, UI-15 |
| X8 | dequelabs/axe-core, <https://github.com/dequelabs/axe-core> | Repo | The accessibility ***check*** the ***browser*** ***tests*** ***run*** on ***each*** ***page*** | UI-12, UI-15, US-23 |
| X9 | MDN ***Web*** Docs, 'HTTP caching', <https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Caching> | Docs | `max-age`, `ETag`, 304 and `Vary`, the ***whole*** of ***Part*** C | N-18, N-19, N-20 |

## Where to start

***Start*** with F1, F3, B1 and M2: they ***explain*** the ***shape*** of the ***whole*** ***code*** in ***four*** sittings. Then ***follow*** `docs/guides/reading-the-code.md`, ***reading*** ***each*** ***step***'s essential ***resources*** in the ***week*** you ***reach*** it. ***Keep*** `docs/guides/reading-the-harder-files.md` ***beside*** B3, B5 and B11, which ***explain*** the ***three*** harder ***files***.

| Step of the reading guide | Essential | Optional |
|---|---|---|
| 1 · How the program starts | B1 | F8 |
| 2 · How the service is put together | B1, B4 | B12, F8, G3 |
| 3 · Where the settings come from | B1, B3, B7 | B10 |
| 4 · What Nginx does first | B9 | — |
| 5 · The tile route | B1, B2, B4, F1 | B16, B17, B18, F7 |
| 6 · The list of layers | B6 | B11, F6 |
| 7 · SQL and the pool | B5, B6, B7, B8, F2, F3 | B10, B13, B16, G1, G5 |
| 8 · The page reaches the browser | B2 | — |
| 9 · The places enter the database | — | B13, B14, B15 |
| 10 · The browser starts the page | M1, M4 | M9 |
| 11 · One model, one update | M1, M2, M3, F1, F3 | F5, F6, M7, M12 |
| 12 · The layers and the style | M1, M5, M6 | — |
| 13 · Where the map meets MapLibre | M4, M5, F3 | M14 |
| 14 · The shared kernel and design | M1, F2 | F7, M7, M8, M11, M13 |
| 15 · The tests | F4 | F8, M9, M10, G4 |

## The story map

This ***table*** ***turns*** the ***lists*** ***round***: for ***each*** ***story*** in `docs/user-stories/user-stories.md`, the essential ***resources*** to ***read*** ***before*** you ***build*** it, then the ***optional*** ***ones*** that ***help*** ***most***. F1 to F4 ***serve*** every ***story*** and are not ***repeated***.

| Step | Story | Essential | Optional |
|---|---|---|---|
| | **Part A · The service** | | |
| 1 | US-00 ***Set*** ***up*** the ***walking*** skeleton | B1, B3, M9 | F8, G3, M10 |
| 2 | US-01 ***Start*** the ***same*** ***database*** for the ***service*** and its ***tests*** | B5, G4 | B13 |
| 3 | US-03 ***Publish*** a ***table*** by ***granting*** it | B6, B7, X1, F7 | B13, G1 |
| 4 | US-24 ***Publish*** the ***places*** of the ***area*** in ***nine*** ***layers*** | B14, B15, X2 | B13, B8 |
| 5 | US-07 ***Serve*** a ***tile*** | B2, B4, B8, X3 | B17, B18, G1, G2, X5 |
| 6 | S-01 ***Keep*** SQL out of every ***request*** | B6, X4 | F7 |
| 7 | S-06 ***Refuse*** ***malformed*** URLs | B9, X6 | X5 |
| 8 | US-35 ***Open*** the ***service*** to the ***office*** on ***port*** 80 | B9 | B16 |
| | **Part B · The map** | | |
| 9 | US-23 ***Open*** the ***map*** of the ***area*** | M1, M2, M3, M4, M5 | M7, M8, M9, M10, X8 |
| 10 | UI-09 ***Zoom*** by web-map ***levels*** 0 to 22 | M5, X3 | M6 |
| 11 | UI-15 ***Keep*** the ***page*** ***calm*** in Material 3 | M11, M6 | X7, X8 |
| 12 | UI-12 ***Tell*** ***place*** ***types*** ***apart*** ***without*** ***relying*** on ***colour*** | M6, X7 | M11, X8 |
| 13 | UI-19 ***Read*** ***distance***, ***direction*** and ***position*** | M1, M5 | X3, B13 |
| 14 | UI-10 ***See*** ***crowded*** ***places*** merge into clusters that ***open*** as I ***zoom*** | M5, M6 | M14 |
| 15 | UI-11 ***Read*** ***place*** ***names*** when ***zoomed*** in | M6 | M14 |
| 16 | UI-17 ***Keep*** ***place*** ***names*** from overlapping | M6, M10 | M14 |
| | **Part C · Tiles that cost less** | | |
| 17 | N-18 ***Let*** a ***browser*** ***keep*** a ***tile*** | X9 | B9 |
| 18 | N-19 ***Answer*** a ***kept*** ***tile*** ***without*** a ***query*** | X9, B9 | — |
| 19 | N-20 ***Send*** ***tiles*** and the ***map***'s ***files*** ***packed*** | B9, X9 | M9 |
| | **Part D · The Java way** | | |
| 20 | N-21 ***Name*** the ***settings*** as pg_tileserv ***names*** them | B3, G1 | B1 |
| 21 | N-22 ***Ask*** the ***browser*** to ***wait*** when the ***database*** ***cannot*** ***answer*** | B4, B16 | X5 |
| 22 | N-23 ***Serve*** a ***newly*** ***granted*** ***table*** ***without*** a ***restart*** | B11, X1 | F6, G1 |
| 23 | N-24 ***Serve*** a ***crowd*** from ***one*** ***query*** | B9 | B16 |
| 24 | N-25 ***Tell*** a ***watcher*** the ***service*** is ***well*** | B12 | B16, G3 |
| 25 | N-26 ***Wait*** in the ***order*** of ***arrival*** | B5 | B16, G5 |
| 26 | N-28 ***Answer*** at ***once*** ***while*** the ***database*** ***hangs*** | B9 | B16 |

***Three*** ***stories*** ***now*** ***rest*** on Nginx ***rather*** than on the ***service***'s ***own*** ***code***: N-19, N-24 and N-28 (ADR 0008). For those, B9 ***explains*** the configuration you ***read***, and the ***service***'s ***own*** ***code*** is ***small***.
