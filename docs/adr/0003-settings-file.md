# ADR 0003: Read the service's settings from tessera.json

| | |
|---|---|
| Status | ***Accepted***; ***amended*** 14 ***October*** 2026 |
| Date | 13 ***October*** 2026 |
| Decider | The maintainer |
| Covers | The Settings module of the back ***end***, in `nathanwh71/Tessera` |
| Related | ADR 0001, which ***names*** the database's ***environment*** variables; US-00, US-07, US-23; N-18 to N-23 |
| Amended | On 14 ***October*** 2026 the maintainer ***chose*** pg_tileserv's ***own*** ***key*** ***names*** in a ***flat*** ***file*** (N-21). ***Keys*** pg_tileserv ***lacks*** ***take*** its ***style*** of ***name***; `Area` ***stays*** ***nested***. The ***port*** ***moved*** from `TESSERA_HTTP_PORT` to `TESSERA_HTTPPORT`, the ***override*** of `HttpPort` |

## Decision

The ***service*** ***reads*** its ***settings*** from ***two*** ***sources***, ***split*** as pg_tileserv ***splits*** its own. A JSON ***file***, `tessera.json`, ***holds*** the ***settings*** that ***describe*** the ***service***. ***Environment*** variables ***hold*** ***machines***, ***ports*** and ***passwords***, and may ***override*** any ***key*** in the ***file***.

## What goes where

| Setting | Source | Why |
|---|---|---|
| The ***area***'s ***name***, ***box*** and ***home*** ***view*** | `tessera.json` | The ***stories*** ***call*** them ***settings*** the ***office*** ***sets*** |
| The ***map*** preview, `ShowPreview` | `tessera.json`; ***true*** when ***left*** out | US-23, ***criterion*** 5 |
| The tile's ***cap*** on ***features***, `MaxFeaturesPerTile`, and its buffer, `DefaultBuffer` | `tessera.json`; 10000 and 64 | US-07 ***leaves*** them ***open***; the ***file*** ***lets*** you ***tune*** them without a ***build*** |
| How ***long*** a tile is ***kept***, `CacheTTL`, and the ***room*** for ***kept*** ***copies***, `TileCacheMegabytes` | `tessera.json`; 60 ***seconds*** and 64 MB | N-18, N-19 |
| The database ***pool***: `DbPoolMaxConns`, `DbPoolMaxConnLifeTime`, `DbTimeout`, `DbPoolConnectionTimeout` | `tessera.json`; 4, `1h`, 10 s and 3 s | N-22; the Java ***build***'s ***tuning*** ***guide*** ***chose*** the ***numbers*** |
| How ***often*** the ***list*** of ***layers*** is ***reread***, `CatalogueRefresh` | `tessera.json`; 30 ***seconds***, 0 for ***never*** | N-23 |
| The ***port*** the ***service*** ***listens*** on, `HttpPort` | ***Environment***, `TESSERA_HTTPPORT`; the ***shipped*** ***file*** ***leaves*** it out | US-00, ***criterion*** 4 |
| `DATABASE_URL`, `TESSERA_PUBLISHER_DATABASE_URL` | ***Environment*** | US-00 and ADR 0001; a ***password*** ***never*** ***sits*** in a ***file*** |

## How it works

- **The file.** `TESSERA_CONFIG` ***names*** the ***file***'s ***path***, as in the Java ***build***; without that variable, the ***service*** ***reads*** `tessera.json` beside ***itself***. The ***service*** ***reads*** no other JSON ***file***: it ***builds*** its ***settings*** without .NET's ***default*** `appsettings.json`, so ***one*** ***file*** ***holds*** every ***value***.
- **Overrides.** An ***environment*** variable ***starting*** `TESSERA_` ***overrides*** the ***key*** it ***names***, in any ***case***, as pg_tileserv's `TS_HTTPPORT` ***overrides*** `HttpPort`: `TESSERA_CACHETTL` ***overrides*** `CacheTTL`. `__` ***steps*** ***down*** a ***level***, as in `TESSERA_AREA__HOME__ZOOM`. A variable that ***names*** no ***key***, such as `TESSERA_DB_PASSWORD`, is ***left*** ***alone***.
- **Defaults.** Every ***key*** but `Area` and `HttpPort` may be ***left*** out, as in pg_tileserv; the ***table*** ***above*** ***gives*** each ***default***. ***Two*** ***differ*** in ***meaning*** from pg_tileserv's: `CacheTTL` 0 ***sends*** `no-cache` ***rather*** than no header, and `MaxFeaturesPerTile` ***takes*** no -1 for '***no*** ***limit***'.
- **Checked at start-up.** Settings ***reads*** both ***sources*** into ***one*** immutable ***record*** and ***returns*** a `Result`. A ***missing*** or ***wrong*** ***key*** ***stops*** the ***service***, which ***names*** the ***key***, as US-00 ***requires***.
- **One rule for the file.** It ***names*** no ***machine***, no ***port*** and no ***password***, and every ***key*** in it is ***one*** the ***service*** ***reads***. ***Two*** ***tests*** ***read*** the ***shipped*** `tessera.json` and ***fail*** the ***build*** if it ***breaks*** either ***rule***.
- **Grouped in the code.** The ***flat*** ***keys*** ***fill*** ***grouped*** ***records***, `HttpSettings`, `DbSettings`, `TileSettings` and `CatalogueSettings`, so a ***port*** ***cannot*** ***stand*** where a ***pool*** ***size*** ***belongs***.

The ***shipped*** `tessera.json` for Macau ***looks*** ***like*** this. The ***box*** ***comes*** from US-24; the other ***numbers*** are ***examples***, ***since*** US-07 and US-23 ***leave*** them ***open***.

```json
{
  "ShowPreview": true,
  "DbPoolMaxConns": 4,
  "DbPoolMaxConnLifeTime": "1h",
  "DbTimeout": 10,
  "DbPoolConnectionTimeout": 3,
  "MaxFeaturesPerTile": 10000,
  "DefaultBuffer": 64,
  "CacheTTL": 60,
  "TileCacheMegabytes": 64,
  "CatalogueRefresh": 30,
  "Area": {
    "Name": "Macau",
    "Box": { "West": 113.52, "South": 22.10, "East": 113.61, "North": 22.22 },
    "Home": { "Longitude": 113.55, "Latitude": 22.19, "Zoom": 15 }
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
