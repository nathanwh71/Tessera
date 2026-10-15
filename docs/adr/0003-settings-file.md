# ADR 0003: Read the service's settings from tessera.json

| | |
|---|---|
| Status | <span style="color:grey">Accepted</span> |
| Date | 13 <span style="color:grey">October</span> 2026 |
| Decider | The <span style="color:grey">maintainer</span> |
| Covers | The <span style="color:grey">Settings</span> ***module*** of the <span style="color:grey">back</span> <span style="color:grey">end</span>, in `nathanwh71/Tessera` |
| Related | ADR 0001, which <span style="color:grey">names</span> the <span style="color:grey">database</span>'s ***environment variables***; ADR 0008; US-00, US-07, US-23; N-18 to N-25 |

## Decision

The <span style="color:grey">service</span> <span style="color:grey">reads</span> its <span style="color:grey">settings</span> from <span style="color:grey">two</span> <span style="color:grey">sources</span>. A JSON <span style="color:grey">file</span>, `tessera.json`, <span style="color:grey">holds</span> the <span style="color:grey">settings</span> that <span style="color:grey">describe</span> the <span style="color:grey">service</span>, <span style="color:grey">grouped</span> in <span style="color:grey">sections</span>, <span style="color:grey">each</span> <span style="color:grey">time</span> with its <span style="color:grey">unit</span> in its <span style="color:grey">name</span>. <span style="color:grey">Environment</span> variables <span style="color:grey">hold</span> <span style="color:grey">machines</span>, <span style="color:grey">ports</span> and <span style="color:grey">passwords</span>, and may <span style="color:grey">override</span> <span style="color:grey">any</span> <span style="color:grey">key</span> in the <span style="color:grey">file</span>. ASP.NET Core's configuration <span style="color:grey">reads</span> both.

## What goes where

| Setting | Source | Default |
|---|---|---|
| `Area`: `Name`, `MinLongitude`, `MinLatitude`, `MaxLongitude`, `MaxLatitude`, `HomeLongitude`, `HomeLatitude`, `HomeLevel` | `tessera.json` | Macau |
| `Map.ShowPreview` | `tessera.json` | <span style="color:grey">true</span> (US-23 c5) |
| `Tiles`: `Resolution`, `Buffer`, `MaxFeatures`, `MinZoom`, `MaxZoom` | `tessera.json` | 4096, 256, 10000, 0 and 22 |
| `Tiles.CacheSeconds`: how long a browser and Nginx keep a tile | `tessera.json` | 60 (N-18) |
| `Database`: `PoolSize`, `ConnectionLifetimeSeconds`, `QueryTimeoutSeconds`, `ConnectionWaitSeconds` | `tessera.json` | 4, 3600, 10 and 3 (N-22) |
| `Catalogue.RefreshSeconds` | `tessera.json` | 30; 0 <span style="color:grey">reads</span> the <span style="color:grey">list</span> <span style="color:grey">once</span> (N-23) |
| `Logging.LogLevel`: Microsoft's own lines at Warning, its start-up lines and Tessera's at Information | `tessera.json` | As <span style="color:grey">shipped</span>; with no <span style="color:grey">rules</span>, .NET <span style="color:grey">writes</span> <span style="color:grey">four</span> <span style="color:grey">lines</span> for every <span style="color:grey">request</span> |
| `Urls`, where the service listens for Nginx | `tessera.json`; `npm start` <span style="color:grey">sets</span> `Urls` from `TESSERA_SERVICE_PORT` | `http://127.0.0.1:7800` |
| `DATABASE_URL`, `TESSERA_PUBLISHER_DATABASE_URL` | <span style="color:grey">Environment</span> | <span style="color:grey">None</span>: a <span style="color:grey">password</span> <span style="color:grey">never</span> <span style="color:grey">sits</span> in a <span style="color:grey">file</span> |
| `TESSERA_OFFICE_NETWORK`, for Nginx | <span style="color:grey">Environment</span>, <span style="color:grey">read</span> by `npm start` | <span style="color:grey">None</span>: `npm start` <span style="color:grey">refuses</span> to <span style="color:grey">start</span> <span style="color:grey">without</span> it (S-07, ADR 0007) |

The <span style="color:grey">room</span> for <span style="color:grey">kept</span> ***tiles*** <span style="color:grey">belongs</span> to Nginx: `max_size` in `nginx/default.conf.template`, 64 MB (ADR 0008).

## How it works

- **One unit, one form.** A <span style="color:grey">key</span> that <span style="color:grey">holds</span> a <span style="color:grey">time</span> <span style="color:grey">names</span> its <span style="color:grey">unit</span>, such as `QueryTimeoutSeconds`, and <span style="color:grey">takes</span> a <span style="color:grey">whole</span> <span style="color:grey">number</span>. No <span style="color:grey">key</span> <span style="color:grey">takes</span> <span style="color:grey">two</span> <span style="color:grey">forms</span>, such as <span style="color:grey">seconds</span> or `"1h"`.
- **The file.** The <span style="color:grey">service</span> <span style="color:grey">starts</span> on ASP.NET Core's <span style="color:grey">standard</span> start-up, `WebApplication.CreateBuilder`, and <span style="color:grey">adds</span> `tessera.json` from <span style="color:grey">beside</span> <span style="color:grey">itself</span>; the `seed` <span style="color:grey">command</span> does the <span style="color:grey">same</span> on a <span style="color:grey">plain</span> <span style="color:grey">host</span>. A <span style="color:grey">missing</span> <span style="color:grey">file</span> is no <span style="color:grey">fault</span>: every <span style="color:grey">key</span> <span style="color:grey">takes</span> its <span style="color:grey">default</span>.
- **Overrides.** <span style="color:grey">Any</span> <span style="color:grey">environment</span> variable <span style="color:grey">overrides</span> the <span style="color:grey">key</span> it <span style="color:grey">names</span>, with `__` for <span style="color:grey">each</span> <span style="color:grey">step</span> <span style="color:grey">down</span> a <span style="color:grey">level</span>, so `Tiles__CacheSeconds=0` <span style="color:grey">sets</span> `Tiles.CacheSeconds`. Variables that <span style="color:grey">name</span> no <span style="color:grey">setting</span>, such as `TESSERA_DB_PASSWORD`, <span style="color:grey">change</span> <span style="color:grey">nothing</span>.
- **Checked at start-up.** <span style="color:grey">Each</span> <span style="color:grey">section</span> <span style="color:grey">binds</span> to its <span style="color:grey">record</span> <span style="color:grey">through</span> .NET's <span style="color:grey">options</span> <span style="color:grey">pattern</span>. Attributes such as `[Range]` <span style="color:grey">give</span> <span style="color:grey">each</span> <span style="color:grey">value</span> its <span style="color:grey">limits</span>, a <span style="color:grey">record</span>'s <span style="color:grey">own</span> `Validate` <span style="color:grey">checks</span> <span style="color:grey">values</span> that must agree, such as a <span style="color:grey">box</span> that <span style="color:grey">runs</span> <span style="color:grey">west</span> to <span style="color:grey">east</span>, and the <span style="color:grey">check</span> <span style="color:grey">runs</span> <span style="color:grey">before</span> the <span style="color:grey">service</span> <span style="color:grey">starts</span>. Every <span style="color:grey">value</span> out of its <span style="color:grey">limits</span> is <span style="color:grey">named</span> at <span style="color:grey">once</span>, with its <span style="color:grey">record</span>; a <span style="color:grey">key</span> the <span style="color:grey">record</span> <span style="color:grey">lacks</span> <span style="color:grey">stops</span> the <span style="color:grey">start</span> on its <span style="color:grey">own</span> (US-00 c4). A <span style="color:grey">misspelt</span> <span style="color:grey">section</span> <span style="color:grey">name</span> <span style="color:grey">passes</span> unseen, and the <span style="color:grey">section</span> <span style="color:grey">keeps</span> its <span style="color:grey">defaults</span>.
- **No password in the file.** The <span style="color:grey">shipped</span> <span style="color:grey">file</span> <span style="color:grey">holds</span> no <span style="color:grey">password</span>, and <span style="color:grey">listens</span> on 127.0.0.1 <span style="color:grey">alone</span>; a <span style="color:grey">test</span> <span style="color:grey">reads</span> it and <span style="color:grey">fails</span> the <span style="color:grey">build</span> if it <span style="color:grey">breaks</span> either <span style="color:grey">rule</span>.

The <span style="color:grey">shipped</span> `tessera.json` for Macau:

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
| A hand-written reader, with pg_tileserv's flat key names | <span style="color:grey">Hundreds</span> of <span style="color:grey">lines</span> to <span style="color:grey">keep</span>, and <span style="color:grey">two</span> <span style="color:grey">forms</span> for <span style="color:grey">one</span> <span style="color:grey">time</span>; ASP.NET Core's configuration does the <span style="color:grey">same</span> <span style="color:grey">work</span> |
| A small loader of our own on ASP.NET Core's configuration | It would <span style="color:grey">read</span> <span style="color:grey">exactly</span> <span style="color:grey">two</span> <span style="color:grey">sources</span>, but in a <span style="color:grey">pattern</span> <span style="color:grey">only</span> this codebase <span style="color:grey">uses</span>; the <span style="color:grey">options</span> <span style="color:grey">pattern</span> is the <span style="color:grey">one</span> .NET's <span style="color:grey">own</span> <span style="color:grey">guides</span> <span style="color:grey">teach</span> |
| Environment variables alone | A <span style="color:grey">box</span> and a <span style="color:grey">home</span> <span style="color:grey">view</span> <span style="color:grey">read</span> <span style="color:grey">badly</span> as <span style="color:grey">flat</span> variables, and <span style="color:grey">nothing</span> <span style="color:grey">records</span> the <span style="color:grey">office</span>'s <span style="color:grey">values</span> in <span style="color:grey">one</span> <span style="color:grey">place</span> |
| .NET's default `appsettings.json` | A <span style="color:grey">generic</span> <span style="color:grey">name</span> that <span style="color:grey">says</span> <span style="color:grey">nothing</span> <span style="color:grey">about</span> the <span style="color:grey">service</span>, and <span style="color:grey">one</span> the <span style="color:grey">framework</span> <span style="color:grey">loads</span> with <span style="color:grey">rules</span> of its <span style="color:grey">own</span> |

## Consequences

- The <span style="color:grey">office</span>'s <span style="color:grey">settings</span> <span style="color:grey">live</span> in <span style="color:grey">one</span> <span style="color:grey">file</span> you can <span style="color:grey">read</span>, <span style="color:grey">copy</span> and <span style="color:grey">keep</span> in Git.
- The <span style="color:grey">messages</span> <span style="color:grey">come</span> from .NET, such as 'Tiles: The field CacheSeconds must be between 0 and 2147483647.', <span style="color:grey">each</span> with its <span style="color:grey">section</span> in <span style="color:grey">front</span>.

## When to revisit

- A <span style="color:grey">setting</span> <span style="color:grey">needs</span> to <span style="color:grey">change</span> <span style="color:grey">while</span> the <span style="color:grey">service</span> <span style="color:grey">runs</span>; the <span style="color:grey">service</span> then <span style="color:grey">needs</span> to <span style="color:grey">reload</span> the <span style="color:grey">file</span>.
