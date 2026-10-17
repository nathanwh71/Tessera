# Tessera

Tessera is a _small_ _web_ _service_ that _serves_ a _map_ of _places_ as ***tiles***. It _reads_ _tables_ from a _database_, _cuts_ them into _map_ _tiles_ on _request_, and _answers_ every _machine_ in the _office_ on _port_ 80, _through_ Nginx. This _build_, _Phase_ Oct, _serves_ the 900 _places_ of Macau in _nine_ ***layers***, and _shows_ them on a _map_ at `/`.

A **tile** is _one_ _square_ of a _map_ at _one_ ***zoom level***. A _web_ _map_ is _made_ of _thousands_ of them, and the _browser_ _asks_ for _only_ the _ones_ on the _screen_. Tessera _makes_ _each_ _tile_ from the _database_ the _first_ _time_ a _map_ _asks_ for it; Nginx _keeps_ it for a _minute_ and _hands_ the _same_ _copy_ to everyone who _asks_ in that _time_.

## What this build holds, story by story

The thirty _stories_ in `docs/user-stories/user-stories.md` _fix_ what Tessera must do. _Each_ _stands_ as _follows_:

| Story | State |
|---|---|
| US-00 Walking skeleton | _Built_ and _tested_ |
| US-01 One database | _Built_ and _tested_ |
| US-03 Publish by grant | _Built_ and _tested_ |
| US-24 Places in nine layers | _Built_ and _tested_ |
| US-07 Serve a tile | _Built_ and _tested_ |
| S-01 SQL injection | _Built_ and _tested_ |
| S-06 Malformed URLs | _Built_ and _tested_: Nginx _refuses_ an _address_ it _cannot_ _read_, and a disguised _one_ _matches_ no _route_; _each_ _answer_ is a ***problem document*** |
| US-35 Office on port 80 | _Script_ _written_ and _tested_: it _checks_ for WSL2's ***mirrored networking*** and _prints_ the firewall _steps_. It _needs_ a _first_ _run_ on the _office_ _machine_ |
| US-23 and UI-09 to UI-19, the map | _Built_ and _tested_: the _map_ in `web/` (ADR 0006), and its ***browser tests*** _pass_ _through_ Nginx |
| N-18 to N-20, tiles that cost less | _Built_ and _tested_: a _tile_ _carries_ `max-age` and a _weak_ `ETag`, and Nginx _keeps_ it, _answers_ 304, and _packs_ it with gzip (ADR 0008) |
| N-21 to N-28, steady when the database is not | _Built_ and _tested_: 503 with `Retry-After`, ***grants*** _seen_ _within_ `Catalogue.RefreshSeconds`, _one_ _query_ for a _crowd_, `/health`, and _queries_ _served_ in the _order_ they _arrive_. _While_ the _database_ is _down_, Nginx _serves_ every _tile_ it _holds_. The _service_ and the _database_ _start_ in _any_ _order_ |
| US-36 Clean places | _Built_ and _tested_: _eight_ ***filters***, ***stop rules*** and a _log_ for _each_ _run_ of `npm run etl`, in the Python ***pipeline*** in `etl/` (ADR 0009) |
| S-07, S-30 and N-29, Nginx guards the door | _Built_ and _tested_: Nginx _answers_ the _office_'s _network_ _alone_, _caps_ _each_ _machine_ at 100 _tiles_ a _second_ with a burst of 300 (ADR 0007), and _shares_ _requests_ _between_ _two_ _copies_ of the _service_, either of which can _carry_ them all (ADR 0008); a ***start-up task*** _starts_ them _again_ _after_ the _machine_ _restarts_, and _needs_ a _first_ _run_ on the _office_ _machine_ |

## What you need

- The .NET 10 SDK, 10.0.100 or _later_: the _tools_ that _build_ and _run_ C# 14
- Docker Engine, which _runs_ the _database_ and Nginx in _containers_: _sealed_ _boxes_ with their _own_ _files_
- Node.js 26, which _runs_ the _scripts_ and _builds_ the _map_ (ADR 0002)
- For `npm run etl`: uv, Python's _project_ _tool_, which _installs_ Python and the _pipeline_'s _packages_ for `etl/` on the _first_ _run_
- GDAL's `ogrinfo`, for `npm run inspect` _alone_; _without_ it, the _script_ _runs_ GDAL's Docker _image_

## The commands

| Command | What it does |
|---|---|
| `npm run build` | _Builds_ the _map_, then the _service_ with the _map_ _inside_ it (US-00 c1) |
| `npm test` | _Runs_ every _test_: the _scripts_', the _map_'s, the _service_'s _borders_ and its _two_ _suites_, the _pipeline_'s, and the _browser_ _tests_ _through_ Nginx (US-00 c3) |
| `dotnet build` | _Builds_ the _service_ and its _three_ _test_ _suites_ _alone_; _needs_ `web/dist` from a _map_ _build_ |
| `dotnet run --project tests/Tessera.ArchitectureTests` | _Checks_ the _borders_ _between_ the _service_'s _layers_ on the _built_ _program_: _eight_ _seconds_ (ADR 0010) |
| `dotnet run --project tests/Tessera.UnitTests` | _Runs_ the _unit_ and ***property tests***: no _database_, a _second_ or _two_ |
| `dotnet run --project tests/Tessera.IntegrationTests` | _Starts_ the _packaged_ _database_ in Docker and _tests_ _through_ it and _through_ _real_ HTTP (US-00 c3) |
| `npm install` | _Installs_ the _scripts_' _tools_: zx and TypeScript |
| `npm start` | _Starts_ the _database_, _runs_ _any_ _new_ ***seed script***, _starts_ _two_ _copies_ of the _service_ and Nginx in _front_ of them, then _waits_ for `/health` to _say_ `UP`; it _starts_ _again_ _any_ _copy_ that _stops_, until Ctrl+C (US-00 c2, N-25, N-29, ADR 0009) |
| `npm run inspect -- SOURCE [--split FIELD]` | _Describes_ a _source_'s _layers_, _rows_, _shapes_, _systems_ and _fields_, and _prints_ a _starting_ ***pipeline file***; `--split` _makes_ _one_ _table_ for _each_ _value_ of a _field_ (ADR 0009) |
| `npm run etl -- PIPELINE.json` | _Runs_ the Python _pipeline_: it _cleans_ a _file_ of _places_ into the _next_ _seed_ _script_ in `db/seed`, and logs _each_ _row_'s fate in `db/seed-logs` (US-24, ADR 0009) |
| `npm run publish` | _Grants_ the _nine_ _layers_ to the _service_, which _serves_ them _within_ `Catalogue.RefreshSeconds`, 30 by _default_ (US-24 c5, N-23) |
| `npm run office-port` | _Checks_ that WSL2 _uses_ _mirrored_ _networking_, and _prints_ the _steps_ that _open_ _port_ 80 to the _office_ (US-35) |
| `npm run boot-task` | _Prints_ the PowerShell that _makes_ _Windows_ _run_ `npm start` when the _office_ _machine_ _switches_ on, with its _output_ in `start.log` _beside_ `package.json` (N-29) |
| `npm run backup` | _Backs_ _up_ the _database_ to _one_ _file_ in `backups/`, or the _folder_ `TESSERA_BACKUP_FOLDER` _names_, _reads_ it _back_ to _prove_ it _whole_, and _keeps_ the _newest_ 14 |
| `npm run restore -- FILE` | _Puts_ a backup _back_, in _one_ _transaction_: every _table_, every _grant_ and DbUp's _record_ of the _seed_ _scripts_ |
| `npm run backup-task` | _Prints_ the PowerShell that _makes_ _Windows_ _run_ `npm run backup` _each_ _night_ at 02:00 |
| `npm run e2e` | _Runs_ the _map_'s _browser_ _tests_, then the Nginx _checks_, _against_ a _database_ and an Nginx of their _own_ |

_Copy_ `.env.example` to `.env` and _give_ _each_ _password_ a _value_ _before_ `npm start`. Git _ignores_ `.env`, so no _password_ _reaches_ the repository.

## Where the settings live

_Machines_, _ports_ and _passwords_ _come_ from ***environment variables***: _values_ the _operating_ _system_ _hands_ a _program_ when it _starts_. The _service_ _refuses_ to _start_ _without_ its _database_'s _address_ and _names_ every _problem_ it _finds_ (US-00 c4); it _starts_ _whether_ or not the _database_ _answers_ yet, and _answers_ 503 until it does (N-23). Every _other_ _setting_ _lives_ in `tessera.json`, in _sections_, with _each_ _time_'s _unit_ in its _name_: the _area_'s _name_, _box_ and _home_ _view_, the _map_ _preview_, _each_ _tile_'s _limits_ and `Tiles.CacheSeconds`, the _database_ _pool_'s _limits_, and how _often_ the _list_ of _layers_ is _reread_. _Any_ _environment_ variable _overrides_ the _key_ it _names_, with `__` for _each_ _step_ _down_, such as `Tiles__CacheSeconds=0` (ADR 0003).

| Variable | Read by | Holds |
|---|---|---|
| `TESSERA_HTTPPORT` | `npm start`, for Nginx | The _port_ the _office_ _reaches_: 80 in the _office_ |
| `TESSERA_OFFICE_NETWORK` | `npm start`, for Nginx | The _office_'s _network_, such as `192.168.10.0/24`; Nginx _answers_ it and this _machine_ _alone_ (S-07) |
| `TESSERA_SERVICE_PORT` | `npm start` | _Optional_: the _first_ _copy_'s _port_, 7800 by _default_; the _second_ _copy_ _takes_ the _next_ _port_ (N-29) |
| `DATABASE_URL` | The _service_ | `postgresql://tessera_reader:<password>@<host>:<port>/tessera` |
| `TESSERA_PUBLISHER_DATABASE_URL` | `seed` | `postgresql://tessera_publisher:<password>@<host>:<port>/tessera` |
| `TESSERA_DB_PASSWORD`, `TESSERA_READER_PASSWORD`, `TESSERA_PUBLISHER_PASSWORD` | `npm start`, which _writes_ the _two_ URLs _above_ from them | The _three_ ***roles***' _passwords_ |
| `TESSERA_DB_HOST`, `TESSERA_DB_PORT`, `TESSERA_DB_NAME` | `npm start`; `npm run publish` _reads_ `TESSERA_DB_NAME` _alone_ | _Optional_: where the _database_ _answers_, 127.0.0.1, 5432 and `tessera` by _default_ |

## Adding places

The _places_ _reach_ the _database_ as SQL _scripts_ in `db/seed`, which DbUp _runs_ _once_ _each_, in the _order_ of their _names_ (ADR 0009). A **pipeline file** _turns_ a _file_ of _places_ into the _next_ _script_, _cleaning_ it on the _way_: GDAL extracts _each_ _table_, filters _clean_ its _rows_, and the _pipeline_ _writes_ the SQL. To _add_ _places_:

1. Inspect the _data_, such as `npm run inspect -- data/parks.gdb`, and _save_ the _starting_ _pipeline_ _file_ it _prints_ _beside_ the _data_, such as `data/parks.pipeline.json`. Then _set_ _each_ _table_'s ***schema*** and _name_, and its filters. It _names_ the _source_, the _script_ to _write_, and _each_ _table_ with its filters. GDAL _reads_ _file_ ***geodatabases***, GeoJSON, shapefiles, GeoPackages, CSV and more, and _stores_ every _shape_ in longitude and latitude.
2. _Run_ it: `npm run etl -- data/parks.pipeline.json`. It _writes_ the _script_, such as `db/seed/0002-parks.sql`, and a _log_ of what _each_ filter did to _each_ _row_, in `db/seed-logs/`.
3. _Read_ the _log_ and the _script_, then _commit_ both. _Never_ _change_ a _script_'s SQL _once_ it has _run_: DbUp would not _run_ it _again_. A _comment_ may _change_, _since_ DbUp _knows_ a _script_ by its _name_ _alone_. _Put_ _new_ or _corrected_ _data_ in a _new_ _script_, with the _next_ _number_.
4. _Run_ `npm start`, or `Tessera seed` as `tessera_publisher`, then `npm run publish`. A _script_ that _replaces_ a _table_ _takes_ its _grant_ with it, so the _table_ _waits_ for the _publish_.

A _table_ of a geodatabase _names_ its ***feature class*** as `layer`; a _table_ _cut_ from _one_ _file_ _names_ its _rows_ with `where`, as `data/macau.pipeline.json` does for the _nine_ Macau _layers_. A _pipeline_ _file_ for a geodatabase, `data/parks.gdb`, _looks_ _like_ this:

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
| `dropEmptyShapes` | A _row_ with no _shape_, or an _empty_ _one_, _fails_ |
| `repairShapes` | An invalid _shape_, such as an _area_ whose _edge_ _crosses_ _itself_, is _repaired_; _one_ that _cannot_ be _repaired_ _fails_ |
| `swapCoordinates` | A _shape_ _outside_ the _area_ that _falls_ _inside_ it with longitude and latitude _swapped_ is _swapped_ _back_ |
| `insideArea` | A _shape_ that does not _touch_ the _area_'s _box_ _fails_ |
| `tidyText` | _Text_ is _put_ into _one_ Unicode _form_, _runs_ of _spaces_ _become_ _one_, _ends_ are trimmed, and _empty_ _text_ _becomes_ _null_; `fields` _limits_ it to _some_ _columns_ |
| `requireFields` | A _row_ _missing_ _any_ of `fields`, or _holding_ _only_ _spaces_ there, _fails_ |
| `dropDuplicateIds` | A _row_ that _repeats_ an earlier _row_'s `field` _fails_ |
| `dropNearDuplicates` | A _row_ whose `field` _matches_ an earlier _row_'s, _within_ `metres` of it, _fails_ |

Filters _run_ in the _order_ _listed_, _each_ on the _rows_ the _one_ _before_ _kept_. A _row_ that _fails_ is _dropped_, or, with `"onFail": "stop"`, _ends_ the _whole_ _run_. A _table_ _also_ _stops_ the _run_ when more than `droppedOverPercent` of its _rows_ were _dropped_, 10 by _default_, or _fewer_ than `rowsUnder` _remain_, 1 by _default_; `stopWhen` _sets_ them for the _whole_ _file_ or for _one_ _table_. A _stopped_ _run_ _writes_ no _script_, but _always_ _writes_ its _log_. The _area_ _comes_ from the _pipeline_'s _own_ `area`, or _else_ from `tessera.json`. `lines: true` _stores_ _lines_ and _areas_ as many-part _shapes_, so _one_ _table_ _holds_ both _kinds_.

**Adding a filter.** The _pipeline_ _lives_ in `etl/`, in Python. _Each_ filter is _one_ ***module*** in `etl/src/tessera_etl/core/filters/`. It _holds_ the filter's _name_, what it does, a _model_ of its _own_ _settings_, its _checks_, and a _function_ that _returns_ the filter as a _step_ of the _chain_. The filter _itself_ is a _judge_: a _function_ that _reads_ the _whole_ _table_ at _once_ and _gives_ a verdict, _fixed_ or _failed_, to _each_ _row_ it does not _pass_ as it is; a _row_ with no verdict _passes_. Then `filter_step`, in `core/steps.py`, does the _dropping_, _logging_ and _stopping_ for every filter alike. A _new_ filter is _one_ _new_ _module_, _modelled_ on `inside_area.py`, and _one_ _line_ in `KINDS`. _Each_ filter _reads_ _only_ its _own_ _settings_, so the _pipeline_ _file_ _refuses_ a _setting_ that _belongs_ to another filter, or _one_ that is _missing_.

**Filters in code.** Filters _join_ with `>>`, which _runs_ the _left_ _one_, then the _right_ _one_ on the _rows_ it _kept_, and _stops_ at the _first_ _stop_. _Each_ _step_ is a _function_ that _takes_ a _table_, its _log_ and _any_ _reason_ to _stop_, and _returns_ them _changed_ (`kernel/pipes.py`):

```python
clean_cafes = (
    drop_empty_shapes.create(OnFail.STOP)
    >> swap_coordinates.create(macau)
    >> inside_area.create(macau, OnFail.STOP)
    >> tidy_text.create()
    >> require_fields.create(["id", "name"]))
```

The _pipeline_ _file_ _builds_ the _same_ _chain_ from its _list_, and _adds_ the _stop_ _rules_ as its _last_ _step_. A filter may _also_ _add_ or _remove_ a _column_: the _script_ _takes_ its _columns_ from the _cleaned_ _rows_.

## How the database keeps the service safe

Tessera _follows_ pg_tileserv's _rule_: the _database_ _decides_ what the _map_ _shows_. The _service_ _signs_ in as `tessera_reader`, a _role_ that may _only_ _read_, and a _table_ _becomes_ a _layer_ _only_ when that _role_ is _granted_ it. The _seed_ _signs_ in as `tessera_publisher`, which _owns_ the _tables_ but _never_ _grants_ them on its _own_; _publishing_ is the _publisher_'s _own_ _act_.

_Two_ _attacks_ are _closed_ by _design_. Every _value_ from a _request_ _travels_ _beside_ the _query_ as a ***parameter***, _never_ _inside_ its _text_, and every _name_ _enters_ the _text_ as a `SqlName`, which _only_ `SqlName.Quote` and `SqlName.Qualified` can _make_, so it _always_ _arrives_ _quoted_; no _request_ can smuggle in a _command_ (***SQL injection***, S-01). Nginx _refuses_ an _address_ it _cannot_ _read_, and the _service_ _matches_ _routes_ on the _address_ as _sent_, so a disguised _address_ _reaches_ no _route_ (a _malformed_ URL, S-06).

## What is where

The _service_ is _one_ _program_, _one_ _project_ laid out as the modular-monolith _template_: `Common`, which every _module_ may _use_, and _one_ _module_, `Modules/Tessera`, in _three_ _layers_. _Six_ ***architecture tests*** _keep_ _each_ _layer_ to its _own_ _work_ (ADR 0010). It _borrows_ what _libraries_ do _well_: ASP.NET Core _reads_ its _settings_, _writes_ its _problem_ _documents_, _tags_ _each_ _tile_, _answers_ 304 and `/health`; Dapper and Npgsql _talk_ to the _database_; DbUp _runs_ the _seed_ _scripts_ (ADR 0009); Nginx _keeps_, _packs_ and _guards_ (ADR 0008). CSharpFunctionalExtensions _gives_ `Result`; a _missing_ _value_ is a `T?`, which the _compiler_ _checks_ (ADR 0004).

| Folder | What it holds |
|---|---|
| `src/Tessera/Common` | `Api/Problems.cs`, the _shape_ of every _problem_ _document_; `Infrastructure/Settings.cs`, how a _section_ of `tessera.json` is _read_ and _checked_ |
| `src/Tessera/Modules/Tessera/Domain` | _Records_ and ***pure functions***: a _tile_'s _address_, a _quoted_ SQL _name_, the _layers_ and their ***catalogue***, the _area_ and _tile_ _settings_ |
| `src/Tessera/Modules/Tessera/Infrastructure` | Every SQL _text_, in `Sql/Queries.cs`, and the _code_ that _talks_ to the _database_; the _list_ of _layers_, _reread_ every `Catalogue.RefreshSeconds`; the ***health checks***; the `seed` _command_'s DbUp _run_, in `Seeding/` |
| `src/Tessera/Modules/Tessera/Features` | The _routes_: the _tiles_, the _map_'s _page_ with the _area_ _written_ in, and `/health` |
| `src/Tessera/Modules/Tessera/TesseraModule.cs` | The _module_'s _door_: `AddTessera` and `MapTessera` |
| `src/Tessera` | `Program.cs`, the _two_ _commands_; `TesseraHost.cs`, the _service_ _put_ _together_, the _template_'s _Host_; `.editorconfig`, which _makes_ _each_ namespace _match_ its _folder_ |
| `nginx/` | Nginx's _image_, in `Dockerfile`, which _names_ its _release_, and its _settings_, in `default.conf.template`; `npm start` and the _browser_ _tests_ _build_ and _run_ both (ADR 0008) |
| `web/` | The _map_: TypeScript, React, MapLibre GL JS, react-elmish, Valibot, ts-pattern and Tailwind CSS (ADR 0006) |
| `etl/` | The _pipeline_ that _cleans_ a _file_ of _places_ into a _seed_ _script_, in Python; the _service_ _never_ _runs_ it (ADR 0009, `etl/README.md`) |
| `tests/` | The _architecture_ _tests_, and the _unit_, _property_ and ***integration tests*** of the _service_ |
| `db/` | The _packaged_ _database_: PostgreSQL 18 with PostGIS 3.6, the _role_ _script_, the _seed_ _scripts_ in `db/seed`, and, _beside_ _each_ _script_ `npm run etl` _writes_, its _log_ in `db/seed-logs` |
| `data/` | The _seed_'s _source_: 900 _places_ in longitude and latitude, and `macau.pipeline.json`, which _cleans_ them into a _seed_ _script_ |
| `scripts/` | The _scripts_, in TypeScript with zx (ADR 0002): `start.ts`, `boot.ts` and `boot-task.ts`, `publish.ts`, `inspect.ts`, `etl.ts`, which _hands_ a _pipeline_ _file_ to the Python _pipeline_, `office-port.ts`, the _test_ and _build_ _runners_, and the Nginx _checks_ in `scripts/lib/edge.ts` |
| `docs/` | The _stories_ and the ***architecture decision records*** |

## The documents

- `docs/user-stories/user-stories.md`: the thirty _stories_ of _Phase_ Oct, in _build_ _order_
- `docs/adr/0001-modular-monolith.md`: why the _service_ is _one_ _program_ with _modules_
- `docs/adr/0002-scripts-in-typescript-with-zx.md`: why the _scripts_ are TypeScript on Node.js 26
- `docs/adr/0003-settings-file.md`: why _settings_ _live_ in `tessera.json` and the _environment_, and every _key_
- `docs/adr/0004-functional-style.md`: why the _code_ is _functional_, with `Result` and `T?`
- `docs/adr/0005-cluster-in-the-browser.md`: why the _map_ ***clusters*** the _places_ _itself_, from the _tiles_ it has _loaded_
- `docs/adr/0006-the-map-and-the-service.md`: why the _map_ _lives_ in `web/`, and the _contract_ _between_ the _map_ and the _service_
- `docs/adr/0007-limits-in-nginx.md`: why Nginx _answers_ the _office_ _alone_ and _caps_ _each_ _machine_, and why the _service_ _keeps_ no _resilience_ _library_
- `docs/adr/0008-nginx-in-front.md`: why Nginx _stands_ in _front_, and what it does in _place_ of the _service_
- `docs/adr/0009-seed-with-dbup.md`: why the _places_ _arrive_ as SQL _scripts_ that DbUp _runs_ _once_
- `docs/adr/0010-template-layout-guarded-by-tests.md`: why the _service_ _follows_ the modular-monolith _template_, with _architecture_ _tests_ at its _borders_
- `docs/design/etl-pipeline.md`: how the ETL _pipeline_ _cleans_ _places_ into _seed_ _scripts_, as a _chain_ of _steps_ _joined_ with `>>`
- `docs/design/template-mapping.md`: how Tessera _maps_ onto the modular-monolith _template_, and which _jobs_ ASP.NET Core does
- `docs/guides/delivery-plan.md`: how to _build_ _Parts_ A and B in _four_ iterations, from 19 _October_ to the _release_ of 7 December 2026, and what _follows_
- `docs/guides/reading-the-code.md`: _fifteen_ _steps_ _through_ the backend and the _map_, in the _order_ a _tile_ _travels_
- `docs/guides/reading-the-frontend.md`: _fifteen_ _steps_ _through_ the _map_ _alone_, from the _page_ that _starts_ to the _tests_ that _state_ its _rules_
- `docs/guides/reading-the-scripts.md`: _eleven_ _steps_ _through_ the TypeScript _scripts_, from the _rules_ they _keep_ to the _tests_ that _check_ Nginx
- `docs/guides/reading-the-harder-files.md`: the _one_ _idea_ _behind_ _each_ of `Settings.cs`, `Database.cs` and `LiveCatalogue.cs`, for _readers_ _new_ to ASP.NET Core


## Comments in the code

Every _comment_ is _written_ for a _school_ _student_ of _computer_ _science_. It _says_ why the _code_ is as it is: the _reason_, the _rule_, the _limit_ and the _story_ _behind_ it, such as `US-24 c6`. A _type_ or _method_ _carries_ _one_ _short_ _comment_; the _long_ _story_ _lives_ in `docs/`.
