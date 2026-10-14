# Half the size: libraries and Nginx in place of our own code

14 ***October*** 2026. The ***maintainer*** ***found*** the ***code*** ***too*** ***hard*** to ***follow*** and ***asked*** to ***halve*** it, with ***any*** ***library*** he ***liked***, ASP.NET Core's ***among*** them, and with Nginx as the ***cache*** and the passive ***health*** ***check***. He ***chose*** ***one*** ***format*** for ***each*** ***unit*** in `tessera.json`, ***one*** ***copy*** of Nginx for ***now***, and CSharpFunctionalExtensions for `Result`.

## The result

The ***source*** ***fell*** from 3,743 ***lines*** to 1,457, and its ***code*** ***alone***, ***without*** ***comments*** or blank ***lines***, from 1,827 to 895: a ***little*** over ***half*** ***gone***. The ***tests*** ***fell*** from 2,715 ***lines*** to 1,672. Nginx's ***file*** ***adds*** 72 ***lines***, and its ***checks*** in `scripts/lib/edge.ts` 94. Every ***test*** ***passes***: 69 ***unit*** ***tests***, 40 ***integration*** ***tests***, 17 ***browser*** ***tests***, and the Nginx ***checks***.

| File | Lines | What it holds |
|---|---|---|
| `Settings/Settings.cs` | 259 | Every ***setting***, its ***default*** and its ***limits***, and the loader that ***names*** every ***problem*** at ***once*** |
| `Import/Seed.cs` | 186 | The GeoJSON ***checks*** |
| `Sql/Database.cs` | 185 | The ***one*** ***door*** to PostgreSQL |
| `Import/Importer.cs` | 153 | The ***import***'s ***plan*** and its ***transaction*** |
| `Sql/Queries.cs` | 137 | Every SQL ***text*** |
| `Tiles/Tiles.cs` | 132 | The ***tile*** ***route*** |
| `Catalogue/Catalogue.cs` | 115 | The ***list*** of ***layers*** and its refresh |
| `Map/Map.cs` | 113 | The ***map***'s ***page*** and ***files*** |
| `Service.cs` | 80 | The ***service*** ***put*** ***together***, and `/health` |
| `Program.cs` | 62 | The ***two*** ***commands*** |
| `Problems.cs` | 35 | The ***shape*** of every ***problem*** ***document*** |

## Seven steps, one commit each

***Each*** ***count*** is net: ***lines*** ***removed*** ***less*** ***lines*** ***added***, in `src/` and in `tests/`.

| Commit | Step | Source | Tests |
|---|---|---|---|
| `dc8e2a8` | The ***nine*** ***projects*** merge into ***one***, a ***folder*** for ***each*** ***module***; the ***architecture*** ***tests*** ***go*** | 79 | 113 |
| `ed915bf` | Nginx in ***front***: `nginx/default.conf.template`, `npm start`, and the ***browser*** ***tests*** ***through*** it | 0 | 0 |
| `92099c8` | The ***tile*** ***cache***, the single-flight ***table***, gzip, 304, the ***target*** ***screen*** and the ***database*** ***watch*** ***go*** | 719 | 884 |
| `b645ea2` | ASP.NET Core ***reads*** the ***settings*** and ***writes*** the ***problem*** ***documents*** | 407 | 94 |
| `123adaf` | CSharpFunctionalExtensions, ***nullable*** ***types*** and Dapper's ***named*** ***parameters*** ***replace*** `Option`, `Result` and the ***query*** ***builder*** | 1,159 | 97 |
| `ca1d9ea` | Nginx ***answers*** its ***own*** 400s with a ***problem*** ***document*** | 0 | 0 |
| `ec48dc6` | The ***pool*** ***tests*** of N-26 ***return*** on the ***new*** ***types*** | 0 | 118 ***added*** |

ADR 0008 ***records*** why Nginx ***stands*** in ***front***, and what it does in ***place*** of the ***service***.

## What changed for the operator

- **Two ports.** Nginx ***answers*** the ***office*** on `TESSERA_HTTPPORT`, 80 in the ***office***; the ***service*** ***answers*** Nginx on 127.0.0.1, ***port*** 7800, which `TESSERA_SERVICE_PORT` may ***change***. `npm start` ***starts*** both. ***Port*** 80 no ***longer*** ***needs*** Ubuntu's ***change*** to its ***limit*** on ***low*** ***ports***, ***since*** Nginx ***runs*** as ***root*** ***inside*** its ***container***.
- **One form for each setting.** `tessera.json` ***now*** ***groups*** its ***keys*** in ***sections***, and ***each*** ***time*** ***carries*** its ***unit*** in its ***name***: `Tiles.CacheSeconds`, `Database.QueryTimeoutSeconds`, `Database.ConnectionWaitSeconds`, `Catalogue.RefreshSeconds`. The ***room*** for ***kept*** ***tiles*** is Nginx's `max_size`, 64 MB. ADR 0003 ***lists*** every ***key***.
- **Overrides.** `TESSERA__` and a ***key***'s ***path*** ***override*** it, with `__` for ***each*** ***step*** ***down***: `TESSERA__TILES__CACHESECONDS=0`.
- **A problem for every malformed address.** Nginx ***refuses*** an ***address*** it ***cannot*** ***read***, such as `%00`, with the ***same*** ***problem*** ***document*** the ***service*** ***writes***, so the ***old*** ***gap*** in S-06 has ***closed***.

## What the change gave up

| Given up | Why it is acceptable |
|---|---|
| A withdrawn layer's kept tiles leave at once (N-23 c3) | They ***stay*** in Nginx for ***up*** to `Tiles.CacheSeconds`, 60 by ***default***, as they ***already*** ***stay*** in ***browsers*** |
| 503 at once while the database hangs (N-28) | Nginx ***serves*** every ***tile*** it ***holds***; a ***tile*** it ***never*** ***held*** ***waits*** ***up*** to the ***database***'s ***own*** ***limits***, then ***answers*** 503 |
| The passive health check, for now | It ***needs*** a ***second*** ***copy*** of the ***service***; ADR 0008 ***says*** how to ***add*** ***one*** |
| The compiler's proof that only `Sql` writes SQL (S-01 c2) | Every SQL ***text*** ***still*** ***lives*** in `Queries.cs`, and every ***value*** ***still*** ***travels*** as a ***parameter***; a ***reviewer***, not the ***compiler***, ***keeps*** the ***rule*** |
| The compiler's guard on the module borders (ADR 0001) | The ***folders*** ***keep*** the ***modules*** ***apart***; ***one*** ***project*** is easier to ***read*** |
| One settings file for both builds (ADR 0006 rule 5) | The Java ***build*** is ***archived***; its ***own*** ***file*** ***stays*** as it was |

## Checked

- `dotnet run --project tests/Tessera.UnitTests`: 69 ***passed***.
- `dotnet run --project tests/Tessera.IntegrationTests`: 40 ***passed***, ***against*** the ***packaged*** ***database***.
- `node scripts/e2e.ts --grep-invert "approved image"`: 17 ***browser*** ***tests*** ***passed*** ***through*** Nginx, then every Nginx ***check***.
- Nginx ***answered*** ***malformed*** ***addresses*** as ***below***, ***checked*** with ***requests*** ***written*** ***byte*** for ***byte***.

| Address | Through Nginx |
|---|---|
| `/food.cafes/../../1/2/3.pbf`, `/../etc/passwd` | 400, a ***problem*** ***document*** |
| `/food.cafes/15/1/2.pbf%zz`, `…%00`, a raw tab | 400, a ***problem*** ***document*** |
| `/food.cafes/%2e%2e/1/2/3.pbf`, `/food.cafes%2F15/1/2.pbf`, a backslash, `%01` | 404 from the ***service***: no ***route*** ***matches*** |
| `/food.c%61fes/15/1/2.pbf` | The ***tile*** of `food.cafes`: `%61` is the ***letter*** a, and no ***longer*** ***refused*** |

## Update, the same evening: the standard start-up

The ***maintainer*** ***asked*** ***whether*** ***moving*** the ***settings*** into ASP.NET Core's ***own*** ***file*** would simplify the ***code***, ***kept*** the ***name*** `tessera.json`, and ***chose*** the ***move***. ***Commit*** `da62c21` ***starts*** the ***service*** on `WebApplication.CreateBuilder`, ***binds*** ***each*** ***section*** ***through*** .NET's ***options*** ***pattern***, and ***checks*** every ***value*** ***before*** the ***service*** ***starts***. `SettingsLoader` and `HttpSettings` ***went***; `tessera.json` ***gained*** `Urls` and `Logging`; an ***override*** ***now*** ***takes*** the ***standard*** ***form***, such as `Tiles__CacheSeconds=0`; and `TESSERA_CONFIG` ***went***.

The estimate of ***about*** 100 ***fewer*** ***lines*** was ***wrong***. The ***source*** ***went*** from 1,457 ***lines*** to 1,454, and its ***code*** ***alone*** from 895 to 897: the loader's ***lines*** ***moved*** into the ***service***'s registrations, the ***import***'s ***host*** and the ***printing*** of ***problems***. What the ***change*** bought is the ***pattern*** .NET's ***own*** ***guides*** ***teach***, in ***place*** of ***one*** of our ***own***. A ***key*** the ***record*** ***lacks*** ***now*** ***stops*** the ***start*** on its ***own***, ***before*** the ***other*** ***problems*** are ***named***. Every ***test*** ***passes***: 68 ***unit*** ***tests***, 40 ***integration*** ***tests***, 17 ***browser*** ***tests*** and the Nginx ***checks***.
