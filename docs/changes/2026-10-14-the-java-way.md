# The Java way: five behaviours brought across

14 ***October*** 2026, ***morning***. ***Carries*** out the maintainer's ***rule*** for Part D: ***follow*** the Java ***way***, so the ***service*** and the database may ***start*** in any ***order***.

## What changed

The .NET ***build*** ***now*** ***behaves*** as the Java ***build*** did in the ***five*** ***places*** where it ***fell*** ***short***. ***Five*** ***commits***, in the ***order*** the ***stories*** ***set***, each with its ***tests*** ***written*** ***first***; GitHub Actions ***passed*** on each.

| Story | Commit | What a maintainer ***sees*** ***now*** |
|---|---|---|
| N-21 | `65acf57` | `tessera.json` is a ***flat*** ***list*** in pg_tileserv's ***names***: `CacheTTL`, `DbPoolMaxConns`, `DefaultBuffer` and the ***rest***. Every ***key*** but `Area` and `HttpPort` has a ***default***. `TESSERA_` and a ***key***'s ***name***, in any ***case***, ***overrides*** it; the ***port*** ***moved*** from `TESSERA_HTTP_PORT` to `TESSERA_HTTPPORT` |
| N-22 | `a310bea` | A database that ***cannot*** ***answer*** ***gives*** 503 with `Retry-After: 5`, not 500; a query past `DbTimeout` (10 s) is ***cancelled*** on the ***server*** |
| N-23 | `a944b70` | A ***grant*** ***shows*** ***within*** `CatalogueRefresh` ***seconds*** (30) with no restart; a ***revoke*** ***takes*** the ***layer***'s ***kept*** tiles with it. The ***service*** ***starts*** with the database down and ***answers*** 503 until the ***list*** ***loads*** |
| N-24 | `af27f51` | ***One*** query for a ***crowd*** ***asking*** for ***one*** ***unbuilt*** tile |
| N-25 | `a3c0999` | `/health` ***answers*** `UP` or `DOWN`; `npm start` ***waits*** for `UP` |

## How each works

**N-21, the names.** `SettingsReader.Keys` ***lists*** every ***key*** the ***service*** ***reads***. An ***override*** must ***name*** ***one*** of them, so `TESSERA_DB_PASSWORD` ***changes*** ***nothing***. The ***flat*** ***keys*** ***fill*** ***grouped*** ***records***, `HttpSettings`, `DbSettings`, `TileSettings` and `CatalogueSettings`, so a ***port*** ***cannot*** ***stand*** where a ***pool*** ***size*** ***belongs***. `DbPoolMaxConnLifeTime` ***takes*** pg_tileserv's ***form*** of a ***time***, such as `1h` or `1h30m`. ***Two*** ***keys*** ***differ*** in ***meaning*** from pg_tileserv's: `CacheTTL` 0 ***sends*** `no-cache` ***rather*** than no header, and `MaxFeaturesPerTile` ***takes*** no -1. ADR 0003 is ***amended***.

**N-22, waiting.** Sql ***gained*** `PoolLimits`, ***filled*** from `DbSettings`, ***because*** Sql may not ***reference*** Settings. `DatabaseError.IsUnavailable` is ***true*** for a ***failed*** ***connection*** (`08`), a ***cancelled*** query (`57014`), a ***server*** ***shutting*** ***down*** or ***starting*** (`57P01` to `57P03`) and ***too*** ***many*** ***connections*** (`53300`). Only Http ***writes*** the 503, ***through*** `Problems.Unavailable`.

**N-23, the live list.** `LiveCatalogue` ***holds*** ***one*** immutable ***list*** and ***swaps*** it ***whole***. A ***hosted*** ***service*** ***rereads*** it every `CatalogueRefresh` ***seconds***, and every 5 until the ***first*** ***load*** ***succeeds***. A ***failed*** ***reload*** ***keeps*** the ***last*** ***good*** ***list***.

**N-24, the crowd.** The tile ***server*** ***keeps*** the queries in ***flight*** in a ***map*** ***keyed*** by tile. `Lazy` ***makes*** ***sure*** ***two*** ***racing*** ***requests*** ***start*** ***one*** query. The ***shared*** query ***runs*** on the ***service***'s ***stop*** ***signal***; each ***request*** ***waits*** on its ***own***, so a browser that ***leaves*** ***never*** ***cancels*** the tile for the ***rest***.

**N-25, health.** ASP.NET Core's ***own*** ***health*** ***checks***, at `/health` as in pg_tileserv: the ***list*** has ***loaded***, and the database ***answers*** `SELECT 1` ***within*** a ***second***. The ***ping*** is `HealthQueries.Ping`, so every SQL ***text*** ***still*** ***lives*** in Sql.

## Proved by hand

`npm start` on a ***spare*** ***port*** ***built*** the ***map***, ***started*** the database, ***imported*** the 900 ***places***, ***started*** the ***service***, and ***printed*** '***Tessera*** is ***ready***' once `/health` ***said*** `UP`. A tile then ***answered*** 404, ***since*** ***nothing*** was ***granted***; `npm run publish` ***granted*** the ***nine*** ***layers***, and 21 ***seconds*** ***later*** the same tile ***answered*** 200, with no restart.

## Tests

160 ***unit***, 4 ***architecture*** and 45 integration ***tests*** ***pass***, with 10 in the ***scripts***. ***Two*** ***tests*** ***prove*** ***behaviour*** ***against*** the ***real*** database with a ***view*** that ***sleeps*** ***three*** ***seconds***: with a `DbTimeout` of ***one***, the tile ***answers*** 503 in ***under*** ***three***; and twenty ***requests*** for it ***answer*** 200 in ***about*** ***three***. With the ***sharing*** ***removed***, that ***second*** ***test*** ***failed*** 16 of 20, which ***shows*** it ***tests*** what it ***claims***.

## The two started in the other order

***Last***, the ***service*** ***started*** ***against*** a database ***container*** ***still*** ***setting*** ***itself*** up, and `/health` ***said*** `DOWN`. The database then ***came*** up, the ***places*** were ***imported*** and ***granted***, and ***one*** ***second*** ***later*** `/health` ***said*** `UP`; ***two*** ***seconds*** ***later*** a tile ***answered*** 200. No restart. The ***map***'s 20 browser ***tests*** then ***passed*** ***against*** that ***same*** ***service***.

## One slip, mended

While ***proving*** that ***last*** ***point***, a `git checkout` ***threw*** away the ***uncommitted*** ***sharing*** ***code***. It ***came*** ***back*** from the ***stash*** ***entry*** ***still*** ***held*** by Git, and every ***suite*** ***ran*** ***again*** before the ***commit***. ***Nothing*** ***reached*** GitHub ***broken***.

## What to read next

- ADR 0003, as ***amended***: the ***keys***, their ***defaults*** and their ***overrides***.
- Part D of the ***stories***: ***steps*** 20 to 24.
