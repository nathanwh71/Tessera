# ADR 0003: Read the service's settings from tessera.json

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | The ***Settings*** ***module*** of the ***back*** ***end***, in `nathanwh71/Tessera` |
| Related | ADR 0001, which ***names*** the ***database***'s ***environment*** variables; ADR 0008; US-00, US-07, US-23; N-18 to N-25 |

## Decision

The ***service*** ***reads*** its ***settings*** from ***two*** ***sources***. A JSON ***file***, `tessera.json`, ***holds*** the ***settings*** that ***describe*** the ***service***, ***grouped*** in ***sections***, ***each*** ***time*** with its ***unit*** in its ***name***. ***Environment*** variables ***hold*** ***machines***, ***ports*** and ***passwords***, and may ***override*** ***any*** ***key*** in the ***file***. ASP.NET Core's configuration ***reads*** both.

## What goes where

| Setting | Source | Default |
|---|---|---|
| `Area`: `Name`, `MinLongitude`, `MinLatitude`, `MaxLongitude`, `MaxLatitude`, `HomeLongitude`, `HomeLatitude`, `HomeLevel` | `tessera.json` | Macau |
| `Map.ShowPreview` | `tessera.json` | ***true*** (US-23 c5) |
| `Tiles`: `Resolution`, `Buffer`, `MaxFeatures`, `MinZoom`, `MaxZoom` | `tessera.json` | 4096, 256, 10000, 0 and 22 |
| `Tiles.CacheSeconds`: how long a browser and Nginx keep a tile | `tessera.json` | 60 (N-18) |
| `Database`: `PoolSize`, `ConnectionLifetimeSeconds`, `QueryTimeoutSeconds`, `ConnectionWaitSeconds` | `tessera.json` | 4, 3600, 10 and 3 (N-22) |
| `Catalogue.RefreshSeconds` | `tessera.json` | 30; 0 ***reads*** the ***list*** ***once*** (N-23) |
| `Logging.LogLevel`: Microsoft's own lines at Warning, its start-up lines and Tessera's at Information | `tessera.json` | As ***shipped***; with no ***rules***, .NET ***writes*** ***four*** ***lines*** for every ***request*** |
| `Urls`, where the service listens for Nginx | `tessera.json`; `npm start` ***sets*** `Urls` from `TESSERA_SERVICE_PORT` | `http://127.0.0.1:7800` |
| `DATABASE_URL`, `TESSERA_PUBLISHER_DATABASE_URL` | ***Environment*** | ***None***: a ***password*** ***never*** ***sits*** in a ***file*** |
| `TESSERA_OFFICE_NETWORK`, for Nginx | ***Environment***, ***read*** by `npm start` | ***None***: `npm start` ***refuses*** to ***start*** ***without*** it (S-07, ADR 0007) |

The ***room*** for ***kept*** ***tiles*** ***belongs*** to Nginx: `max_size` in `nginx/default.conf.template`, 64 MB (ADR 0008).

## How it works

- **One unit, one form.** A ***key*** that ***holds*** a ***time*** ***names*** its ***unit***, such as `QueryTimeoutSeconds`, and ***takes*** a ***whole*** ***number***. No ***key*** ***takes*** ***two*** ***forms***, such as ***seconds*** or `"1h"`.
- **The file.** The ***service*** ***starts*** on ASP.NET Core's ***standard*** start-up, `WebApplication.CreateBuilder`, and ***adds*** `tessera.json` from ***beside*** ***itself***; the `seed` ***command*** does the ***same*** on a ***plain*** ***host***. A ***missing*** ***file*** is no ***fault***: every ***key*** ***takes*** its ***default***.
- **Overrides.** ***Any*** ***environment*** variable ***overrides*** the ***key*** it ***names***, with `__` for ***each*** ***step*** ***down*** a ***level***, so `Tiles__CacheSeconds=0` ***sets*** `Tiles.CacheSeconds`. Variables that ***name*** no ***setting***, such as `TESSERA_DB_PASSWORD`, ***change*** ***nothing***.
- **Checked at start-up.** ***Each*** ***section*** ***binds*** to its ***record*** ***through*** .NET's ***options*** ***pattern***. Attributes such as `[Range]` ***give*** ***each*** ***value*** its ***limits***, a ***record***'s ***own*** `Validate` ***checks*** ***values*** that must agree, such as a ***box*** that ***runs*** ***west*** to ***east***, and the ***check*** ***runs*** ***before*** the ***service*** ***starts***. Every ***value*** out of its ***limits*** is ***named*** at ***once***, with its ***record***; a ***key*** the ***record*** ***lacks*** ***stops*** the ***start*** on its ***own*** (US-00 c4). A ***misspelt*** ***section*** ***name*** ***passes*** unseen, and the ***section*** ***keeps*** its ***defaults***.
- **No password in the file.** The ***shipped*** ***file*** ***holds*** no ***password***, and ***listens*** on 127.0.0.1 ***alone***; a ***test*** ***reads*** it and ***fails*** the ***build*** if it ***breaks*** either ***rule***.

The ***shipped*** `tessera.json` for Macau:

```json
{
  "Urls": "http://127.0.0.1:7800",
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft": "Warning", "Microsoft.Hosting.Lifetime": "Information" } },
  "Database": { "PoolSize": 4, "ConnectionLifetimeSeconds": 3600, "QueryTimeoutSeconds": 10, "ConnectionWaitSeconds": 3 },
  "Tiles": { "Resolution": 4096, "Buffer": 256, "MaxFeatures": 10000, "MinZoom": 0, "MaxZoom": 22, "CacheSeconds": 60 },
  "Map": { "ShowPreview": true },
  "Catalogue": { "RefreshSeconds": 30 },
  "Area": {
    "Name": "Macau",
    "MinLongitude": 113.52, "MinLatitude": 22.10, "MaxLongitude": 113.61, "MaxLatitude": 22.22,
    "HomeLongitude": 113.55, "HomeLatitude": 22.19, "HomeLevel": 15
  }
}
```

## Options considered

| Option | Why not |
|---|---|
| A hand-written reader, with pg_tileserv's flat key names | ***Hundreds*** of ***lines*** to ***keep***, and ***two*** ***forms*** for ***one*** ***time***; ASP.NET Core's configuration does the ***same*** ***work*** |
| A small loader of our own on ASP.NET Core's configuration | It would ***read*** ***exactly*** ***two*** ***sources***, but in a ***pattern*** ***only*** this codebase ***uses***; the ***options*** ***pattern*** is the ***one*** .NET's ***own*** ***guides*** ***teach*** |
| Environment variables alone | A ***box*** and a ***home*** ***view*** ***read*** ***badly*** as ***flat*** variables, and ***nothing*** ***records*** the ***office***'s ***values*** in ***one*** ***place*** |
| .NET's default `appsettings.json` | A ***generic*** ***name*** that ***says*** ***nothing*** ***about*** the ***service***, and ***one*** the ***framework*** ***loads*** with ***rules*** of its ***own*** |

## Consequences

- The ***office***'s ***settings*** ***live*** in ***one*** ***file*** you can ***read***, ***copy*** and ***keep*** in Git.
- The ***messages*** ***come*** from .NET, such as 'Tiles: The field CacheSeconds must be between 0 and 2147483647.', ***each*** with its ***section*** in ***front***.

## When to revisit

- A ***setting*** ***needs*** to ***change*** ***while*** the ***service*** ***runs***; the ***service*** then ***needs*** to ***reload*** the ***file***.
