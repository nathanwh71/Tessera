# ADR 0003: Read the service's settings from tessera.json

| | |
|---|---|
| Status | ***Accepted***; ***amended*** 14 ***October*** 2026 |
| Date | 13 ***October*** 2026 |
| Decider | The maintainer |
| Covers | The Settings module of the back ***end***, in `nathanwh71/Tessera` |
| Related | ADR 0001, which ***names*** the database's ***environment*** variables; ADR 0006 ***rule*** 5; US-00, US-07, US-23; N-18 to N-25 |
| Amended | On 14 ***October*** 2026 the maintainer ***chose*** pg_tileserv's ***own*** ***key*** ***names*** in a ***flat*** ***file*** (N-21). The ***port*** ***moved*** from `TESSERA_HTTP_PORT` to `TESSERA_HTTPPORT`, the ***override*** of `HttpPort` |
| Amended again | ***Later*** the same ***day*** the ***file*** ***became*** the Java ***build***'s, ***byte*** for ***byte***, under ADR 0006 ***rule*** 5: its `Area` ***keys***, its ***defaults***, and its milliseconds for `DbPoolConnectionTimeout` |

## Decision

The ***service*** ***reads*** its ***settings*** from ***two*** ***sources***, ***split*** as pg_tileserv ***splits*** its own. A JSON ***file***, `tessera.json`, ***holds*** the ***settings*** that ***describe*** the ***service***. ***Environment*** variables ***hold*** ***machines***, ***ports*** and ***passwords***, and may ***override*** any ***key*** in the ***file***.

## What goes where

| Setting | Source | Why |
|---|---|---|
| The ***area***'s ***name***, ***box*** and ***home*** ***view***: `Area`, with `Name`, `MinLongitude`, `MinLatitude`, `MaxLongitude`, `MaxLatitude`, `HomeLongitude`, `HomeLatitude`, `HomeLevel` | `tessera.json`; Macau when ***left*** out | The ***stories*** ***call*** them ***settings*** the ***office*** ***sets*** |
| The ***map*** preview, `ShowPreview` | `tessera.json`; ***true*** | US-23, ***criterion*** 5 |
| The tile's ***cap*** on ***features***, `MaxFeaturesPerTile`; its buffer, `DefaultBuffer`; its ***width***, `DefaultResolution`; its ***zoom*** ***range***, `DefaultMinZoom` and `DefaultMaxZoom` | `tessera.json`; 10000, 256, 4096, 0 and 22 | pg_tileserv's ***names*** and ***defaults*** |
| How ***long*** a ***browser*** ***keeps*** a tile, `CacheTTL`, and the ***room*** for ***kept*** ***copies***, `TileCacheMegabytes` | `tessera.json`; 0 ***seconds*** and 64 MB; the ***shipped*** ***file*** ***sets*** 60 ***seconds*** | N-18, N-19 |
| The database ***pool***: `DbPoolMaxConns`, `DbPoolMaxConnLifeTime`, `DbTimeout`, `DbPoolConnectionTimeout` | `tessera.json`; 4, `1h`, 10 ***seconds*** and 3000 milliseconds | N-22; the Java ***build***'s ***tuning*** ***guide*** ***chose*** the ***numbers*** |
| How ***often*** the ***list*** of ***layers*** is ***reread***, `CatalogueRefresh` | `tessera.json`; 30 ***seconds***, 0 for ***never*** | N-23 |
| The ***web*** server's ***queue***, `HttpMaxConnections` and `HttpAcceptCount` | `tessera.json`; 8192 and 100 | The Java ***build***'s Tomcat ***defaults*** |
| The ***address*** and ***port*** the ***service*** ***listens*** on, `HttpHost` and `HttpPort` | ***Environment***, `TESSERA_HTTPHOST` and `TESSERA_HTTPPORT`; 0.0.0.0 and 7800, as in pg_tileserv | US-00, ***criterion*** 4; the ***shipped*** ***file*** ***names*** no ***machine*** and no ***port*** |
| `DATABASE_URL`, `TESSERA_PUBLISHER_DATABASE_URL` | ***Environment*** | US-00 and ADR 0001; a ***password*** ***never*** ***sits*** in a ***file*** |

## How it works

- **One file for both builds.** `tessera.json` is the Java ***build***'s ***file***, ***byte*** for ***byte*** (ADR 0006 ***rule*** 5): its ***keys***, its ***forms*** and its ***defaults***, which are pg_tileserv's ***wherever*** pg_tileserv has the ***setting***. A ***test*** in each ***build*** ***checks*** the ***shipped*** ***file***'s fingerprint, so a ***change*** to ***one*** ***copy*** ***fails*** until the other ***matches***.
- **The file.** `TESSERA_CONFIG` ***names*** the ***file***'s ***path***; without that variable, the ***service*** ***reads*** `tessera.json` beside ***itself***. A ***missing*** ***file*** is no ***fault***, as in pg_tileserv: every ***key*** ***takes*** its ***default***. The ***service*** ***reads*** no other JSON ***file***: it ***builds*** its ***settings*** without .NET's ***default*** `appsettings.json`.
- **Overrides.** An ***environment*** variable ***starting*** `TESSERA_` ***overrides*** the ***key*** it ***names***, in any ***case***, as pg_tileserv's `TS_HTTPPORT` ***overrides*** `HttpPort`: `TESSERA_CACHETTL` ***overrides*** `CacheTTL`, and `TESSERA_AREA__HOMELEVEL` ***overrides*** `HomeLevel` in `Area`. A variable that ***names*** no ***key***, such as `TESSERA_DB_PASSWORD`, is ***left*** ***alone***.
- **Forms.** ***Times*** are ***whole*** ***seconds***, ***except*** ***two***: `DbPoolMaxConnLifeTime` ***takes*** ***seconds*** or pg_tileserv's ***form***, such as `"1h"` or `"1h30m"`, and `DbPoolConnectionTimeout` ***counts*** milliseconds, as the Java ***build***'s ***pool*** ***does***. A ***number*** may ***come*** as JSON ***text***, `"60"`, since an ***environment*** variable is always ***text***.
- **Checked at start-up.** Settings ***reads*** both ***sources*** into ***one*** immutable ***record*** and ***returns*** a `Result`. A ***wrong*** ***value***, or a ***key*** neither ***build*** ***knows***, ***stops*** the ***service***, which ***names*** the ***key***, as US-00 ***requires***; a ***key*** no ***build*** ***reads*** is ***most*** ***often*** a misspelling.
- **One rule for the file.** It ***names*** no ***machine***, no ***port*** and no ***password***. A ***test*** ***reads*** the ***shipped*** `tessera.json` and ***fails*** the ***build*** if it ***breaks*** that ***rule***.
- **Grouped in the code.** The ***flat*** ***keys*** ***fill*** ***grouped*** ***records***, `HttpSettings`, `DbSettings`, `TileSettings` and `CatalogueSettings`, so a ***port*** ***cannot*** ***stand*** where a ***pool*** ***size*** ***belongs***.

The ***shipped*** `tessera.json` for Macau ***looks*** ***like*** this. The ***box*** ***comes*** from US-24; the other ***numbers*** are the Java ***build***'s.

```json
{
  "DefaultResolution": 4096,
  "DefaultBuffer": 256,
  "MaxFeaturesPerTile": 10000,
  "DefaultMinZoom": 0,
  "DefaultMaxZoom": 22,
  "DbPoolMaxConns": 4,
  "DbPoolMaxConnLifeTime": "1h",
  "DbTimeout": 10,
  "DbPoolConnectionTimeout": 3000,
  "CacheTTL": 60,
  "TileCacheMegabytes": 64,
  "ShowPreview": true,
  "CatalogueRefresh": 30,
  "HttpMaxConnections": 8192,
  "HttpAcceptCount": 100,
  "Area": {
    "Name": "Macau",
    "MinLongitude": 113.52,
    "MinLatitude": 22.10,
    "MaxLongitude": 113.61,
    "MaxLatitude": 22.22,
    "HomeLongitude": 113.55,
    "HomeLatitude": 22.19,
    "HomeLevel": 15
  }
}
```

## Options considered

| Option | Why not |
|---|---|
| ***Environment*** variables ***alone*** | A ***box*** and a ***home*** ***view*** ***read*** ***badly*** as ***flat*** variables, and ***nothing*** ***records*** the ***office***'s ***values*** in ***one*** ***place*** |
| A TOML ***file***, as pg_tileserv ***uses*** | .NET ***reads*** JSON ***built*** in; TOML would ***need*** a ***library*** for no ***gain*** |
| .NET's ***default*** `appsettings.json` | A ***generic*** ***name*** that ***says*** ***nothing*** about the ***service***, and ***one*** the ***framework*** ***loads*** with ***rules*** of its own |

## Consequences

- The ***office***'s ***settings*** ***live*** in ***one*** ***file*** you can ***read***, ***copy*** and ***keep*** in Git.
- ***Two*** ***sources*** ***mean*** ***two*** ***places*** to ***look***; the ***table*** ***above*** ***says*** which ***holds*** what.

## When to revisit

- A ***setting*** ***needs*** to ***change*** while the ***service*** ***runs***; the ***service*** then ***needs*** to ***reload*** the ***file***.
