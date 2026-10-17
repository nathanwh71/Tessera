# Non-functional requirements

Tessera must _meet_ 67 non-functional _requirements_: how _well_ it does its _work_, _rather_ than what _work_ it does. They _cover_ ***tiles*** that _cost_ _less_ to _send_ _again_, a _service_ that _stays_ steady when the _database_ is _slow_ or _down_, its _settings_ and ***health check***, and a _map_ that every _visitor_ can _read_ and _use_ in _calm_. _Each_ _comes_ from an acceptance _criterion_ of a ***user story*** in `docs/user-stories/user-stories.md`: the N- _stories_, and the _criteria_ of the UI- _stories_ that _judge_ quality _rather_ than _behaviour_. _Two_ _other_ _files_ _hold_ the _rest_. `functional-requirements.md` _says_ what the _service_, the _map_, the ***pipeline*** and the _scripts_ do, and `security-requirements.md` _says_ what Tessera must _refuse_, and to whom.

**How to read them.** 'Must' _marks_ a _requirement_. _Source_ _names_ the _story_ and the _criterion_ the _requirement_ _comes_ from: 'US-07 c4' is _criterion_ 4 of _story_ US-07. _Checked_ by _names_ the _test_ that _proves_ it: a C# _test_ _class_, such as `PoolTests`; a _test_ _file_ in TypeScript or Python, such as `web/e2e/map.spec.ts`; or a _script_ that _checks_ the _running_ _system_, such as `scripts/lib/edge.ts`. 'In part' _means_ the _test_ _proves_ _some_ of the _sentence_, and the _note_ _says_ which _part_ it _leaves_ out. 'No test yet' _means_ no _test_ _proves_ it.

## 1. Tiles that cost less to send again

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| NFR-01 | A _tile_ _answered_ 200 must _carry_ `Cache-Control: public, max-age=N`, where N is `Tiles.CacheSeconds` in `tessera.json`; the _shipped_ _file_ and the _default_ must both _set_ 60. | N-18 c1 | `TileTests`, `ServiceTests`, `SettingsTests`, `scripts/lib/edge.ts`, `web/e2e/map.spec.ts` |
| NFR-02 | With `Tiles.CacheSeconds` _set_ to 0, a _tile_ must _carry_ `Cache-Control: no-cache`, so a _browser_ _checks_ _before_ it _reuses_ a _copy_ and Nginx _keeps_ _nothing_. | N-18 c2 | `TileTests`, in _part_: no _test_ _checks_ that Nginx _keeps_ _nothing_ |
| NFR-03 | Every _error_ _answer_ must _carry_ `Cache-Control: no-store`, so no _browser_ _keeps_ a ***problem document***. | N-18 c3 | `ServiceTests`, `CatalogueTests` (_integration_), `scripts/lib/edge.ts` |
| NFR-04 | _Once_ `max-age` _seconds_ _pass_ _after_ a _new_ ***seed script*** _runs_, every _browser_ must _ask_ _again_ and _see_ the _new_ _places_. | N-18 c4 | No _test_ yet |
| NFR-05 | The _service_ must _refuse_ to _start_, and _name_ the _setting_, when `Tiles.CacheSeconds` is _below_ 0 or not a _whole_ _number_. | N-18 c5 | `SettingsTests` |
| NFR-06 | A _tile_ _answered_ 200 must _carry_ a _weak_ `ETag`, `W/"…"`, _made_ from the _tile_'s _bytes_ _alone_, so the _same_ _bytes_ _carry_ the _same_ _tag_ on _any_ _machine_ and _after_ a _restart_. | N-19 c1 | `TileTests`, `ServiceTests`, `scripts/lib/edge.ts` |
| NFR-07 | A _request_ whose `If-None-Match` _equals_ the _tile_'s _tag_ must _receive_ 304 with no _body_. | N-19 c2 | `ServiceTests`, `scripts/lib/edge.ts` |
| NFR-08 | Nginx must _answer_ _any_ _request_ for a _tile_ _built_ _within_ the _last_ `Tiles.CacheSeconds` _seconds_ from its _kept_ _copy_, _say_ `X-Cache-Status: HIT`, and _run_ no _query_. | N-19 c3 | `scripts/lib/edge.ts` |
| NFR-09 | _Kept_ _tiles_ must _take_ no more than 64 MB in all, and when the _room_ is _full_ the _tiles_ _used_ _least_ must _go_ _first_. | N-19 c4 | No _test_ yet |
| NFR-10 | _Once_ `max-age` _seconds_ _pass_ _after_ a _new_ _seed_ _script_ _runs_, every _kept_ _tile_ must have _expired_, and the _next_ _request_ must _build_ it _again_. | N-19 c5 | No _test_ yet |
| NFR-11 | A 200 _answer_ to a _request_ with `Accept-Encoding: gzip`, for a _tile_, the _page_ or _one_ of the _map_'s _files_, must _arrive_ gzip-packed, _carry_ `Content-Encoding: gzip`, and unpack to the _plain_ _answer_'s _bytes_. | N-20 c1 | `scripts/lib/edge.ts`, in _part_: it _checks_ a _tile_ and the _map_'s _main_ _script_, not the _page_ |
| NFR-12 | A _request_ _without_ `Accept-Encoding` must _receive_ a _plain_ _body_ with no `Content-Encoding`. | N-20 c2 | `scripts/lib/edge.ts`, in _part_: its _plain_ _requests_ _send_ `Accept-Encoding: identity`, and no _check_ _reads_ `Content-Encoding` |
| NFR-13 | _Packed_ and _plain_ _answers_ alike must _carry_ `Vary: Accept-Encoding`. | N-20 c3 | `scripts/lib/edge.ts`, in _part_: it _checks_ the _packed_ _answer_ _alone_ |
| NFR-14 | The ***weak ETag*** of a 304 must _match_ whichever _form_, _packed_ or _plain_, the _browser_ _holds_. | N-20 c4 | `scripts/lib/edge.ts`, in _part_: it _sends_ _back_ the _plain_ _answer_'s _tag_ _alone_ |
| NFR-15 | _Many_ _requests_ at _once_ for _one_ _unbuilt_ _tile_ must _run_ _one_ _query_, and every _request_ must _receive_ the _tile_. | N-24 c1 | `scripts/lib/edge.ts` |
| NFR-16 | The _query_ must _go_ on for the _rest_ when _one_ _browser_ _leaves_ _while_ it _waits_. | N-24 c2 | No _test_ yet |
| NFR-17 | A _request_ that _arrives_ _once_ the _query_ has _ended_ must _find_ the _kept_ _copy_. | N-24 c3 | `scripts/lib/edge.ts` |

## 2. Steady when the database is not

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| NFR-18 | A _tile_ _request_ must _answer_ 503 with `Retry-After: 5` and `Cache-Control: no-store`, as a _problem_ _document_, when the _database_ _cannot_ be _reached_, _refuses_ a _connection_, has no _room_ for another, or is _stopping_ or _starting_. | N-22 c1 | `CatalogueTests` (_integration_), `DatabaseErrorTests` |
| NFR-19 | A _query_ that _runs_ _past_ `Database.QueryTimeoutSeconds` (10), or a _wait_ for a _connection_ _past_ `Database.ConnectionWaitSeconds` (3), must _answer_ 503. | N-22 c2 | `CatalogueTests` (_integration_) |
| NFR-20 | The _service_ must _keep_ no _copy_ of a _failure_, so the _next_ _request_ _asks_ _again_. | N-22 c3 | `CatalogueTests` (_integration_), which _reads_ `no-store` on the 503 |
| NFR-21 | _Any_ _other_ _database_ _failure_ must _answer_ 500. | N-22 c4 | `DatabaseErrorTests`, in _part_: it _sorts_ the _codes_, and no _test_ _sends_ a 500 _through_ the _route_ |
| NFR-22 | The _service_ must _serve_ a _table_ _granted_ _after_ start-up _within_ `Catalogue.RefreshSeconds` _seconds_. | N-23 c1 | `CatalogueTests` (_integration_) |
| NFR-23 | A _revoked_ ***layer*** must _leave_ the _list_ _within_ the _same_ _time_, and a _new_ _tile_ of it must _answer_ 404; _tiles_ _already_ _kept_ may _stay_ in _browsers_ and in Nginx for _up_ to `Tiles.CacheSeconds`. | N-23 c2 | `CatalogueTests` (_integration_) |
| NFR-24 | With the _database_ _down_ at _start_, the _service_ must _start_, _serve_ the _map_ and _answer_ _each_ _tile_ 503 until the _list_ _loads_, _trying_ _again_ every 5 _seconds_. | N-23 c3 | `StartTests`, `CatalogueTests` (_unit_), in _part_: no _test_ _asks_ for the _map_ with the _database_ _down_ |
| NFR-25 | A _reload_ that _fails_ must _keep_ the _last_ _good_ _list_. | N-23 c4 | `CatalogueTests` (_unit_) |
| NFR-26 | With every _connection_ _busy_, the _pool_ must _lend_ the _next_ _free_ _one_ to the _request_ that has _waited_ _longest_. | N-26 c1 | `PoolTests` |
| NFR-27 | A _query_ _sent_ with its _request_'s _own_ _cancel_ _signal_ must _leave_ the _queue_ at _once_ when that _request_ is _cancelled_ _while_ it _waits_. | N-26 c2 | `PoolTests` |
| NFR-28 | With every _connection_ _busy_ for more than a _second_, the _health_ _check_'s _ping_ must _give_ _up_ _within_ _about_ a _second_, `/health` must _say_ `DOWN`, and the _ping_ must _leave_ the _queue_. | N-26 c3 | `PoolTests`, in _part_: it _calls_ the _ping_, not `/health` |
| NFR-29 | With the _service_ or the _database_ _down_ or _hanging_, Nginx must _serve_ at _once_ every _tile_ it _holds_, _even_ _past_ its `max-age`. | N-28 c1 | `scripts/lib/edge.ts`, in _part_: it _stops_ the _service_, not the _database_, and its _kept_ _tile_ is younger than its `max-age` |
| NFR-30 | Nginx must _serve_ an _expired_ _tile_ _while_ it fetches a _fresh_ _copy_ _behind_ it. | N-28 c2 | No _test_ yet |
| NFR-31 | With the _service_ _down_, a _tile_ Nginx _never_ _held_ must _answer_ 503 with `Retry-After: 5`, as a _problem_ _document_. | N-28 c3 | `scripts/lib/edge.ts` |
| NFR-32 | With the _database_ _hanging_, a _tile_ Nginx _never_ _held_ must _wait_ no _longer_ than `Database.ConnectionWaitSeconds` or `Database.QueryTimeoutSeconds`, then _answer_ 503. | N-28 c4 | `CatalogueTests` (_integration_), in _part_: its _database_ is _slow_ or _gone_, _never_ _hanging_ |
| NFR-33 | With the _database_ _down_, `/health` must _answer_ `DOWN` _within_ a _second_. | N-28 c5 | `StartTests`, in _part_: the _test_ _allows_ _two_ _seconds_ |
| NFR-34 | `npm start` must _run_ _two_ _copies_ of the _service_, on `TESSERA_SERVICE_PORT` (7800 by _default_) and the _next_ _port_, and Nginx must _share_ the _requests_ _between_ them in _turn_. | N-29 c1 | `scripts/lib/plan.test.ts`, `scripts/lib/edge.ts` |
| NFR-35 | When a _request_ _reaches_ a _copy_ that has _stopped_ or _crashed_, Nginx must _pass_ it to the _other_ _copy_, and the _visitor_ must _get_ the _answer_. | N-29 c2 | `scripts/lib/edge.ts` |
| NFR-36 | _After_ _three_ _failed_ _requests_ to _one_ _copy_ _within_ _ten_ _seconds_, Nginx must _rest_ that _copy_ for _ten_ _seconds_ and _send_ _everything_ to the _other_. | N-29 c3 | No _test_ yet |
| NFR-37 | `npm start` must _start_ a _copy_ that _stops_ on its _own_ _again_ _after_ a _second_, _double_ the _wait_ _after_ _each_ _quick_ _failure_ _up_ to 30 _seconds_, and _wait_ a _second_ _again_ _after_ a _copy_ that _ran_ for a _minute_. | N-29 c4 | `scripts/lib/supervise.test.ts`, `scripts/lib/plan.test.ts` |
| NFR-38 | With the _database_ _down_, Nginx must _rest_ neither _copy_, _since_ a 503 _counts_ as an _answer_, not a _failure_. | N-29 c5 | No _test_ yet |
| NFR-39 | With both _copies_ _down_, Nginx must _serve_ every _tile_ it _holds_ and _answer_ the _rest_ 503. | N-29 c6 | `scripts/lib/edge.ts` |
| NFR-40 | Ctrl+C must _make_ `npm start` _stop_ both _copies_ and _start_ neither _again_. | N-29 c7 | `scripts/lib/supervise.test.ts`, in _part_: it _proves_ a _stopped_ loop _starts_ _nothing_, and no _test_ _presses_ Ctrl+C |

## 3. Settings and health

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| NFR-41 | `tessera.json` must _hold_ its _keys_ in _five_ _sections_, `Database`, `Tiles`, `Map`, `Catalogue` and `Area`, _beside_ ASP.NET Core's `Urls` and `Logging`, and _each_ _time_ must _carry_ its _unit_ in its _name_. | N-21 c1 | `SettingsTests`, in _part_: it _reads_ the _shipped_ _file_, and no _test_ _checks_ the _sections_ or the _units_ |
| NFR-42 | _Each_ _key_ _left_ out, or every _key_ when the _file_ is _missing_, must _take_ its _default_. | N-21 c2 | `SettingsTests`, in _part_: no _test_ _runs_ _without_ the _file_ |
| NFR-43 | An ***environment variable*** that _names_ a _key_, with `__` for _each_ _step_ _down_, such as `Tiles__CacheSeconds=0`, must _override_ the _file_. | N-21 c3 | `SettingsTests`, in _part_: it _sets_ `Tiles:CacheSeconds` in _memory_, not a variable with `__` |
| NFR-44 | The _service_ must _refuse_ to _start_, and _name_ every such _problem_ at _once_, when a _section_ _holds_ a _key_ it does not _read_ or a _value_ _outside_ its _limits_. | N-21 c4 | `SettingsTests` |
| NFR-45 | With the _list_ of _layers_ _loaded_ and the _database_ _answering_ _within_ a _second_, `/health` must _answer_ 200 `{"status":"UP"}`. | N-25 c1 | `ServiceTests` |
| NFR-46 | `/health` must _answer_ 503 `{"status":"DOWN"}` when the _database_ _fails_, and 503 `{"status":"OUT_OF_SERVICE"}` when _only_ the _list_ is _unloaded_, and _name_ no _fault_. | N-25 c2 | `StartTests`, `CatalogueTests` (_unit_), in _part_: no _test_ _reads_ `OUT_OF_SERVICE` from `/health` |
| NFR-47 | _Each_ _answer_ from `/health` must _carry_ `Cache-Control: no-store`. | N-25 c3 | `ServiceTests`, in _part_: it _checks_ `UP` _alone_ |
| NFR-48 | `npm start` must _say_ the _service_ is _ready_ _only_ _once_ `/health`, _asked_ _through_ Nginx, _says_ `UP`. | N-25 c4 | No _test_ yet |

## 4. A calm page anyone can use

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| NFR-49 | The _page_'s _colours_ must _come_ from Material 3's _colour_ ***roles***, _built_ from the _action_ _blue_ `#1F5FA8`, and the _group_ _colours_ must _appear_ on _pins_ and ***clusters*** _alone_. | UI-15 c1 | `web/src/rules.test.ts`, `web/src/design/design.test.ts` |
| NFR-50 | All _text_ must _use_ Roboto in _Material_'s _type_ _scale_, and every icon must _come_ from _Material_ Symbols, _rounded_. | UI-15 c2 | `web/src/rules.test.ts`, `web/src/map/style.test.ts`, `web/e2e/map.spec.ts`, in _part_: no _test_ _checks_ the _type_ _scale_ |
| NFR-51 | _Each_ _surface_ must _follow_ _Material_'s elevation, with the _scale_ _bar_ and the coordinates at _level_ 1 and the _zoom_ _buttons_, the compass and clusters at _level_ 2, and every _surface_ must be _solid_. | UI-15 c3 | No _test_ yet |
| NFR-52 | _Each_ _shape_ must _follow_ _Material_'s _corners_: 12 pixels for the _zoom_ _buttons_, the _scale_ _bar_ and the coordinates, and 4 for the _name_ tooltips. | UI-15 c4 | No _test_ yet |
| NFR-53 | A _state_ _layer_ must tint a _control_ by 8 _per_ _cent_ on hover and 12 _per_ _cent_ on _press_. | UI-15 c5 | No _test_ yet |
| NFR-54 | As the _page_ _loads_, _each_ _pin_ must _drop_ into _place_, _one_ _after_ another, in 0.3 _seconds_. | UI-15 c6 | `web/src/design/design.test.ts`, `web/e2e/map.spec.ts` |
| NFR-55 | Every _other_ _movement_ must _follow_ an _action_ of the _visitor_'s, _use_ _Material_'s _emphasised_ easing and _last_ 0.3 _seconds_ or _less_, and _nothing_ must loop. | UI-15 c7 | `web/src/rules.test.ts`, `web/src/design/design.test.ts` |
| NFR-56 | _Nothing_ must _move_ for a _visitor_ who has _asked_ their _system_ to _reduce_ motion. | UI-15 c8 | `web/src/rules.test.ts`, `web/src/design/design.test.ts`, `web/e2e/map.spec.ts` |
| NFR-57 | The _map_'s _background_ must _stay_ _plain_, with _nothing_ _drawn_ beneath the _places_. | UI-15 c9 | `web/src/map/style.test.ts`, `web/e2e/map.spec.ts` |
| NFR-58 | _Zoomed_ in _far_ _enough_, the _pins_ must _move_ out from a cluster to their _own_ _places_ _within_ 0.3 _seconds_, and _zooming_ out must _draw_ them _back_. | UI-10 c2 | No _test_ yet |
| NFR-59 | A cluster on _screen_ must _rest_ _still_, with no pulse or loop. | UI-10 c5 | `web/src/rules.test.ts`, in _part_: it _checks_ the _page_'s _styles_, not the clusters MapLibre _draws_ |
| NFR-60 | The _scale_ _bar_ must _change_ its _length_ and _figures_ _within_ 0.3 _seconds_ of a _zoom_. | UI-19 c3 | `web/src/rules.test.ts`, in _part_: it _caps_ every _written_ duration at 300 ms |
| NFR-61 | A _browser_ that has no WebGL, or has had it _taken_ _away_, must _see_ _words_ in _place_ of the _controls_, and the _controls_ _again_ _once_ it can _draw_; an _error_ no _part_ of the _page_ _catches_ must _leave_ _words_, not a blank _page_. | US-23 c9 | `web/src/app/App.test.tsx`, `web/src/app/mount.test.tsx`, `web/e2e/cannot-draw.spec.ts` |

## 5. Accessibility

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| NFR-62 | _Each_ _zoom_ _button_'s _label_ must _state_ the _current_ _level_ to a _screen_ _reader_, such as 'Zoom in, now at level 15 of 22', and the _level_ must _show_ _nowhere_ on _screen_. | UI-09 c3 | `web/src/map/controls.test.tsx`, `web/src/map/readouts.test.ts`, `web/e2e/map.spec.ts` |
| NFR-63 | A cluster's _label_ must _name_ its _places_ to a _screen_ _reader_ and _say_ that _zooming_ in _shows_ them. | UI-10 c4 | `web/src/map/clusters.test.ts`, `web/e2e/map.spec.ts` |
| NFR-64 | _Each_ _pin_ must _show_ its _group_'s _colour_ with the _group_'s _shape_ in _white_ _inside_: a _circle_ for _Food_ and _drink_, a triangle for _Outdoors_, a _square_ for _Culture_ and a _diamond_ for _Transport_. | UI-12 c1 | `web/src/design/design.test.ts`, `web/src/map/style.test.ts` |
| NFR-65 | _Text_ in a _group_'s _colour_ on _white_ must _reach_ a _contrast_ of at _least_ 4.5:1. | UI-12 c2 | `web/src/design/design.test.ts` |
| NFR-66 | The compass must be a _sign_, not a _button_, and _screen_ _readers_ must _hear_ 'Compass: north is up'. | UI-19 c6 | `web/src/map/controls.test.tsx`, `web/e2e/map.spec.ts` |
| NFR-67 | The readout of the _pointer_'s _position_ must _stay_ _silent_ to _screen_ _readers_, _since_ it _follows_ the _pointer_ _alone_. | UI-19 c9 | `web/src/map/controls.test.tsx` |
