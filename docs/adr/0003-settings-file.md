# ADR 0003: Read the service's settings from tessera.json

| | |
|---|---|
| Status | _Accepted_ |
| Date | 13 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | The _service_'s _settings_, in `nathanwh71/Tessera`: the helpers in `src/Tessera/Common/Infrastructure/Settings.cs`, and _each_ _section_'s _record_ _beside_ the _code_ that _reads_ it |
| Related | ADR 0001, which _names_ the _database_'s ***environment variables***; ADR 0008; US-00, US-07, US-23; N-18 to N-25 |

## Decision

The _service_ _reads_ its _settings_ from _two_ _sources_. A JSON _file_, `tessera.json`, _holds_ the _settings_ that _describe_ the _service_, _grouped_ in _sections_, _each_ _time_ with its _unit_ in its _name_. _Environment_ variables _hold_ _machines_, _ports_ and _passwords_, and may _override_ _any_ _key_ in the _file_. ASP.NET Core's configuration _reads_ both.

## What goes where

| Setting | Source | Default |
|---|---|---|
| `Area`: `Name`, `MinLongitude`, `MinLatitude`, `MaxLongitude`, `MaxLatitude`, `HomeLongitude`, `HomeLatitude`, `HomeLevel` | `tessera.json` | Macau |
| `Map.ShowPreview` | `tessera.json` | _true_ (US-23 c5) |
| `Tiles`: `Resolution`, `Buffer`, `MaxFeatures`, `MinZoom`, `MaxZoom` | `tessera.json` | 4096, 256, 10000, 0 and 22 |
| `Tiles.CacheSeconds`: how long a browser and Nginx keep a tile | `tessera.json` | 60 (N-18) |
| `Database`: `PoolSize`, `ConnectionLifetimeSeconds`, `QueryTimeoutSeconds`, `ConnectionWaitSeconds` | `tessera.json` | 4, 3600, 10 and 3 (N-22) |
| `Catalogue.RefreshSeconds` | `tessera.json` | 30; 0 _reads_ the _list_ _once_ (N-23) |
| `Logging.LogLevel`: Microsoft's own lines at Warning, its start-up lines and Tessera's at Information | `tessera.json` | As _shipped_; with no _rules_, .NET _writes_ _four_ _lines_ for every _request_ |
| `Urls`, where the service listens for Nginx | `tessera.json`; `npm start` _sets_ `Urls` from `TESSERA_SERVICE_PORT` | `http://127.0.0.1:7800` |
| `DATABASE_URL`, `TESSERA_PUBLISHER_DATABASE_URL` | _Environment_ | _None_: a _password_ _never_ _sits_ in a _file_ |
| `TESSERA_OFFICE_NETWORK`, for Nginx | _Environment_, _read_ by `npm start` | _None_: `npm start` _refuses_ to _start_ _without_ it (S-07, ADR 0007) |

The _room_ for _kept_ ***tiles*** _belongs_ to Nginx: `max_size` in `nginx/default.conf.template`, 64 MB (ADR 0008).

## How it works

- **One unit, one form.** A _key_ that _holds_ a _time_ _names_ its _unit_, such as `QueryTimeoutSeconds`, and _takes_ a _whole_ _number_. No _key_ _takes_ _two_ _forms_, such as _seconds_ or `"1h"`.
- **The file.** The _service_ _starts_ on ASP.NET Core's _standard_ start-up, `WebApplication.CreateBuilder`, and _adds_ `tessera.json` from _beside_ _itself_. The `seed` _command_ _reads_ the _same_ _file_ and the _environment_, and _takes_ from them the _publisher_'s _database_ URL _alone_, so a _fault_ in another _section_ _never_ _stops_ it. A _missing_ _file_ is no _fault_: every _key_ _takes_ its _default_.
- **Overrides.** _Any_ _environment_ variable _overrides_ the _key_ it _names_, with `__` for _each_ _step_ _down_ a _level_, so `Tiles__CacheSeconds=0` _sets_ `Tiles.CacheSeconds`. Variables that _name_ no _setting_, such as `TESSERA_DB_PASSWORD`, _change_ _nothing_.
- **Checked at start-up.** _Each_ _section_ _binds_ to its _record_ _through_ .NET's _options_ _pattern_. Attributes such as `[Range]` _give_ _each_ _value_ its _limits_, a _record_'s _own_ `Validate` _checks_ _values_ that must agree, such as a _box_ that _runs_ _west_ to _east_, and the _check_ _runs_ _before_ the _service_ _starts_. _One_ _failed_ _start_ _names_ every _problem_ at _once_ (US-00 c4, N-21 c4): _each_ _value_ out of its _limits_, with its _record_; _each_ _key_ the _record_ _lacks_, _most_ _often_ a misspelling, with its _section_; _each_ _value_ of the _wrong_ _kind_, such as a _word_ where a _number_ _belongs_; and a _missing_ or _malformed_ _database_ URL. The _program_ _prints_ _one_ _problem_ to a _line_ and _stops_ with exit _code_ 1. A _misspelt_ _section_ _name_ _passes_ unseen, and the _section_ _keeps_ its _defaults_.
- **No password in the file.** The _shipped_ _file_ _holds_ no _password_, and _listens_ on 127.0.0.1 _alone_; a _test_ _reads_ it and _fails_ the _build_ if it _breaks_ either _rule_.

The _shipped_ `tessera.json` for Macau:

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
| A hand-written reader, with pg_tileserv's flat key names | _Hundreds_ of _lines_ to _keep_, and _two_ _forms_ for _one_ _time_; ASP.NET Core's configuration does the _same_ _work_ |
| A small loader of our own on ASP.NET Core's configuration | It would _read_ _exactly_ _two_ _sources_, but in a _pattern_ _only_ this codebase _uses_; the _options_ _pattern_ is the _one_ .NET's _own_ _guides_ _teach_ |
| Environment variables alone | A _box_ and a _home_ _view_ _read_ _badly_ as _flat_ variables, and _nothing_ _records_ the _office_'s _values_ in _one_ _place_ |
| .NET's default `appsettings.json` | A _generic_ _name_ that _says_ _nothing_ _about_ the _service_, and _one_ the _framework_ _loads_ with _rules_ of its _own_ |

## Consequences

- The _office_'s _settings_ _live_ in _one_ _file_ you can _read_, _copy_ and _keep_ in Git.
- The _messages_ for a _value_ out of its _limits_ _come_ from .NET and _name_ the _record_, not the _section_; the _program_'s _own_ _messages_, for a _key_ or a _value_ it _cannot_ _read_, _name_ the _section_. `Tiles__CacheSecond=5 Tiles__CacheSeconds=-1 Tiles__MaxZoom=soon`, with no `DATABASE_URL`, _prints_:

```text
DataAnnotation validation failed for 'TileSettings' members: 'CacheSeconds' with the error: 'The field CacheSeconds must be between 0 and 2147483647.'.
Tiles: 'CacheSecond' is not a setting; Tiles holds Resolution, Buffer, MaxFeatures, MinZoom, MaxZoom, CacheSeconds.
Tiles: MaxZoom must be a whole number, not 'soon'.
DataAnnotation validation failed for 'DatabaseConnection' members: '' with the error: 'DATABASE_URL: missing or empty'.
```

## When to revisit

- A _setting_ _needs_ to _change_ _while_ the _service_ _runs_; the _service_ then _needs_ to _reload_ the _file_.
