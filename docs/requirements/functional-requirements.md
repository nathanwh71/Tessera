# Functional requirements

Tessera must _meet_ 58 _functional_ _requirements_: what the _service_, the _map_, the ***pipeline*** and the _scripts_ do when _someone_ _uses_ them. A _requirement_ is _one_ _sentence_ that _says_ what the _system_ must do, _written_ so that a _test_ can _pass_ or _fail_ it. _Each_ _comes_ from an acceptance _criterion_ of a ***user story*** in `docs/user-stories/user-stories.md`. _Two_ _other_ _files_ _hold_ the _rest_. `non-functional-requirements.md` _says_ how _well_ Tessera must do these _things_: how _fast_, how steadily, and how easily every _visitor_ can _use_ the _map_. `security-requirements.md` _says_ what Tessera must _refuse_, and to whom.

**How to read them.** 'Must' _marks_ a _requirement_. _Source_ _names_ the _story_ and the _criterion_ the _requirement_ _comes_ from: 'US-07 c4' is _criterion_ 4 of _story_ US-07. _Checked_ by _names_ the _test_ that _proves_ it: a C# _test_ _class_, such as `ServiceTests`; a _test_ _file_ in TypeScript or Python, such as `web/e2e/map.spec.ts`; or a _script_ that _checks_ the _running_ _system_, such as `scripts/lib/edge.ts`. 'In part' _means_ the _test_ _proves_ _some_ of the _sentence_, and the _note_ _says_ which _part_ it _leaves_ out. 'No test yet' _means_ no _test_ _proves_ it.

## 1. Build, run and test

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| FR-01 | The _build_ _command_ must _build_ the _service_ from a _fresh_ _copy_ of the repository. | US-00 c1 | `.github/workflows/build.yml`, which _builds_ a _fresh_ _clone_ on _each_ _push_ |
| FR-02 | The _run_ _command_ must _start_ the _service_, and the _service_ must _answer_ an HTTP _request_ on its _port_. | US-00 c2 | `ServiceTests`, which _starts_ `Tessera.dll` and _asks_ it for a ***tile*** |
| FR-03 | The _test_ _command_ must _run_ every _test_, and every _test_ must _pass_. | US-00 c3 | `scripts/test.ts` _runs_ every _suite_; `.github/workflows/build.yml` _runs_ them all, and _since_ 18 _October_ 2026 _compares_ the _approved_ _images_ _inside_ Playwright's _own_ Docker _image_, _though_ no GitHub _run_ has yet _proved_ that _step_ |
| FR-04 | The _package_ must _carry_ the ***seed script*** `db/seed/0001-macau-places.sql`, _made_ from `data/macau-pois.geojson`. | US-01 c2 | `DatabaseTests`, which _checks_ the _source_'s SHA-256 fingerprint and the _script_'s _first_ _line_ |
| FR-05 | The ***integration tests*** must _start_ the _same_ _packaged_ _database_, _run_ the _same_ ***role*** _script_ and _read_ the _same_ _seed_ as the _service_. | US-01 c3 | `DatabaseFixture`, `DatabaseTests` |

## 2. Publishing a table as a layer

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| FR-06 | A _granted_ _table_ with a _typed_ geometry _column_ and an ***SRID*** _above_ 0 must _become_ a ***layer*** that _carries_ its SRID and geometry _type_. | US-03 c1 | `CatalogueTests` (_integration_) |
| FR-07 | A _granted_ _view_, or a _granted_ _view_ that _stores_ its _rows_, must _become_ a _layer_. | US-03 c2 | `CatalogueTests` (_integration_) |
| FR-08 | A _table_ whose _name_ _holds_ a _space_ and a _double_ _quote_ must _become_ a _layer_, and the _service_ must _serve_ its _tiles_. | US-03 c4 | `CatalogueTests` (_integration_) |
| FR-09 | A _granted_ _function_ must _stay_ out of the ***catalogue***, _since_ _Phase_ Oct _publishes_ _tables_ and _views_ _alone_. | US-03 c5 | `CatalogueTests` (_integration_ and _unit_) |

## 3. Turning files of places into seed scripts

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| FR-10 | `npm run etl` must _write_ _one_ SQL _script_ that _makes_ _each_ _table_ the ***pipeline file*** _lists_, with a _numbered_ `fid` as its _primary_ _key_, _one_ _column_ for _each_ _property_, a ***spatial index*** and _fresh_ _statistics_. | US-24 c1 | `etl/tests/test_table_sql.py`, `etl/tests/test_golden.py` |
| FR-11 | The _script_ must _store_ the _shapes_ of a _file_ in another coordinate _system_, such as EPSG:3857, in longitude and latitude, SRID 4326. | US-24 c2 | `etl/tests/test_pipeline.py`; `etl/tests/test_run.py`, whose `test_stores_a_source_in_web_map_metres_in_longitude_and_latitude` _runs_ the _pipeline_ on a _place_ in EPSG:3857 and _reads_ it _back_ from the _script_ at 113.55 E, 22.19 N in SRID 4326 |
| FR-12 | The _pipeline_ must _refuse_ a _script_ _name_ _already_ _used_, or a _table_ _left_ with no _rows_, _name_ the _fault_ and _write_ no _script_. | US-24 c3 | `etl/tests/test_golden.py`, `etl/tests/test_table_sql.py` |
| FR-13 | _Each_ _table_ must _run_ the ***filters*** its _pipeline_ _file_ _lists_, in that _order_, _each_ on the _rows_ the _one_ _before_ _kept_; the filters are `dropEmptyShapes`, `repairShapes`, `swapCoordinates`, `insideArea`, `tidyText`, `requireFields`, `dropDuplicateIds` and `dropNearDuplicates`. | US-36 c1 | `etl/tests/test_pipeline.py`, `etl/tests/test_chain.py`, `etl/tests/test_all_filters.py` |
| FR-14 | A filter must _drop_ _each_ _row_ it _cannot_ _pass_, or, when it _says_ `"onFail": "stop"`, _end_ the _run_ with no _script_ _written_. | US-36 c2 | `etl/tests/test_filters.py`, `etl/tests/test_run.py` |
| FR-15 | The _run_ must _end_ with no _script_ _written_ when a _table_ _loses_ more than `droppedOverPercent` of its _rows_ (10 by _default_) or _keeps_ _fewer_ than `rowsUnder` (1 by _default_); `stopWhen` must _set_ both for the _whole_ _file_ or for _one_ _table_. | US-36 c3 | `etl/tests/test_pipeline.py`, `etl/tests/test_golden.py` |
| FR-16 | Every _run_, _finished_ or _stopped_, must _write_ a _log_ in `db/seed-logs/`, _named_ _after_ the _script_, with _each_ _table_'s _rows_ _read_ and _kept_, _each_ filter's _counts_, and _one_ _line_, with its _reason_, for _each_ _row_ _fixed_, _dropped_ or _stopped_ at. | US-36 c4 | `etl/tests/test_run.py`, `etl/tests/test_pipeline.py`, `etl/tests/test_golden.py` |
| FR-17 | A _misspelt_ filter, a _missing_ _setting_ a filter _needs_, or a _key_ the _file_ _format_ _lacks_ must _make_ the _pipeline_ _name_ every _fault_ at _once_ and _run_ _nothing_. | US-36 c5 | `etl/tests/test_pipeline.py`, `etl/tests/test_golden.py` |
| FR-18 | From a _file_ ***geodatabase***, _each_ _table_ must _read_ its ***feature class*** by `layer`, with curves _made_ _straight_, heights _dropped_ and _coded_ _values_ _written_ as their _descriptions_. | US-36 c6 | `etl/tests/test_run.py`, `etl/tests/test_golden.py`, `etl/tests/test_pipeline.py` |

## 4. Loading the seed scripts

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| FR-19 | `Tessera seed` must _run_ _each_ _script_ in `db/seed` it has not _run_ _before_, _once_, in the _order_ of their _names_, and _record_ it; _run_ _again_, it must _run_ _nothing_. | US-24 c4 | `SeedingTests`, whose `Runs_the_scripts_in_the_order_of_their_names` _writes_ _three_ _scripts_ _last_ _first_, 0010 _after_ 0002, and _reads_ _back_ the _order_ they _ran_ in |
| FR-20 | A _script_ that _fails_ must _leave_ the _database_ as it was, and must _run_ _again_ _once_ _fixed_; a _script_ that _replaces_ _tables_ must _replace_ them in _one_ _transaction_. | US-24 c6 | `SeedingTests`; `etl/tests/test_run.py` _checks_ that a _script_ _holds_ no `BEGIN;` of GDAL's _own_ |

## 5. Serving a tile

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| FR-21 | A _request_ for `/{layer}/{z}/{x}/{y}.pbf` of a _published_ _layer_ must _receive_ 200, with the _content_ _type_ `application/vnd.mapbox-vector-tile`. | US-07 c1 | `ServiceTests`, `scripts/lib/edge.ts` |
| FR-22 | The _same_ _request_ _ending_ `.mvt` must _receive_ an _identical_ _body_. | US-07 c2 | `ServiceTests` |
| FR-23 | The _features_ of a _layer_ _stored_ in WGS 84 must _sit_ at the _right_ _positions_ in the _tile_'s _grid_. | US-07 c3 | `ServiceTests`, which decodes a _tile_ of a WGS 84 _layer_ and of a ***Web Mercator*** _layer_ |
| FR-24 | A _request_ for a _tile_ of an _unknown_ _layer_ must _receive_ 404. | US-07 c4 | `ServiceTests` |

## 6. Opening the map

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| FR-25 | `/` must _show_ a _map_ that _fills_ the _window_ and _opens_ on the _area_'s _home_ _view_, with the _places_ of the _nine_ _layers_ on a _plain_ _background_. | US-23 c1 | `web/e2e/map.spec.ts`, `web/src/app/App.test.tsx` |
| FR-26 | `/food.cafes.html` must _open_ the _map_ with the cafés _alone_, in _view_. | US-23 c2 | `web/e2e/map.spec.ts`, `web/src/app/App.test.tsx`, `MapTests`, `ServiceTests` |
| FR-27 | The _front_ _end_ must _keep_ its _state_ in _one_ immutable _model_ that a _pure_ `update` _function_ _alone_ _changes_, and its _tests_ must _run_ _without_ a _browser_. | US-23 c3 | `web/src/app/app.test.ts`, `web/src/architecture.test.ts` |
| FR-28 | The _front_ _end_ must _ship_ _inside_ the _service_'s _own_ _release_. | US-23 c4 | `ServiceTests`, which fetches the _page_ and its _script_ from the _built_ _service_; `scripts/lib/build.test.ts`, which _empties_ the _release_'s _map_ _folder_ _before_ _each_ _build_, so no _file_ of an older _map_ _ships_ |
| FR-29 | `/` must _answer_ 404 when the _settings_ _switch_ the _preview_ _off_. | US-23 c5 | `MapTests` |
| FR-30 | A _pin_ must _act_ as a _sign_, not a _button_: a _click_ on it must _open_ _nothing_. | US-23 c6 | `web/e2e/map.spec.ts` |
| FR-31 | The _map_ must _name_ the _nine_ _layers_ in its _own_ _code_, _build_ _each_ _tile_'s _address_, `/{layer}/{z}/{x}/{y}.pbf`, on the _page_'s _own_ _origin_, and _load_ no _list_ of _layers_. | US-23 c7 | `web/src/layers/layers.test.ts`, `web/src/map/style.test.ts`, `web/e2e/map.spec.ts`; `SourceTests`, whose `Lists_the_same_nine_layers_as_the_map` _compares_ the _service_'s _nine_ _layers_ with the _map_'s |
| FR-32 | When a _tile_ _request_ _answers_ with a ***problem document***, the _page_ must _show_ its `detail`, or its `title` where it has _none_, as _text_ over the _map_. | US-23 c8 | `web/src/map/map.test.ts`, `web/src/app/App.test.tsx`, `web/e2e/map.spec.ts` |

## 7. Zooming and clusters

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| FR-33 | '+' and '−' must _move_ the _map_ _one_ _whole_ _level_, _doubling_ or _halving_ the _scale_ _about_ the _centre_ of the _view_, and _stop_ at 0 and at 22. | UI-09 c1 | `web/src/map/map.test.ts`, `web/src/map/readouts.test.ts`, `web/e2e/map.spec.ts` |
| FR-34 | The _button_ at the _end_ of the _range_ must _turn_ _grey_. | UI-09 c2 | `web/src/map/controls.test.tsx` |
| FR-35 | A _click_ on a ***cluster*** must _let_ the _map_ _stop_ _between_ _levels_, such as 16.4. | UI-09 c4 | `web/src/map/readouts.test.ts`, `web/src/map/clusters.test.ts` |
| FR-36 | A _pan_ must _never_ _slide_ the _map_ _far_ _enough_ to _show_ its _edge_. | UI-09 c5 | `web/e2e/map.spec.ts` |
| FR-37 | On a _zoom_, the _pins_ must _keep_ their _size_ on _screen_ and _move_ in _step_ with the _map_. | UI-09 c6 | No _test_ yet |
| FR-38 | _Pins_ that would overlap at the _current_ _level_ must _show_ as _one_ cluster with their _number_, _ringed_ in the _colours_ of their _groups_ in proportion. | UI-10 c1 | `web/src/map/clusters.test.ts`, in _part_: no _test_ _checks_ the _ring_ |
| FR-39 | A _click_ on a cluster must _zoom_ the _map_ no _further_ than it must to _open_ it, and at _least_ to _level_ 16. | UI-10 c3 | `web/src/map/clusters.test.ts`, `web/e2e/map.spec.ts` |

## 8. Names on the map

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| FR-40 | From _level_ 16, _each_ _pin_ on _screen_ must _show_ its _name_ _beside_ it in a _dark_, _plain_ tooltip. | UI-11 c1 | `web/e2e/map.spec.ts`, `web/src/map/style.test.ts` |
| FR-41 | _Below_ _level_ 16, every _pin_ must _hide_ its _name_. | UI-11 c2 | `web/e2e/map.spec.ts` |
| FR-42 | The _pins_ _inside_ a cluster must _hide_ their _names_ until it _opens_. | UI-11 c3 | `web/src/map/style.test.ts` |
| FR-43 | At _any_ _level_, _each_ _name_ must _stay_ _clear_ of every _other_ _name_ and every icon, _across_ every _layer_. | UI-17 c1 | `web/e2e/map.spec.ts`, which _compares_ _images_ at _levels_ 16, 18 and 20; the GitHub _build_ _now_ _compares_ them _inside_ Playwright's _own_ Docker _image_, _though_ no GitHub _run_ has yet _proved_ that _step_ |
| FR-44 | When _two_ _names_ would clash, a _fixed_ _order_ must _decide_ which _keeps_ its _name_: _between_ _layers_, the _order_ the _map_'s _style_ _sets_; _within_ a _layer_, the _places_' ids. | UI-17 c2 | `web/src/map/style.test.ts`; `TileTests` _holds_ the _approved_ ***tile query***, which _orders_ _places_ by _primary_ _key_, and its `Gives_a_view_no_order` _proves_ a _view_, which has no _key_, _gets_ no _order_ |
| FR-45 | The _map_ must _try_ a _name_ on the _right_, the _left_, _above_ and _below_ _before_ it _leaves_ the _name_ out. | UI-17 c3 | `web/src/map/style.test.ts` |
| FR-46 | A _name_ _left_ out must _leave_ its icon on _show_, and must _appear_ _once_ _zooming_ in _makes_ _room_. | UI-17 c4 | `web/src/map/style.test.ts` |
| FR-47 | _Crowded_ _places_ must _stay_ in clusters _below_ _level_ 16; from _level_ 16 every _place_ must _show_ its icon, and the _fixed_ _order_ must _decide_ _only_ which _names_ _win_ the _space_. | UI-17 c5 | `web/src/map/clusters.test.ts` |
| FR-48 | A _name_ _near_ the _edge_ of a _tile_ must _give_ _way_ to its _neighbours_ as a _name_ in the _middle_ does. | UI-17 c6 | No _test_ yet |

## 9. Distance, direction and position

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| FR-49 | A _scale_ _bar_ at the _bottom_ _left_ must _show_ a _round_ _length_, 1, 2 or 5 of a _unit_, no _longer_ than 140 pixels: 500 m at _level_ 15 and 50 m at _level_ 18 at Macau's latitude. | UI-19 c1 | `web/src/map/readouts.test.ts`, `web/e2e/map.spec.ts` |
| FR-50 | The _bar_ must have _four_ _sections_, _filled_ and _empty_ in _turn_, _marked_ '0', the halfway _figure_ and the _full_ _length_ with its _unit_. | UI-19 c2 | `web/src/map/controls.test.tsx`, which _reads_ the _four_ _sections_ from the _bar_'s gradient in `styles.css` and the _three_ _figures_ _under_ them; `web/e2e/map.spec.ts` |
| FR-51 | A _click_ on the _bar_ must _switch_ _between_ metric and imperial _units_, and its _label_ must _name_ the _units_ it will _switch_ to. | UI-19 c4 | `web/src/map/readouts.test.ts`, `web/src/map/controls.test.tsx`, `web/e2e/map.spec.ts` |
| FR-52 | A compass at the _top_ of the _controls_ on the _right_ must _point_ _north_, with a _red_ _needle_ and a _red_ 'N'. | UI-19 c5 | `web/src/map/controls.test.tsx`, in _part_: no _test_ _checks_ its _place_ or its _colour_ |
| FR-53 | With the _pointer_ over the _map_, the latitude and longitude _under_ it must _show_ to the _right_ of the _scale_ _bar_, latitude _first_, to _two_ decimal _places_, such as '22.19° N, 113.55° E', and _change_ as the _pointer_ _moves_. | UI-19 c7 | `web/src/map/readouts.test.ts`, `web/src/map/controls.test.tsx`, `web/e2e/map.spec.ts` |
| FR-54 | The readout must _go_ when the _pointer_ _leaves_ the _map_ or _moves_ over the _scale_ _bar_, the compass or a _zoom_ _button_. | UI-19 c8 | `web/e2e/map.spec.ts`, `web/src/map/controls.test.tsx`, in _part_: no _test_ _covers_ the _scale_ _bar_ or the compass |

## 10. Opening the service to the office

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| FR-55 | An _office_ _machine_ that _opens_ `http://` and the _Windows_ _machine_'s _office_ _name_ must _reach_ Nginx in Ubuntu on _port_ 80 and _receive_ _tiles_ in _plain_ HTTP. | US-35 c1 | No _test_ yet: it _needs_ a _run_ on the _office_ _machine_ |
| FR-56 | The _Windows_ _machine_ must _keep_ its _office_ _address_ _through_ a _restart_, so the _office_ _name_ _still_ _points_ at it. | US-35 c3 | No _test_ yet |
| FR-57 | _After_ WSL2 _restarts_, _port_ 80 must _reach_ the _service_ _again_ _once_ `npm start` _runs_, with no _other_ _step_ _run_ _again_. | US-35 c4 | No _test_ yet |
| FR-58 | `npm run office-port` must _say_ so when WSL2 _runs_ in _any_ _mode_ but _mirrored_, and _print_ the _two_ _steps_ that _switch_ it. | US-35 c6 | `scripts/lib/office.test.ts` |
