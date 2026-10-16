# Reading the code, step by step

**The short answer.** _Read_ the _code_ in the _order_ a _request_ _travels_. _Part_ A _follows_ _one_ ***tile*** from the _office_'s _browser_, _through_ Nginx and the _service_, to the _database_ and _back_. _Part_ B _follows_ the _map_'s _page_ as it _opens_ in the _browser_ and _draws_ that _tile_. _Each_ _step_ _names_ the _files_ to _open_, what to _look_ for, and _one_ _question_ to _check_ yourself. _Fifteen_ _steps_ _take_ an _afternoon_; the backend is _about_ 1,500 _lines_ of C#, and the _map_ _about_ 3,000 _lines_ of TypeScript, _beside_ their _tests_.

**Before you start.** _Run_ `npm test` _once_, so you _know_ every _part_ _works_, then _keep_ _two_ terminals _open_: _one_ for `npm start`, _one_ for `curl`. _Seeing_ an _answer_ _arrive_ _teaches_ _faster_ than _reading_ _about_ it.

## Part A · The backend: one tile, from request to bytes

### Step 1 · How the program starts

- **Open:** `src/Tessera/Program.cs`.
- **Look for:** the `switch` on the _arguments_. No _argument_ _builds_ the _service_ with `TesseraHost.Build` and _serves_ _tiles_; `seed` _reads_ _one_ _setting_, the _publisher_'s _database_ URL, _loads_ the _places_ and _stops_. Every _failure_ _ends_ with a _list_ of _problems_ and exit _code_ 1, or 2 for a _command_ it does not _understand_.
- **Check yourself:** what does `Tessera seed` _sign_ in as, and why not as the _reader_?

### Step 2 · How the service is put together

- **Open:** `src/Tessera/TesseraHost.cs`, then `src/Tessera/Modules/Tessera/TesseraModule.cs`, `src/Tessera/Tessera.csproj`, `src/Tessera/.editorconfig` and `tests/Tessera.ArchitectureTests/BoundaryTests.cs`.
- **Look for:** _two_ _halves_. _Before_ `builder.Build()`, `AddTessera`, the ***module***'s _door_, _fills_ the _table_ of _services_: the _settings_, the _database_ _pool_, the _list_ of ***layers***, the _two_ ***health checks***. _After_ it, the _code_ _lays_ the ***pipeline*** every _request_ _passes_ _through_, in _order_: the _error_ _handlers_, _routing_, then `MapTessera` _adds_ `/health`, the _tiles_ and the _map_. Then the _borders_: _one_ _project_ _holds_ `Common/` and `Modules/Tessera/`, and _each_ _folder_ is its namespace. _Rule_ IDE0130 in `.editorconfig` _fails_ the _build_ when a _file_'s namespace strays from its _folder_, and _six_ ArchUnitNET _rules_ in `BoundaryTests.cs` _keep_ the _layers_ _apart_, so the Domain _never_ _touches_ the _database_ and _only_ the Infrastructure does (ADR 0010).
- **Check yourself:** what does `/health` _answer_ _while_ the _list_ of _layers_ is _still_ _loading_?

### Step 3 · Where the settings come from

- **Open:** `tessera.json`, then `src/Tessera/Common/Infrastructure/Settings.cs` and the `Settings.cs` in _each_ of `Domain/`, `Infrastructure/` and `Features/` _under_ `src/Tessera/Modules/Tessera/`, with `docs/guides/reading-the-harder-files.md` _beside_ you.
- **Look for:** _one_ _record_ for _each_ _section_ of `tessera.json`, _kept_ _beside_ the _code_ that _reads_ it, its _defaults_ in the _record_, its _limits_ in `[Range]`; `TesseraModule.AddSettings`, which _binds_ the _five_ _sections_; and `Check`, which _names_ every _problem_ at _once_.
- **Check yourself:** _set_ `Tiles__CacheSeconds=-1` and _run_ the _program_. What does it _print_?

### Step 4 · What Nginx does before the service sees a request

- **Open:** `nginx/default.conf.template`, and ADR 0008.
- **Look for:** _first_ the `geo` _block_ and the `if` that _refuses_ a _machine_ _outside_ the _office_, and the _limits_ for _each_ _visitor_ (ADR 0007); then the `upstream` _group_ of _two_ _copies_; then the `location` for _tiles_. Nginx _keeps_ _each_ _tile_, _answers_ 304, _packs_ with gzip, _refuses_ a _malformed_ _address_, _passes_ a _request_ to the _other_ _copy_ when _one_ _fails_, and _serves_ _kept_ _tiles_ _while_ both are _down_. The _service_ does _none_ of these _jobs_ but _one_: it _too_ _answers_ 304 when a _matching_ _tag_ _reaches_ it (ADR 0008).
- **Check yourself:** _ask_ for the _same_ _tile_ _twice_ with `curl -I`. What does `X-Cache-Status` _say_ _each_ _time_?

### Step 5 · The tile route

- **Open:** `src/Tessera/Modules/Tessera/Features/Tiles.cs`, then `src/Tessera/Modules/Tessera/Domain/Tiles.cs`.
- **Look for:** the _path_ a _request_ _takes_. `MapTiles` _matches_ `/{layer}/{z}/{x}/{y}.pbf`; `TileAddress.Of` _refuses_ a _tile_ _outside_ the _grid_; `TileServer.Answer` _finds_ the _layer_, _builds_ the _query_ with `Queries.Tile`, _runs_ it, and _returns_ the _bytes_ with `Cache-Control`. The _list_ of _layers_, `Find` and `TileAddress.Of` _each_ _return_ a `Maybe`, a _value_ that may be _missing_, and `TryGetValue` _opens_ _each_ _one_ with an _early_ _return_. `TypedResults.Bytes` _writes_ the `ETag`, and _answers_ 304 _itself_ when the _browser_'s _copy_ _matches_. _Each_ _failure_ _becomes_ a ***problem document*** from `src/Tessera/Common/Api/Problems.cs`: 404 for no such _layer_ or _tile_, 503 _while_ the _list_ of _layers_ _loads_, _while_ the _database_ _cannot_ _answer_, or when a ***seed script*** has _dropped_ a _column_ the _list_ _still_ _names_. That _last_ 503 _lasts_ until the _list_'s _next_ refresh.
- **Check yourself:** why does a _revoked_ ***grant*** _answer_ 404 _rather_ than 500?

### Step 6 · Where the list of layers comes from

- **Open:** `src/Tessera/Modules/Tessera/Domain/Layers.cs`, then `Catalogue.cs` and `LiveCatalogue.cs` in `src/Tessera/Modules/Tessera/Infrastructure/`, and US-03 and N-23 in the _stories_.
- **Look for:** `LayerCatalogue`, _one_ _list_ at _one_ _moment_; `CatalogueReader.Load`, which _builds_ _one_ from the _database_'s _rows_; and `LiveCatalogue`, which _rereads_ it every `Catalogue.RefreshSeconds`. A _table_ _becomes_ a _layer_ _only_ when the _reader_ ***role*** may _read_ it, so a _grant_ _publishes_ and a _revoke_ withdraws.
- **Check yourself:** the _database_ blinks _during_ a refresh. Does the _map_ _lose_ its _layers_?

### Step 7 · Every SQL text, and the one door to PostgreSQL

- **Open:** `src/Tessera/Modules/Tessera/Infrastructure/Sql/Queries.cs`, then `Database.cs` _beside_ it.
- **Look for:** in `Queries.cs`, the ***tile query***: `ST_TileEnvelope` _makes_ the _tile_'s _square_, `ST_AsMVTGeom` _fits_ _each_ _place_ into it, `ST_AsMVT` _packs_ the _tile_. Every _value_ _travels_ as a ***parameter*** such as `@z`; every _name_ _goes_ _through_ `Quote` (S-01). In `Database.cs`, `Run`, which _borrows_ a _connection_ from the _pool_ and _turns_ _each_ _failure_ into a `Result`.
- **Check yourself:** `tests/Tessera.UnitTests/approved/tile-query.sql` _holds_ the _approved_ _text_. Why does a _test_ _compare_ the _query_ with it?

### Step 8 · How the page reaches the browser

- **Open:** `src/Tessera/Modules/Tessera/Features/Map.cs`.
- **Look for:** `/` and `/{layer}.html` both _answer_ the _same_ _page_, `web/dist/index.html`, with the _area_ _written_ in as a _block_ of JSON by `Render`. Both _routes_ _share_ _one_ _group_, and `PageHeaders`, an ***endpoint filter*** on that _group_, _adds_ the _page_'s _headers_, such as its _Content_ _Security_ _Policy_. The _page_'s _files_ _under_ `/assets` are _kept_ for a _year_, _since_ _each_ _name_ _carries_ a hash of its _content_.
- **Check yourself:** `curl` the _page_ and _find_ the `tessera-area` _block_. Where does its _box_ _come_ from?

### Step 9 · How the places enter the database

- **Open:** `data/macau.pipeline.json`, then `tools/Tessera.Etl/Program.cs`, `Pipes.cs`, `Steps.cs` and _one_ ***filter*** in `Filters/`, such as `InsideArea.cs`, `db/seed/0001-macau-places.sql` (the _first_ _lines_ _alone_), `src/Tessera/Modules/Tessera/Infrastructure/Seeding/SeedScripts.cs`, and ADR 0009.
- **Look for:** GDAL extracts _each_ _table_ to GeoJSON, the ***pipeline file***'s filters, _joined_ into _one_ _chain_ of _handlers_ with `>>` and _ended_ by the ***stop rules***, _clean_ its _rows_ in _turn_, and GDAL _writes_ the _cleaned_ _rows_ as SQL; DbUp _runs_ _each_ _new_ _script_ _once_ and _records_ it in `tessera.schemaversions`; `npm run publish` _grants_ the _tables_.
- **Check yourself:** you _find_ a _wrong_ _address_ in a _script_ that has _run_. What do you _change_?

## Part B · The frontend: the page opens and draws a tile

### Step 10 · The first code the browser runs

- **Open:** `web/index.html`, then `web/src/main.tsx` and `web/src/app/mount.tsx`.
- **Look for:** `main.tsx` _reads_ the _area_ from the `tessera-area` _block_, _works_ out which _layer_ the _address_ _names_, and mounts `App`. `mount.tsx` _swaps_ in a _short_ _message_ when an _error_ escapes the _whole_ _page_ (US-23 c9).
- **Check yourself:** _open_ the _page_ _without_ the _service_, _under_ `npm run dev` in `web/`. Which _area_ does it _show_, and why?

### Step 11 · The heart of the map: one model, one update

- **Open:** `web/src/map/map.ts`, then `web/src/app/App.tsx`.
- **Look for:** the Elmish _pattern_. `Model` _holds_ _everything_ the _page_ _shows_; `Msg` _lists_ _everything_ that can _happen_; `update` _makes_ the _next_ _model_ from the _old_ _one_ and a _message_, and _changes_ _nothing_ _else_ (ADR 0004). _Work_ _outside_, such as _moving_ the _map_, _goes_ out _through_ `Ports`. `App.tsx` _runs_ the loop with `useElmish` and _lays_ out the _parts_.
- **Check yourself:** _find_ the _message_ a _click_ on 'zoom in' _sends_, and _follow_ it _through_ `update`. What _leaves_ _through_ the _ports_?

### Step 12 · The nine layers and the map's style

- **Open:** `web/src/layers/layers.ts`, then `web/src/map/style.ts`.
- **Look for:** the _nine_ _layers_ _live_ in the _map_'s _own_ _code_; _each_ _tile_'s _address_ is `/{layer}/{z}/{x}/{y}.pbf` on the _page_'s _own_ _origin_. `buildStyle` is a ***pure function*** that _turns_ the _model_ into MapLibre's _style_: a _plain_ _background_, then the _names_, the _pins_ and the ***clusters***.
- **Check yourself:** where would you _add_ a tenth _layer_, and what _else_ would _need_ it?

### Step 13 · Where the map meets MapLibre

- **Open:** `web/src/map/MapCanvas.tsx`, `web/src/map/canvas-hooks.ts`, `web/src/map/maplibre.ts`, then `web/src/map/places.ts`.
- **Look for:** the _shell_ _around_ the _pure_ _core_. `canvas-hooks.ts` _holds_ _four_ _named_ hooks: _one_ _makes_ the _map_ and wires its _events_, _one_ _keeps_ the _style_ in _step_, _one_ _runs_ the _pins_' _drop_, _one_ _keeps_ the _newest_ props. `maplibre.ts` _holds_ MapLibre's _options_ and _events_. `places.ts` _joins_ the _places_ of every _loaded_ _tile_ into _one_ _list_, which MapLibre clusters in the _browser_ (ADR 0005).
- **Check yourself:** a _place_ _near_ a _tile_'s _edge_ _arrives_ in _two_ _tiles_. Where is it _kept_ _once_?

### Step 14 · The shared kernel and the design

- **Open:** `web/src/shared/result.ts`, `web/src/shared/area.ts`, then `web/src/design/theme.ts`.
- **Look for:** `Result`, the _same_ _idea_ as the backend's, with F#'s _function_ _names_. `areaOf` _reads_ the _area_ _through_ Valibot, which _checks_ _data_ _against_ a _description_ of its _shape_, and _falls_ _back_ to Macau. `design/` _holds_ the _colours_, _shapes_ and motion of Material 3.
- **Check yourself:** what does `areaOf` _return_ for a _block_ of JSON with the _box_ backwards?

### Step 15 · The tests as a map of the rules

- **Open:** `web/src/architecture.test.ts`, `tests/Tessera.ArchitectureTests/BoundaryTests.cs`, `web/e2e/map.spec.ts`, then `tests/Tessera.IntegrationTests/ServiceTests.cs`.
- **Look for:** the _map_'s ***architecture test*** _keeps_ _each_ _module_'s _borders_ and _keeps_ _changing_ _state_ out of the _pure_ _files_; the _service_'s _six_ ***boundary rules*** _keep_ its _layers_ _apart_ in the _same_ _way_. The ***browser tests*** _drive_ a _real_ Chrome _through_ Nginx. The _service_ _tests_ _ask_ _real_ HTTP _questions_ of a _real_ _database_. _Each_ _test_ _names_ its _story_, such as US-23 c2, so a _test_ _leads_ you _back_ to the _rule_ it _proves_.
- **Check yourself:** _pick_ _one_ _story_ in `docs/user-stories/user-stories.md` and _find_ every _test_ that _names_ it.

## What to read alongside

- `docs/guides/reading-the-frontend.md`: _fifteen_ _steps_ _through_ the _map_ _alone_, in more depth than _Part_ B _above_.
- `docs/guides/reading-the-scripts.md`: _eleven_ _steps_ _through_ the _scripts_ that _start_, _test_ and _set_ _up_ Tessera.
- `docs/guides/reading-the-harder-files.md`: the _one_ _idea_ _behind_ `Settings.cs`, `Database.cs` and `LiveCatalogue.cs`.
- ADR 0001 for the _modules_, ADR 0010 for the _folders_ and the _rules_ that _guard_ them, ADR 0004 for the ***functional style***, ADR 0005 for clustering in the _browser_, ADR 0006 for the _contract_ _between_ the _map_ and the _service_, ADR 0008 for Nginx, and ADR 0009 for the _seed_.
- The _user_ _stories_: _each_ _step_ _above_ _names_ the _ones_ it _serves_.
