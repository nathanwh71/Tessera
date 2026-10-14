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
| Amended a third time | On 14 ***October*** 2026, in the ***change*** that ***halved*** the ***code***, the ***file*** ***took*** ***sections*** and a ***unit*** in ***each*** ***key***'s ***name***, ASP.NET Core's configuration ***replaced*** the hand-written ***reader***, and the ***overrides*** ***moved*** to `TESSERA__`. The ***body*** ***below*** ***describes*** the ***file*** as it ***now*** ***stands*** |
| Amended a fourth time | The ***same*** ***evening*** the ***service*** ***moved*** to ASP.NET Core's ***standard*** start-up: it ***still*** ***reads*** `tessera.json`, which ***gained*** `Urls` and `Logging`, and an ***override*** ***takes*** the ***standard*** ***form***, such as `Tiles__CacheSeconds=0`. The ***body*** ***below*** ***describes*** it |

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

The ***room*** for ***kept*** ***tiles*** is ***now*** Nginx's: `max_size` in `nginx/default.conf.template`, 64 MB (ADR 0008).

## How it works

- **One unit, one form.** A ***key*** that ***holds*** a ***time*** ***names*** its ***unit***, such as `QueryTimeoutSeconds`, and ***takes*** a ***whole*** ***number***. No ***key*** ***takes*** ***two*** ***forms***, such as ***seconds*** or `"1h"`.
- **The file.** The ***service*** ***starts*** on ASP.NET Core's ***standard*** start-up, `WebApplication.CreateBuilder`, and ***adds*** `tessera.json` from ***beside*** ***itself***; the ***import*** does the ***same*** on a ***plain*** ***host***. A ***missing*** ***file*** is no ***fault***: every ***key*** ***takes*** its ***default***.
- **Overrides.** ***Any*** ***environment*** variable ***overrides*** the ***key*** it ***names***, with `__` for ***each*** ***step*** ***down*** a ***level***, so `Tiles__CacheSeconds=0` ***sets*** `Tiles.CacheSeconds`. Variables that ***name*** no ***setting***, such as `TESSERA_DB_PASSWORD`, ***change*** ***nothing***.
- **Checked at start-up.** ***Each*** ***section*** ***binds*** to its ***record*** ***through*** .NET's ***options*** ***pattern***. Attributes such as `[Range]` ***give*** ***each*** ***value*** its ***limits***, a ***record***'s ***own*** `Validate` ***checks*** ***values*** that must agree, such as a ***box*** that ***runs*** ***west*** to ***east***, and the ***check*** ***runs*** ***before*** the ***service*** ***starts***. Every ***value*** out of its ***limits*** is ***named*** at ***once***, with its ***record***, and so is a ***section*** the ***file*** misspells; a ***key*** the ***record*** ***lacks*** ***stops*** the ***start*** on its ***own*** (US-00 c4).
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
| Keep the hand-written reader, with pg_tileserv's flat key names | 535 ***lines*** to ***keep***, and ***two*** ***forms*** for ***one*** ***time***; ASP.NET Core's configuration does the ***same*** ***work*** |
| Keep a small loader of our own on ASP.NET Core's configuration, as between the third and fourth amendments | It ***read*** ***exactly*** ***two*** ***sources***, but it was a ***pattern*** ***only*** this codebase ***uses***; the ***options*** ***pattern*** is the ***one*** .NET's ***own*** ***guides*** ***teach*** |
| Environment variables alone | A ***box*** and a ***home*** ***view*** ***read*** ***badly*** as ***flat*** variables, and ***nothing*** ***records*** the ***office***'s ***values*** in ***one*** ***place*** |
| .NET's default `appsettings.json` | A ***generic*** ***name*** that ***says*** ***nothing*** ***about*** the ***service***, and ***one*** the ***framework*** ***loads*** with ***rules*** of its ***own*** |

## Consequences

- The ***office***'s ***settings*** ***live*** in ***one*** ***file*** you can ***read***, ***copy*** and ***keep*** in Git.
- The ***file*** no ***longer*** ***matches*** the ***archived*** Java ***build***'s, ***byte*** for ***byte*** (ADR 0006, as ***amended***).
- The ***messages*** ***come*** from .NET, such as 'Tiles: The field CacheSeconds must be between 0 and 2147483647.', ***each*** with its ***section*** in ***front***.

## When to revisit

- A ***setting*** ***needs*** to ***change*** ***while*** the ***service*** ***runs***; the ***service*** then ***needs*** to ***reload*** the ***file***.
