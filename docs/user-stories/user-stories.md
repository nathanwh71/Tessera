# Phase Oct: the user stories, in build order

Thirty _user_ _stories_, in the _order_ you _build_ them. _Each_ _builds_ _only_ on _stories_ _above_ it, so you can _build_ and _ship_ them _one_ at a _time_ from the _top_. They _build_ _two_ _things_: an HTTP API that _serves_ a ***tile*** of a ***layer***, and a _map_ that _shows_ those _tiles_. _Office_ _machines_ _call_ the API on _port_ 80, in _plain_ HTTP, and _ask_ for _each_ _tile_ by _layer_ _name_; no _client_, the _map_ _included_, _loads_ a _list_ of _layers_.

The _stories_ _say_ what Tessera must do and how to _prove_ it. Where a _part_ of the _build_ does the _work_, they _name_ it: Nginx _stands_ in _front_ of the _service_ (ADR 0008), DbUp _loads_ the _places_ (ADR 0009), and `tessera.json` _holds_ the _settings_ (ADR 0003). _Standards_ that _fix_ the _data_ or the interface are _named_: HTTP, RFC 9457's _problem_ _details_, GeoJSON, WGS 84 and ***Web Mercator***; so is Material 3, the _design_ _system_ of the _map_, in the NTessera _Project_'s `claude/design/map/`. _One_ _story_ _names_ its _platform_, _because_ the _office_'s _machine_ _fixes_ it: US-35 _opens_ _port_ 80 on Ubuntu 24.04 _inside_ WSL2 on _Windows_ 11. _Each_ _number_'s _letters_ _mark_ its _kind_: _US_ for the _service_, S for _security_, N for _tiles_ that _cost_ _less_ and a _service_ that _stays_ steady, and UI for the _map_.

The _stories_ _name_ **the area**, the _place_ the _tiles_ _cover_, in _place_ of Macau. Its _name_, its _box_ and its _home_ _view_ are _settings_: the _office_ _sets_ them to Macau, and every _figure_ the _stories_ _give_ for the _area_ is Macau's.

_Three_ _rules_ _run_ _through_ every _story_. Nginx _answers_ the _office_ on _port_ 80 and _passes_ _each_ _request_ to the _service_ on 127.0.0.1, _port_ 7800, and from N-29 to a _second_ _copy_ on _port_ 7801. Every _error_ the HTTP API _answers_ is a ***problem document***, as RFC 9457 _sets_ it (US-07). The _database_'s _address_ _comes_ from the ***environment variable*** `DATABASE_URL`, and every _other_ _setting_ from `tessera.json` (US-00, N-21). _Four_ _stories_ _guard_ _security_: S-01 _against_ ***SQL injection***, with the _one_ _function_ that _quotes_ _names_; S-06 _against_ _malformed_ URLs; S-07 _against_ _machines_ _outside_ the _office_; and S-30 _against_ _one_ _machine_ _crowding_ out the _rest_.

## INVEST in plain words

Every _story_ _meets_ _six_ _tests_, which _together_ _spell_ INVEST.

- **Independent.** The _story_ _needs_ _only_ _stories_ _above_ it, and _none_ _below_. _Once_ those are _built_, you can _build_ and _ship_ it on its _own_; 'Builds on' _names_ them.
- **Negotiable.** The _story_ _fixes_ the _need_, not the _design_. 'Open to talk' _lists_ what may _still_ _change_.
- **Valuable.** Its 'so that' _names_ who _gains_ and what. A _story_ for the _maintainer_ is an *enabler*: it _gives_ no _client_ _anything_ _new_, but it unblocks the _stories_ that do.
- **Estimable.** It is _clear_ _enough_ to _size_, in _points_.
- **Small.** No _story_ exceeds 3 _points_.
- **Testable.** Every _criterion_ can _pass_ or _fail_, and 'Tests to write first' _says_ how to _prove_ the _first_ _ones_.

The _table_ at the _end_ _checks_ _each_ _story_ _against_ all _six_.


## The order at a glance

| Step | Story | Points | Builds on |
|---|---|---|---|
| | **Part A · The service** | | |
| 1 | US-00 _Set_ _up_ the ***walking skeleton*** | 1 | _nothing_ |
| 2 | US-01 _Start_ the _same_ _database_ for the _service_ and its _tests_ | 1 | US-00 |
| 3 | US-03 _Publish_ a _table_ by _granting_ it | 3 | US-01 |
| 4 | US-24 _Publish_ the _places_ of the _area_ in _nine_ _layers_ | 2 | US-01, US-03 |
| 5 | US-07 _Serve_ a _tile_ | 2 | US-24 |
| 6 | S-01 _Keep_ SQL out of every _request_ | 2 | US-07 |
| 7 | S-06 _Refuse_ _malformed_ URLs | 1 | US-07 |
| 8 | US-35 _Open_ the _service_ to the _office_ on _port_ 80 | 1 | US-07 |
| | **Part B · The map** | | |
| 9 | US-23 _Open_ the _map_ of the _area_ | 3 | US-07, US-35 |
| 10 | UI-09 _Zoom_ by ***web-map levels*** 0 to 22 | 2 | US-23 |
| 11 | UI-15 _Keep_ the _page_ _calm_ in Material 3 | 2 | US-23 |
| 12 | UI-12 _Tell_ _place_ _types_ _apart_ _without_ _relying_ on _colour_ | 1 | US-23, US-24 |
| 13 | UI-19 _Read_ _distance_, _direction_ and _position_ | 2 | UI-09 |
| 14 | UI-10 _See_ _crowded_ _places_ merge into ***clusters*** that _open_ as I _zoom_ | 3 | UI-09 |
| 15 | UI-11 _Read_ _place_ _names_ when _zoomed_ in | 1 | UI-10 |
| 16 | UI-17 _Keep_ _place_ _names_ from overlapping | 2 | UI-11 |
| | **Part C · Tiles that cost less** | | |
| 17 | N-18 _Let_ a _browser_ _keep_ a _tile_ | 1 | US-07 |
| 18 | N-19 _Answer_ a _kept_ _tile_ _without_ a _query_ | 2 | N-18 |
| 19 | N-20 _Send_ _tiles_ and the _map_'s _files_ _packed_ | 1 | US-07, US-23, N-19 |
| | **Part D · Steady when the database is not** | | |
| 20 | N-21 _Read_ every _setting_ from _one_ _file_ | 2 | US-00, N-18 |
| 21 | N-22 _Ask_ the _browser_ to _wait_ when the _database_ _cannot_ _answer_ | 2 | N-21, US-07 |
| 22 | N-23 _Serve_ a _newly_ _granted_ _table_ _without_ a _restart_ | 2 | US-03, N-22 |
| 23 | N-24 _Serve_ a _crowd_ from _one_ _query_ | 1 | N-19 |
| 24 | N-25 _Tell_ a _watcher_ the _service_ is _well_ | 1 | N-22, N-23 |
| 25 | N-26 _Wait_ in the _order_ of _arrival_ | 1 | N-22, N-25 |
| 26 | N-28 _Serve_ _kept_ _tiles_ _while_ the _database_ _hangs_ | 2 | N-19, N-22 |
| | **Part E · Nginx guards the door** | | |
| 27 | S-07 _Answer_ _office_ _machines_ _alone_ | 1 | US-35 |
| 28 | S-30 _Cap_ what _one_ _visitor_ may _ask_ | 2 | S-07, N-19 |
| 29 | N-29 _Keep_ _serving_ when _one_ ***copy of the service*** _fails_ | 2 | N-25, N-28 |
| | **Part F · Clean places** | | |
| 30 | US-36 _Clean_ the _places_ _before_ they _reach_ a _script_ | 3 | US-24 |
| | **All 30** | **52** | |

The _service_ _builds_ from the _database_ _up_: the _database_, then the _layers_ in it, then the _tiles_ _cut_ from them. S-01 and S-06 _follow_ the _tile_ they _guard_. US-35 then _opens_ the _service_ to the _office_, which _reaches_ it _only_ _once_ it _serves_ _tiles_ _safely_. The _map_ _follows_: US-23 _opens_ it, and _each_ UI _story_ _teaches_ it _one_ more _thing_ to _show_. UI-15 _comes_ _early_ _because_ it _sets_ _rules_ that every _later_ _control_ must _meet_. _Part_ C _comes_ _next_: _each_ of its _stories_ _makes_ a _working_ _tile_ _cheaper_, so _none_ is _needed_ _before_ the _map_ _works_. _Part_ D _comes_ _next_: it _keeps_ the _service_ steady when the _database_ is _slow_, _busy_, _late_ or _down_, and its _settings_ _come_ _first_ _within_ it, _because_ the _later_ _stories_ _read_ them. _Part_ E _closes_ the _door_ to strangers, _shares_ the _service_ fairly, and _runs_ a _spare_ _copy_; it _comes_ _once_ the _service_ _works_, _since_ it _guards_ what is _already_ there. _Part_ F _cleans_ the _places_ _before_ they _become_ a _script_; the _shipped_ _places_ are _clean_, so it _waits_ until a _publisher_ _brings_ _data_ of their _own_.

## Part A · The service

### Step 1 · US-00 Set up the walking skeleton

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | _nothing_ |

**In plain words.** A _builder_ pegs out the _site_ _before_ _laying_ a brick. This _story_ _lays_ out the codebase and _lets_ _one_ _command_ _build_ the _service_, and another _run_ it. The **walking skeleton** is the thinnest _service_ that _works_ _end_ to _end_: an _answer_ from the _service_, on your _screen_.

> **Story.** **As** the _maintainer_, **I want** a codebase that _builds_ and _runs_ from _one_ _command_, **so that** every _later_ _story_ _starts_ from _working_ _ground_.

**Acceptance criteria**

1. **Given** a _fresh_ _copy_ of the repository, **when** I _run_ the _build_ _command_, **then** the _service_ _builds_.
2. **Given** the _build_, **when** I _run_ the _run_ _command_, **then** the _service_ _starts_ and _answers_ an HTTP _request_ on its _port_.
3. **Given** the _test_ _command_, **when** I _run_ it, **then** every _test_ _passes_.
4. **Given** the _run_ _command_, **then** the _service_ _reads_ its _database_'s _name_, _address_ and _port_ from the _environment_ variable `DATABASE_URL`, and the _code_ _names_ no _machine_. **Given** that variable _missing_, _empty_ or _malformed_, **then** the _service_ _refuses_ to _start_ and _names_ it.

**Tests to write first**

1. **The skeleton answers.** A _test_ _starts_ the _service_ and _sends_ it a _request_; it _fails_ until the _service_ _answers_.
2. **Machines come from the environment.** _Started_ with no variables _set_, the _service_ _stops_ and _names_ `DATABASE_URL`.

**Critical to success**

- **Build and run from one command, from the first day.** _Each_ _later_ _story_ then _adds_ to a codebase that _already_ _runs_.

### Step 2 · US-01 Start the same database for the service and its tests

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | US-00: the codebase and its _tests_ |

**In plain words.** Every _layer_ _lives_ in a _database_, and the _service_ and its _tests_ should _meet_ the _same_ _one_. This _story_ _packages_ _one_ spatial _database_, with the _service_'s _own_ sign-in, and the _places_ of the _area_ as _reference_ _data_: for Macau, the ***seed script*** _made_ from `data/macau-pois.geojson`. US-24's _seed_ _later_ _loads_ them into the _database_. The _service_ _signs_ in as a **role**, a _database_ _user_, and _sees_ _only_ the _tables_ _granted_ to that ***role*** (US-03).

> **Story.** **As** the _maintainer_, **I want** _one_ _packaged_ spatial _database_ with the _service_'s _role_ and the _reference_ _data_, **so that** the _service_ and its _tests_ _meet_ the _same_ _database_ and the _same_ _places_.

**Acceptance criteria**

1. **Given** the _packaged_ _database_, **when** it _starts_, **then** the _role_ _script_ has _made_ the _service_'s _role_.
2. **Given** the _package_, **then** it _carries_ the _seed_ _script_, `db/seed/0001-macau-places.sql`, _made_ from `data/macau-pois.geojson`, _ready_ for US-24's _seed_.
3. **Given** the ***integration tests***, **when** they _run_, **then** they _start_ the _same_ _packaged_ _database_, _run_ the _same_ _role_ _script_ and _read_ the _same_ _seed_.

**Tests to write first**

1. **The role, from the script you ship.** The _tests_ _make_ the _role_ by _running_ the _shipped_ _role_ _script_, then _sign_ in as it.
2. **The seed, as the repository holds it.** `data/macau-pois.geojson` _matches_ its _recorded_ SHA-256 fingerprint, and the _package_ _carries_ the _script_ _made_ from it, whose _first_ _line_ _names_ the _file_.

**Open to talk**

- When to _move_ _past_ PostgreSQL 18 and PostGIS 3.6, which `db/Dockerfile` _pins_.

**Critical to success**

- **Test the script you ship.** A _test_ _against_ a hand-made _role_ _proves_ _nothing_ _about_ the _shipped_ _one_.

### Step 3 · US-03 Publish a table by granting it

| | |
|---|---|
| Size | 3 _points_ |
| Builds on | US-01: the _database_ and the _service_'s _role_ |

**In plain words.** The _database_ _keeps_ the _guest_ _list_; the _service_ _only_ _reads_ it. A _table_ _becomes_ a _map_ _layer_ when the _data_ _publisher_ **grants** the _service_'s _role_ the _right_ to _read_ it, and it _leaves_ when that _right_ is withdrawn. The _list_ of _layers_ the _service_ _builds_ at start-up is the **catalogue**.

> **Story.** **As** a _data_ _publisher_, **I want** every _table_ or _view_ I ***grant*** to the _service_'s _role_ to _become_ a _layer_, **so that** _publishing_ _needs_ no _change_ to the _service_.

**Acceptance criteria**

1. **Given** a _granted_ _table_ with a _typed_ geometry _column_ and an ***SRID*** _above_ 0, **when** the ***catalogue*** _loads_, **then** it is a _layer_ with its SRID and geometry _type_. (An SRID is the _number_ that _names_ a coordinate _system_.)
2. **Given** a _granted_ _view_, or a _view_ that _stores_ its _rows_, **when** the catalogue _loads_, **then** it is a _layer_.
3. **Given** a _table_ _never_ _granted_, **when** the catalogue _loads_, **then** it is absent.
4. **Given** a _table_ whose _name_ _holds_ a _space_ and a _double_ _quote_, **when** the catalogue _loads_, **then** the _layer_ is _present_ and the _service_ _serves_ its _tiles_.
5. **Given** a _granted_ _function_, **when** the catalogue _loads_, **then** it is absent, _since_ _Phase_ Oct _publishes_ _tables_ and _views_ _alone_.
6. **Given** a _table_ _revoked_ _after_ start-up, **when** a _tile_ is _requested_, **then** the _answer_ is 404, as a _problem_ _document_ (US-07).

**Tests to write first**

1. **One way to quote a name.** A _name_ is wrapped in _double_ _quotes_ and every _inner_ _quote_ is _doubled_; then a ***property test***: every _name_ _becomes_ a _safely_ _quoted_ _one_.
2. **Rows become layers, purely.** A _row_ from the _database_'s _own_ _list_ of _tables_ _becomes_ a _layer_ whose id is `schema.name`, and lookup is _exact_ and case-sensitive.
3. **The catalogue query.** _Each_ _column_ the _query_ _returns_ _maps_ to _exactly_ _one_ _field_ of the _row_.
4. **Load it as the real role.** The _test_ _makes_ its _own_ _tables_ _first_: a _granted_ _table_, a _granted_ _view_, a _granted_ _view_ that _stores_ its _rows_, a _table_ _never_ _granted_, a _table_ whose _name_ _holds_ a _space_ and a _double_ _quote_, and a _granted_ _function_. _Signed_ in as the _service_'s _role_, a _granted_ _table_, a _granted_ _view_ and a _stored_ _view_ are _published_; a _table_ _without_ a _grant_ _never_ _appears_; the awkward _name_ is _published_ and _served_; no _function_ _appears_, _even_ _one_ _granted_.

**Open to talk**

- _Whether_ a _later_ _phase_ _publishes_ _functions_ as _layers_.

**Critical to success**

- **The database decides visibility.** The privilege _checks_ _run_ _inside_ the _database_ as the _service_'s _own_ _role_, so a _revoke_ _takes_ _effect_ at the _next_ ***tile query***.
- **Only the catalogue mints names.** Every _name_ that _later_ _reaches_ SQL _comes_ from the _database_'s _own_ _list_, _through_ the _one_ _function_ that _quotes_ _names_.
- **The integration tests use the real role.** As the _database_'s superuser, _criterion_ 3 _passes_ for the _wrong_ _reason_.

### Step 4 · US-24 Publish the places of the area in nine layers

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | US-03: _publishing_ by _grant_; US-01: the _seed_ to _load_ |

**In plain words.** Tessera _serves_ the _places_ of the _area_ in _nine_ _layers_, _gathered_ in _four_ _groups_:

| Group | Schema | Layers |
|---|---|---|
| Food and drink | `food` | `cafes`, `restaurants`, `markets` |
| Outdoors | `outdoors` | `parks`, `playgrounds` |
| Culture | `culture` | `museums`, `libraries` |
| Transport | `transport` | `trains`, `buses` |

The _places_ _arrive_ as _files_ the _data_ _publisher_ _owns_, _each_ with _columns_ of its _own_; for Macau, `data/macau-pois.geojson`, a **GeoJSON** _file_ of _points_ with their _properties_. A ***pipeline***, in `etl/`, _reads_ _each_ _file_ _through_ GDAL and _turns_ it into a SQL _script_, and DbUp _runs_ _each_ _script_ _once_ (ADR 0009). The _tables_ _store_ _each_ _shape_ in longitude and latitude in **WGS 84** (SRID 4326), _whatever_ _system_ the _file_ _uses_. The _tiles_ _use_ **Web Mercator** (EPSG:3857), which _measures_ in _metres_ _east_ and _north_: a ***vector tile*** _stores_ _each_ _point_ as a _position_ _inside_ a _Web_ Mercator _square_, so the _tile_ _query_ _converts_ the _places_ it _returns_ (US-07). No _client_ _sees_ that _conversion_; it _turns_ the _tile_ _back_ into longitude and latitude on its _own_. _Each_ _group_ _becomes_ a **schema**, a _named_ _folder_ of _tables_, and _each_ _layer_ a _table_ in it, so the _layer_ `food.cafes` _carries_ its _group_ in its _name_. You _publish_ the _tables_ by a **grant**, the _database_'s _word_ for a _permission_. A _library_ _buys_ a _book_ (the _seed_), but the _book_ _reaches_ the _open_ _shelf_ _only_ when the librarian _says_ so (the _grant_).

> **Story.** **As** a _data_ _publisher_, **I want** _each_ _file_ of _places_ I _own_ _turned_ into a SQL _script_, _each_ _new_ _script_ _loaded_ _once_, and the _tables_ _published_ by _grant_, **so that** _office_ _machines_ can _load_ the _places_ as _tiles_, _layer_ by _layer_.

**Acceptance criteria**

1. **Given** a _file_ of _places_ and a ***pipeline file*** that _lists_ its _tables_, _each_ with a _layer_ or a ***filter*** on its _rows_, **when** `npm run etl` _runs_, **then** it _writes_ _one_ SQL _script_ that _makes_ _each_ _table_, with a _numbered_ `fid` as its _primary_ _key_, _one_ _column_ for _each_ _property_, a ***spatial index*** and _fresh_ _statistics_.
2. **Given** a _file_ in another coordinate _system_, such as EPSG:3857, **then** the _script_ _stores_ its _shapes_ in longitude and latitude, SRID 4326.
3. **Given** a _script_ _name_ _already_ _used_, or a _table_ _left_ with no _rows_, **then** the _pipeline_ _refuses_, _names_ the _fault_, and _writes_ no _script_.
4. **Given** the _scripts_ in `db/seed`, **when** `Tessera seed` _runs_, **then** it _runs_ _each_ _script_ it has not _run_ _before_, _once_, in the _order_ of their _names_, and _records_ it; _run_ _again_, it _runs_ _nothing_.
5. **Given** no _grant_, **then** no _table_ is _published_; _after_ the _grant_ of _each_ ***schema*** and _each_ _table_, all _nine_ are.
6. **Given** a _script_ that _fails_, **then** the _database_ _keeps_ what it _held_ _before_, and the _script_ _runs_ _again_ _once_ _fixed_. A _script_ that _replaces_ _tables_ _replaces_ them in _one_ _transaction_, so no _tile_ _ever_ _carries_ _half_ a _script_.

**Tests to write first**

1. **Nine tables from the shipped script.** `Tessera seed` on a _fresh_ _database_ _gives_ the _nine_ _tables_ in their _four_ schemas, 100 _rows_ in _each_, _each_ with a spatial index; the ***journal*** _records_ `0001-macau-places.sql`, and a _second_ _run_ _runs_ _nothing_.
2. **Publish by grant, and only then.** With no _grant_, the catalogue _lists_ no _layer_ from the _four_ schemas; _after_ the _grant_, all _nine_ _appear_, in SRID 4326, in the _order_ of `fid`.
3. **A failed script changes nothing.** A _script_ that _drops_ a _table_, then _fails_, _leaves_ the _table_ _whole_, and _stays_ out of the journal.
4. **A replaced table waits for its grant.** A _new_ _script_ that _replaces_ a _granted_ _table_ _leaves_ it unpublished until the _next_ _grant_.
5. **The pipeline's own rules.** ***Unit tests*** in `etl/tests`: INSERT _statements_, a _numbered_ _key_, EPSG:4326, GDAL's _own_ _transaction_ _taken_ out, and a _misspelt_ _pipeline_ _file_ _refused_.

**Open to talk**

- Which _properties_ _each_ _place_ _carries_. The _tiles_ _carry_ them all (US-07), `fid` _among_ them. _Places_ _carry_ no _rank_.

**Critical to success**

- **A grant is a publication.** The _seed_ _creates_ the _tables_; the _data_ _publisher_ _alone_ _publishes_ them.
- **A script is the record.** A _committed_ _script_'s SQL _never_ _changes_; _new_ _data_ _goes_ in a _new_ _script_, so the journal and the repository agree.
- **The tile format converts, not the table.** A vector _tile_ can _hold_ _positions_ _only_ _inside_ its _Web_ Mercator _square_, so the _conversion_ _belongs_ to the _tile_ _format_ (US-07). _Each_ _tile_ _query_ _finds_ its _places_ by the _tile_'s _box_, _converted_ to _degrees_, so the spatial index _applies_.
- **The schema is the group.** A _layer_'s id, such as `food.cafes`, _names_ its _group_, so a _client_ _knows_ a _layer_'s _group_ from its _name_ _alone_.
- **Typed, indexed, analysed.** An untyped _column_ is not _published_ (US-03), and _without_ an index and _fresh_ _statistics_ every _tile_ _reads_ the _whole_ _table_.

### Step 5 · US-07 Serve a tile

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | US-24: _places_ to _put_ in the _tiles_ |

**In plain words.** This is the _heart_ of the _product_. _Asked_ for _tile_ z/x/y of a _layer_, the _service_ _returns_ a _small_ _file_ of _map_ _shapes_ that the _database_ _builds_ in _one_ _query_. _Whenever_ it _cannot_ _serve_, it _answers_ with a **problem document**: a _small_ JSON _note_ in the _shape_ that RFC 9457 _sets_, so every _client_ _reads_ every _error_ the _same_ _way_.

> **Story.** **As** a _map_ developer, **I want** a _tile_ by _layer_ and coordinates, **so that** my _map_ _shows_ the _layer_'s _features_.

**Acceptance criteria**

1. **Given** a _published_ _layer_, **when** I _request_ `/{layer}/{z}/{x}/{y}.pbf`, **then** I _receive_ 200 ('OK') with the _content_ _type_ `application/vnd.mapbox-vector-tile`.
2. **Given** the _same_ _request_ _ending_ `.mvt`, **then** the _body_ is _identical_.
3. **Given** a _layer_ _stored_ in WGS 84 (US-24), **when** I decode its _tiles_, **then** its _features_ _sit_ at the _right_ _positions_ in the _tile_'s _grid_.
4. **Given** an _unknown_ _layer_, **when** I _request_ a _tile_, **then** I _receive_ 404.
5. **Given** _any_ _error_ _answer_ from the HTTP API, such as the 404 _above_ and the 400 of S-06, **then** its _body_ is a _problem_ _document_ as RFC 9457 _defines_ it: the _content_ _type_ `application/problem+json`, with `type`, `title` and `status`, and `detail` where a _sentence_ _helps_. `status` _matches_ the HTTP _status_ _code_, and with `type` as `about:blank`, `title` is the _status_'s _own_ _phrase_, such as 'Not Found'.

**Tests to write first**

1. **An unknown layer is not found.** The _request_ is _refused_ _before_ _any_ _query_ _runs_.
2. **Through the real database.** A _tile_ over the _sample_ _places_ _carries_ their _features_ and _properties_.
3. **Every error is a problem document.** For an _unknown_ _layer_, the _content_ _type_ is `application/problem+json`, `status` _equals_ the _status_ _code_, and `title` _reads_ 'Not Found'.

**Open to talk**

- _Whether_ to _keep_ the `.mvt` _ending_.
- The _tile_'s _cap_ on _features_ and the buffer of _space_ _around_ it: `Tiles.MaxFeatures`, 10,000, and `Tiles.Buffer`, 256, by _default_.
- _Whether_ to _coin_ Tessera's _own_ `type` _addresses_, such as _one_ for an _unknown_ _layer_, or _keep_ `about:blank`.

**Critical to success**

- **One shape for every error.** _Clients_ _read_ _each_ _failure_ the _same_ _way_, so no _handler_ _writes_ an _error_ _body_ of its _own_.
- **The index must apply.** _Transform_ the _tile_'s _box_ into the _layer_'s SRID, _never_ the _table_'s geometry into the _tile_'s, or every _tile_ _reads_ the _whole_ _table_.


### Step 6 · S-01 Keep SQL out of every request

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | US-07: the _tile_ _query_ |

**In plain words.** **SQL injection** is an _attack_ that _hides_ SQL _inside_ a _request_, such as a _layer_ _named_ `x; DROP TABLE cafes`, in the _hope_ that the _database_ _runs_ it. _Two_ _defences_ _stop_ it. _Each_ _value_ from the _request_ _travels_ _beside_ the _query_ as a **bound parameter**, _like_ an _answer_ _written_ in a _form_'s _box_, so the _database_ _never_ _reads_ it as SQL. And the _only_ _names_ that _enter_ the SQL's _text_ _come_ from the catalogue (US-03), _quoted_.

> **Story.** **As** the _maintainer_, **I want** no _request_ to _change_ the SQL the _service_ _sends_, **so that** no _office_ _machine_ can _read_, _change_ or _drop_ a _table_ _through_ the _tile_ API.

**Acceptance criteria**

1. **Given** _any_ _tile_ _address_, **when** the _tile_ _query_ _runs_, **then** every _value_ _reaches_ the _database_ as a _bound_ ***parameter***, and a _property_ _test_ _proves_ it.
2. **Given** _any_ _query_, **then** its _text_ _lives_ in `Queries.cs`, the _one_ _file_ that _holds_ SQL, and _only_ `Database` _sends_ it, _save_ the _seed_'s journal schema (ADR 0009); a _search_ at _each_ _review_ _finds_ SQL _text_ _nowhere_ _else_.
3. **Given** a _table_ or _column_ _name_ in a _query_, **then** it _comes_ from the catalogue, _through_ the _one_ _function_ that _quotes_ _names_ (US-03).
4. **Given** a _request_ for a _layer_ _named_ `x; DROP TABLE cafes`, **then** the _service_ _answers_ 404, as a _problem_ _document_ (US-07), and every _table_ _keeps_ its _rows_.

**Tests to write first**

1. **Every address, one text.** A _property_ _test_: for _any_ _level_, _column_ and _row_, the _tile_ _query_'s _text_ _equals_ the _approved_ _text_, and the _address_ _travels_ as _parameters_ _alone_.
2. **The tile query's text.** It _matches_ an _approved_ snapshot, `approved/tile-query.sql`.
3. **An attack changes nothing.** _Through_ the _real_ _database_, _requests_ for the _layers_ `x; DROP TABLE cafes` and `food.cafes'; --` _each_ _get_ 404, and the _nine_ _tables_ _keep_ their _rows_.

**Critical to success**

- **One file holds every SQL text.** A _reviewer_ _reads_ every _query_ in _one_ _place_, and a _search_ _proves_ no _other_ _file_ _holds_ _one_.
- **Only the catalogue mints names.** The _layer_ _name_ in a _request_ is a lookup _key_, _never_ SQL: the _service_ _finds_ the _layer_ in the catalogue, and _writes_ the catalogue's _quoted_ _name_ into the _query_.

### Step 7 · S-06 Refuse malformed URLs

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | US-07: the _routes_ and the _problem_ _documents_ |

**In plain words.** A **malformed URL** is an _address_ disguised so that it _looks_ _clean_ _only_ _after_ a _web_ _server_ _tidies_ it, such as a _path_ that climbs _above_ the _root_, or a `..` _hidden_ as `%2e%2e`. Nginx _stands_ in _front_ of the _service_ and _refuses_ an _address_ it _cannot_ _read_. A disguised _address_ that Nginx _passes_ on _reaches_ the _service_ _exactly_ as _sent_, so it _matches_ no _route_.

> **Story.** **As** the _maintainer_, **I want** every _malformed_ _request_ _refused_, and every disguised _one_ _left_ unmatched, **so that** no _request_ _reaches_ a _route_ in disguise.

**Acceptance criteria**

1. **Given** a _request_ _target_ that climbs _above_ the _root_, _holds_ a stray or invalid `%`, an _encoded_ _null_ (`%00`) or a _raw_ _control_ _character_, **then** Nginx _answers_ 400 _before_ the _service_ _sees_ it.
2. **Given** that 400, **then** its _body_ is a _problem_ _document_ (US-07) whose `title` _reads_ 'Bad Request', and it _never_ _repeats_ the _target_.
3. **Given** a disguised _target_, such as `%2e%2e`, an _encoded_ _slash_ (`%2F`) or a backslash, **then** it _matches_ no _route_, and the _service_ _answers_ 404 as a _problem_ _document_.
4. **Given** a _clean_ _tile_ _target_, **then** it _reaches_ US-07's _route_ _unchanged_.

**Tests to write first**

1. **Malformed targets are refused.** _Raw_ _requests_, _written_ _byte_ for _byte_ _through_ Nginx, for _five_ _malformed_ or disguised _targets_ _each_ _get_ 400 or 404, as a _problem_ _document_ that _hides_ the _target_.
2. **The answer hides the target.** No 400 _body_ _holds_ _any_ _part_ of its _target_.
3. **A disguised address finds nothing.** The _service_ _answers_ an _unknown_ _address_ with a 404 _problem_ _document_.
4. **Clean targets pass.** A _tile_ _request_ _through_ Nginx _gets_ 200.

**Open to talk**

- _Whether_ Nginx should _refuse_ the disguised _forms_ with 400 _too_, _rather_ than _leave_ them to the _service_'s 404.

**Critical to success**

- **Never repair an address.** Nginx _refuses_ what it _cannot_ _read_, and the _service_ _matches_ _routes_ on the _address_ as _sent_; neither _tidies_ a _path_ into _one_ that _matches_.

### Step 8 · US-35 Open the service to the office on port 80

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | US-07: _tiles_ to _serve_ |

**In plain words.** The _service_ _runs_ in Ubuntu 24.04, _inside_ WSL2, on _one_ _Windows_ 11 _machine_ in the _office_. WSL2 is a _light_ _virtual_ _machine_. In its **mirrored networking** _mode_, Ubuntu _shares_ the _Windows_ _machine_'s _own_ _address_, so a _request_ to the _Windows_ _machine_ on _port_ 80 _reaches_ Nginx in Ubuntu _directly_, and Nginx _sees_ the _address_ of the _office_ _machine_ that _sent_ it. Nginx _passes_ _each_ _request_ to the _service_ on 127.0.0.1, _port_ 7800. _Each_ _office_ _machine_ _reaches_ it in _plain_ **HTTP**, by the _Windows_ _machine_'s _office_ _name_. The _Windows_ _machine_ _keeps_ a _fixed_ _address_, so the _name_ _always_ _points_ at it.

> **Story.** **As** the _maintainer_, **I want** _each_ _office_ _machine_ to _reach_ the _service_ on _port_ 80 by the _Windows_ _machine_'s _office_ _name_, with Nginx _seeing_ _each_ _machine_'s _own_ _address_, **so that** _office_ _machines_ can _load_ _tiles_, and _later_ _stories_ can _tell_ _one_ _office_ _machine_ from another.

**Acceptance criteria**

1. **Given** an _office_ _machine_, **when** it _opens_ `http://` and the _Windows_ _machine_'s _office_ _name_, **then** it _reaches_ Nginx in Ubuntu on _port_ 80 and _receives_ _tiles_ in _plain_ HTTP.
2. **Given** a _request_ from an _office_ _machine_, **then** Nginx _sees_ that _machine_'s _own_ _address_, not the _Windows_ _machine_'s.
3. **Given** a _reserved_ _address_ for the _Windows_ _machine_, **when** it _restarts_, **then** it _keeps_ its _office_ _address_, and the _office_ _name_ _still_ _points_ at it.
4. **Given** WSL2 _restarting_, **then** _port_ 80 _still_ _reaches_ the _service_ _after_ `npm start`, with no _step_ _run_ _again_, _since_ Ubuntu _shares_ the _Windows_ _machine_'s _address_.
5. **Given** the _Windows_ firewall and Hyper-V's firewall in _front_ of WSL2, **then** _each_ _lets_ in _requests_ on _port_ 80, and on no _other_ _port_.
6. **Given** WSL2 in _any_ _mode_ but _mirrored_, **when** `npm run office-port` _runs_, **then** it _says_ so and _prints_ the _two_ _steps_ that _switch_ it.

**Tests to write first**

1. **The steps, without Windows.** `npm run office-port`'s _steps_ _switch_ a _machine_ in NAT _mode_ to _mirrored_, _ask_ _nothing_ more of _one_ _already_ _mirrored_, _open_ _port_ 80 in both firewalls, and _never_ _set_ _up_ a _port_ proxy.
2. **Port 80 serves.** From another _office_ _machine_, a _request_ for a _tile_ on _port_ 80 _gets_ 200, and Nginx's _log_ _shows_ that _machine_'s _address_.
3. **A restart changes nothing.** _After_ `wsl --shutdown` and a _fresh_ `npm start`, _test_ 2 _passes_ _again_.

**Critical to success**

- **Check it on the real machine.** WSL2, _Windows_ and its _two_ firewalls _each_ _set_ these _things_ _up_ their _own_ _way_, so _tests_ 2 and 3 _need_ a _run_ on the _office_ _machine_ _itself_. ***Mirrored networking*** _needs_ _Windows_ 11 22H2 or _later_ and WSL 2.0 or _later_.
- **No port proxy.** A _Windows_ _port_ proxy would _also_ _reach_ Ubuntu, but it _hides_ every _office_ _machine_ _behind_ the _Windows_ _machine_'s _own_ _address_, which defeats S-07 and S-30.

## Part B · The map

### Step 9 · US-23 Open the map of the area

| | |
|---|---|
| Size | 3 _points_ |
| Builds on | US-07: the _tiles_; US-35: an _office_ that can _reach_ the _map_ |

**In plain words.** The _service_ _learns_ to _show_ a _map_. The _page_ is a _fixed_ _frame_, and the _tiles_ _arrive_ _later_, _like_ _photographs_ _slid_ into it. The _map_ _knows_ the _nine_ _layers_ from its _own_ _code_, so it _loads_ no _list_ of _layers_. The _front_ _end_ _keeps_ all it _knows_ in _one_ **model**, a _single_ _value_ that _only_ a _pure_ **`update`** _function_ may _replace_, so a _test_ can _check_ every _change_ _without_ a _browser_.

> **Story.** **As** a stakeholder, **I want** _one_ 2D _map_ of the _area_ at `/` and the _same_ _map_ _opened_ on _one_ _layer_ at `/{layer}.html`, **so that** I can _look_ at the _data_ _without_ _installing_ _anything_.

**Acceptance criteria**

1. **Given** `/`, **then** the _map_ _fills_ the _window_ and _opens_ on the _area_'s _home_ _view_, with the _places_ of the _nine_ _layers_ (US-24) on a _plain_ _background_.
2. **Given** `/food.cafes.html`, **then** the _map_ _opens_ with the cafés _alone_, in _view_.
3. **Given** the _front_ _end_'s _state_, **then** it is _one_ immutable _model_ _changed_ _only_ by a _pure_ `update` _function_, and its _tests_ _run_ _without_ a _browser_.
4. **Given** the _front_ _end_, **then** it _ships_ _inside_ the _service_'s _own_ _release_.
5. **Given** the _preview_ _switched_ _off_ in the _settings_, **then** `/` _answers_ 404.
6. **Given** a _pin_, **then** it is a _sign_, not a _button_: a _click_ on it _opens_ _nothing_.
7. **Given** the _map_, **then** it _names_ the _nine_ _layers_ in its _own_ _code_ and _builds_ _each_ _tile_'s _address_, `/{layer}/{z}/{x}/{y}.pbf`, on the _page_'s _own_ _origin_; it _loads_ no _list_ of _layers_.
8. **Given** a _tile_ _request_ _answers_ with a _problem_ _document_, **then** the _page_ _shows_ its `detail`, or its `title` where it has _none_, as _text_ over the _map_.
9. **Given** a _browser_ that _cannot_ _draw_ the _map_, _because_ it has no WebGL or has _taken_ it _away_, **then** the _page_ _says_ so in _words_ in _place_ of the _controls_, and _shows_ them _again_ _once_ the _browser_ can _draw_; **and given** an _error_ no _part_ of the _page_ _catches_, **then** the _page_ _says_ so in _words_ _rather_ than _going_ blank.

**Tests to write first**

1. **A build that fails first.** CI _runs_ the _front_ _end_'s _build_, static _checks_ and _tests_, and _fails_ until the _front_ _end_ _exists_.
2. **One model, one pure `update`.** The _page_ _opens_ on the _area_ with the _nine_ _layers_ on, and _sends_ no _request_ for a _list_ of _layers_. _Effects_ _leave_ _through_ _ports_, such as fetching a _tile_ or _moving_ the _map_, which the _tests_ _fake_.
3. **A problem shows.** A _fake_ _port_ that _fails_ with a _problem_ _document_ _leaves_ its `detail` in the _model_.
4. **A page that cannot draw says so.** A _fake_ _map_ that _fails_ to _start_, and a _browser_ _started_ with WebGL _turned_ _off_, _each_ _leave_ the _words_ of _criterion_ 9 on the _page_ and no _zoom_ _buttons_.

**Open to talk**

- The _area_'s _home_ _view_: the _level_ and _centre_ the _map_ _opens_ at.

**Critical to success**

- **One model, one `update`.** Every _later_ _story_ _adds_ _messages_ to the _same_ _model_, so a _test_ can _check_ _each_ _change_ _without_ a _browser_.

### Step 10 · UI-09 Zoom by web-map levels 0 to 22

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | US-23: the _map_ |

**In plain words.** As on every _web_ _map_, _each_ _level_ _doubles_ the _detail_ of the _one_ _before_: _level_ 0 _shows_ the _world_, _level_ 22 a _single_ doorway. '+' and '−' _move_ _one_ _whole_ _level_.

> **Story.** **As** a _visitor_, **I want** ***zoom levels*** from 0 to 22 that _behave_ as on _other_ _web_ _maps_, **so that** the _map_ _feels_ _familiar_.

**Acceptance criteria**

1. **Given** '+' or '−', **then** the _map_ _moves_ _one_ _whole_ _level_, _doubling_ or _halving_ the _scale_ _about_ the _centre_ of the _view_, and _stops_ at 0 and at 22.
2. **Given** the _end_ of the _range_, **then** the _button_ at that _end_ _turns_ _grey_.
3. **Given** a _screen_ _reader_, **then** _each_ _button_'s _label_ _states_ the _current_ _level_, such as 'Zoom in, now at level 15 of 22'. The _level_ _shows_ _nowhere_ on _screen_.
4. **Given** a _click_ on a cluster (UI-10), **then** the _map_ may _stop_ _between_ _levels_, such as 16.4.
5. **Given** a _pan_, **then** the _map_ _never_ _slides_ _far_ _enough_ to _show_ its _edge_.
6. **Given** a _zoom_, **then** the _pins_ _keep_ their _size_ on _screen_ and _move_ in _step_ with the _map_.

**Tests to write first**

1. 'doubles and halves the scale one level at a time'.
2. 'keeps the level within 0 to 22'.
3. 'keeps the edge of the map out of view'.

**Open to talk**

- _Whether_ the scroll _wheel_ and pinching, which MapLibre _allows_ and which _stop_ _between_ _levels_, should snap to _whole_ _levels_.

### Step 11 · UI-15 Keep the page calm in Material 3

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | US-23: the _page_. It _comes_ _early_ _because_ every _later_ _control_ _follows_ its _rules_ |

**In plain words.** The _page_ _follows_ Material 3, Google's _design_ _system_, and _keeps_ it _quiet_. _Material_'s tonal _colours_, _built_ from _one_ _blue_, _dress_ the _surfaces_; the _places_ _alone_ _carry_ _strong_ _colour_; and, _once_ the _pins_ have _dropped_ in, _things_ _move_ _only_ when you do _something_.

> **Story.** **As** a _visitor_, **I want** a _familiar_, _calm_ _page_ on which _only_ the _places_ _stand_ out, **so that** I can _use_ it _without_ _learning_ it and _look_ at the _map_ for a _long_ _time_ _without_ strain.

**Acceptance criteria**

1. **Given** the _page_, **then** its _colours_ _come_ from Material 3's _colour_ _roles_, _built_ from the _action_ _blue_ `#1F5FA8`; the _group_ _colours_ _appear_ _only_ on _pins_ and clusters.
2. **Given** _any_ _text_, **then** it _uses_ Roboto in _Material_'s _type_ _scale_; **given** _any_ icon, **then** it _comes_ from _Material_ Symbols, _rounded_.
3. **Given** a _surface_, **then** it _follows_ _Material_'s elevation: the _scale_ _bar_ and the coordinates _sit_ at _level_ 1; the _zoom_ _buttons_, the compass and clusters at _level_ 2. Every _surface_ is _solid_, with no see-through _effect_.
4. **Given** a _shape_, **then** it _follows_ _Material_'s _corners_: 12 pixels for the _zoom_ _buttons_, the _scale_ _bar_ and the coordinates, and 4 for the _name_ tooltips.
5. **Given** _any_ _control_, **when** I hover over it or _press_ it, **then** a _state_ _layer_ tints it by 8 or 12 _per_ _cent_.
6. **Given** the _page_ _loading_, **then** _each_ _pin_ _drops_ into _place_, _one_ _after_ another, in 0.3 _seconds_.
7. **Given** _any_ _other_ _movement_, **then** it _follows_ an _action_ of _mine_, _uses_ _Material_'s _emphasised_ easing and _lasts_ 0.3 _seconds_ or _less_. _Nothing_ loops.
8. **Given** a _visitor_ who has _asked_ their _system_ to _reduce_ motion, **then** _nothing_ _moves_.
9. **Given** the _map_'s _background_, **then** it _stays_ _plain_ and _quiet_, with _nothing_ _drawn_ beneath the _places_ (US-24), so the _places_ _stand_ out.

**Tests to write first**

1. 'keeps the theme in styles.css', and '_takes_ _each_ _group_'s _colour_ from the _theme_, _never_ from _code_'.
2. 'moves for no longer than 300 ms, and not at all for a visitor who asks for less'.
3. 'draws the zoom icons from Material Symbols, one glyph each'.
4. 'stops all movement when the visitor asks for reduced motion'.

**Open to talk**

- _Material_'s _dark_ _scheme_.
- _Whether_ to _build_ the _colour_ _roles_ from the _visitor_'s _own_ _system_ _colours_.

### Step 12 · UI-12 Tell place types apart without relying on colour

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | US-23: the _pins_; US-24: the _four_ _groups_ |

**In plain words.** _Each_ _group_ has a _shape_ as _well_ as a _colour_: _circle_, triangle, _square_ or _diamond_. A _reader_ who _cannot_ _tell_ the _colours_ _apart_ can _still_ _tell_ the _groups_ _apart_.

> **Story.** **As** a _visitor_ who _sees_ _colour_ poorly, **I want** _each_ _group_ _marked_ by a _shape_ as _well_ as a _colour_, **so that** I can _tell_ the _groups_ _apart_.

**Acceptance criteria**

1. **Given** a _pin_, **then** it _shows_ its _group_'s _colour_, with the _group_'s _shape_ in _white_ _inside_: a _circle_ for _Food_ and _drink_, a triangle for _Outdoors_, a _square_ for _Culture_ and a _diamond_ for _Transport_.
2. **Given** _text_ in a _group_'s _colour_ on _white_, **then** its _contrast_ is at _least_ 4.5:1.

**Tests to write first**

1. 'gives each group its own shape'.
2. 'meets 4.5:1 for text in each group colour on white'.

**Open to talk**

- The _shapes_ for _groups_ _added_ _later_.

### Step 13 · UI-19 Read distance, direction and position

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | UI-09: the _levels_ |

**In plain words.** As on a _printed_ _map_, a _scale_ _bar_ _shows_ how _far_ a _stretch_ of the _map_ spans, and a compass _shows_ which _way_ is _north_. _Beside_ the _bar_, a readout _gives_ the latitude and longitude _under_ the _pointer_.

> **Story.** **As** a _visitor_, **I want** a _scale_ _bar_, a compass and the coordinates _under_ my _pointer_, **so that** I can _judge_ _distances_, _keep_ my bearings and _say_ where a _point_ _lies_.

**Acceptance criteria**

1. **Given** _any_ _level_, **then** a _scale_ _bar_ at the _bottom_ _left_ _shows_ a _round_ _length_, 1, 2 or 5 of a _unit_, no _longer_ than 140 pixels: 500 m at _level_ 15 and 50 m at _level_ 18 over the _area_, at Macau's latitude.
2. **Given** the _bar_, **then** it has _four_ _sections_, _filled_ and _empty_ in _turn_, _marked_ '0', the halfway _figure_ and the _full_ _length_ with its _unit_.
3. **Given** a _zoom_, **then** the _bar_ _changes_ its _length_ and _figures_ _within_ 0.3 _seconds_.
4. **Given** I _click_ the _bar_, **then** it _switches_ _between_ metric (_metres_ and _kilometres_) and imperial (_feet_ and _miles_), and its _label_ _names_ the _units_ it will _switch_ to.
5. **Given** the _map_, **then** a compass at the _top_ of the _controls_ on the _right_ _points_ _north_, with a _red_ _needle_ and a _red_ 'N'.
6. **Given** a _map_ that _never_ _turns_, **then** the compass is a _sign_, not a _button_, and _screen_ _readers_ _hear_ 'Compass: north is up'.
7. **Given** the _pointer_ over the _map_, **then** the latitude and longitude _under_ it _show_ to the _right_ of the _scale_ _bar_, latitude _first_, to _two_ decimal _places_: '22.19° N, 113.55° E'. They _change_ as the _pointer_ _moves_.
8. **Given** the _pointer_ _off_ the _map_, or over the _scale_ _bar_, the compass or a _zoom_ _button_, **then** the readout _goes_.
9. **Given** a _screen_ _reader_, **then** the readout _stays_ _silent_, _since_ it _follows_ the _pointer_ _alone_.

**Tests to write first**

1. 'chooses 500 m at level 15 and 50 m at level 18', with _metres_ _per_ pixel _taken_ from _Web_ Mercator at the _area_'s latitude, 22.19° N for Macau.
2. 'switches between metric and imperial on a click'.
3. 'names north for screen readers'.
4. 'writes the point under the pointer as 22.19° N, 113.55° E, latitude first, to two decimal places'.

**Open to talk**

- _Whether_ _two_ decimal _places_, _about_ a _kilometre_ on the _ground_, is _fine_ _enough_, or a _third_, _about_ 100 _metres_, _serves_ _better_.
- _Whether_ the _map_ should _turn_; the compass would then _become_ a _button_ that _turns_ it _back_ to _north_.
- _Whether_ to _show_ both _kinds_ of _unit_ at _once_.

### Step 14 · UI-10 See crowded places merge into clusters that open as I zoom

| | |
|---|---|
| Size | 3 _points_ |
| Builds on | UI-09: the _levels_ |

**In plain words.** When _pins_ _crowd_ _together_, they merge into _one_ _round_ _marker_ with a _number_, _ringed_ with the _colours_ of the _groups_ _inside_. _Zoom_ in, and it _breaks_ _back_ into its _pins_.

> **Story.** **As** a _visitor_, **I want** _crowded_ _places_ _shown_ as _one_ _marker_ that _opens_ as I _zoom_ in, **so that** the _map_ _stays_ readable at every _level_.

**Acceptance criteria**

1. **Given** _pins_ that would overlap at the _current_ _level_, **then** they _show_ as _one_ cluster with their _number_, _ringed_ in the _colours_ of their _groups_ in proportion.
2. **Given** I _zoom_ in _far_ _enough_ for them to _part_, **then** the _pins_ _move_ out from the cluster to their _own_ _places_ _within_ 0.3 _seconds_; _zooming_ out _draws_ them _back_.
3. **Given** I _click_ a cluster, **then** the _map_ _zooms_ no _further_ than it must to _open_ it, and at _least_ to _level_ 16.
4. **Given** a _screen_ _reader_, **then** a cluster's _label_ _names_ its _places_ and _says_ that _zooming_ in _shows_ them.
5. **Given** a cluster on _screen_, **then** it _rests_ _still_, with no pulse or loop.

**Tests to write first**

1. 'merges pins that sit closer than the distance for the level'.
2. 'halves that distance at each level up'.

**Open to talk**

- The _distance_ at which _pins_ merge: 48 pixels on _screen_, _up_ to _level_ 15.

### Step 15 · UI-11 Read place names when zoomed in

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | UI-10: _names_ _stay_ _hidden_ _inside_ a cluster |

**In plain words.** _Close_ in, every _pin_ _carries_ its _name_, so you can _read_ the _map_ _without_ _clicking_.

> **Story.** **As** a _visitor_, **I want** _names_ _beside_ the _pins_ _once_ I _zoom_ in, **so that** I can _read_ the _map_ _without_ _clicking_ every _pin_.

**Acceptance criteria**

1. **Given** _level_ 16 or _above_, **then** _each_ _pin_ on _screen_ _shows_ its _name_ _beside_ it in a _dark_ _plain_ tooltip, _one_ _after_ another.
2. **Given** a _level_ _below_ 16, **then** no _pin_ _shows_ its _name_.
3. **Given** a cluster, **then** its _pins_ _show_ no _names_ until it _opens_.

**Tests to write first**

1. 'shows names from level 16, and none below'.
2. 'shows names from level 16, and none inside a cluster', _against_ the _style_.

### Step 16 · UI-17 Keep place names from overlapping

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | UI-11: the _names_ it _places_ |

**In plain words.** On a _crowded_ _street_, _names_ would _pile_ on _top_ of _one_ another. The _map_ _places_ them _one_ at a _time_, in a _fixed_ _order_, _tries_ _each_ _name_ on every _side_ of its icon, and _leaves_ out _any_ _name_ that _still_ has no _room_. The icon _stays_; its _name_ _waits_ until the _reader_ _zooms_ in.

> **Story.** **As** a _visitor_, **I want** _place_ _names_ that _never_ overlap, **so that** I can _read_ a _crowded_ _street_ at a glance.

**Acceptance criteria**

1. **Given** _any_ _level_, **then** no _name_ overlaps another _name_ or another icon, _across_ every _layer_.
2. **Given** _two_ _places_ whose _names_ would clash, **then** a _fixed_ _order_ _decides_ which _keeps_ its _name_: _between_ _layers_, the _order_ _set_ in the _map_'s _style_; _within_ a _layer_, the _places_' ids, so the _same_ _name_ _wins_ every _time_.
3. **Given** a _name_ with no _room_ on its _first_ _side_, **then** the _map_ _tries_ the _right_, the _left_, _above_ and _below_ _before_ it _leaves_ the _name_ out.
4. **Given** a _name_ _left_ out, **then** its icon _still_ _shows_, and the _name_ _appears_ _once_ _zooming_ in _makes_ _room_.
5. **Given** a _crowded_ _area_, **then** _places_ _stay_ in clusters _below_ _level_ 16 (UI-10); from _level_ 16 every _place_ _shows_ its icon, and the _fixed_ _order_ _decides_ _only_ which _names_ _win_ the _space_.
6. **Given** a _name_ _near_ the _edge_ of a _tile_, **then** it _gives_ _way_ to its _neighbours_ as _one_ in the _middle_ does.

**Tests to write first**

1. 'gives every place layer four anchors and optional text', _against_ the _style_.
2. The _tile_ _query_ _orders_ _each_ _table_'s _places_ by its _primary_ _key_, as its _approved_ _text_ _shows_.
3. A ***browser test*** with the _map_'s collision _boxes_ _shown_, whose _screenshots_ at _levels_ 16, 18 and 20 over the busiest _street_ _match_ _approved_ _images_.

**Open to talk**

- The _order_ of _layers_ in the _map_'s _style_, which _decides_ whose _names_ _win_ _between_ _layers_.
- The _space_ _kept_ _around_ _each_ _name_.


## Part C · Tiles that cost less

All _three_ _stories_ _follow_ _one_ _idea_: a _tile_ the _service_ has _built_ should _cost_ it _nothing_ to _serve_ _again_, and _little_ to _send_. Nginx does _most_ of the _work_ (ADR 0008); the _service_ _sets_ the _headers_ that _tell_ Nginx and the _browser_ what they may _keep_.

### Step 17 · N-18 Let a browser keep a tile

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | US-07: the _tile_ |

**In plain words.** A _browser_ _asks_ for the _same_ _tile_ every _time_ the _visitor_ _pans_ _back_ over it, and every _request_ _costs_ a _query_. HTTP _lets_ the _service_ _say_ how _long_ an _answer_ _stays_ _good_: a `Cache-Control` _header_ with `max-age`, in _seconds_. For that _long_ the _browser_ _keeps_ its _copy_ and _asks_ _nothing_. The _places_ _change_ _only_ when a _new_ _seed_ _script_ _runs_, so a _minute_ is _safe_: _new_ _places_ _show_ on every _screen_ _within_ a _minute_.

> **Story.** **As** the _maintainer_, **I want** every _tile_ to _tell_ the _browser_ how _long_ to _keep_ it, **so that** a _visitor_ who _pans_ _back_ and forth _costs_ the _service_ _one_ _query_ a _tile_ a _minute_, not _one_ a _pan_.

**Acceptance criteria**

1. **Given** a _tile_ _answered_ 200, **then** it _carries_ `Cache-Control: public, max-age=N`, where N is `Tiles.CacheSeconds` in `tessera.json`; the _shipped_ _file_ and the _default_ both _set_ 60.
2. **Given** `Tiles.CacheSeconds` _set_ to 0, **then** a _tile_ _carries_ `Cache-Control: no-cache`, so a _browser_ _checks_ _before_ it _reuses_ a _copy_, and Nginx _keeps_ _nothing_.
3. **Given** _any_ _error_ _answer_, such as the 404 of US-07, **then** it _carries_ `Cache-Control: no-store`, so no _browser_ _keeps_ a _problem_ _document_.
4. **Given** a _new_ _seed_ _script_, **when** `max-age` _seconds_ _pass_, **then** every _browser_ _asks_ _again_ and _sees_ the _new_ _places_.
5. **Given** a _setting_ _below_ 0, or not a _whole_ _number_, **then** the _service_ _refuses_ to _start_ and _names_ the _setting_ (US-00 c4).

**Tests to write first**

1. **The header follows the setting.** A ***pure function*** _turns_ 60 into `public, max-age=60` and 0 into `no-cache`; the _settings_ _check_ _refuses_ -1.
2. **Through the real service.** A _tile_ _carries_ `public, max-age=60`; a 404 _carries_ `no-store`.
3. **The map asks once.** In a _browser_ _test_, the _map_ _pans_ _away_ from the _home_ _view_ and _back_ _within_ the _minute_, and _asks_ for _each_ of the _home_ _view_'s _tiles_ _once_, not _twice_.

**Open to talk**

- _Whether_ the _map_'s _page_ and _files_ _keep_ their _own_ _rules_: the _page_ `no-cache`, _each_ _built_ _file_ a _year_, _since_ its _name_ _changes_ with its _content_.

**Critical to success**

- **One number everywhere.** _Browsers_ and Nginx _keep_ a _tile_ for the _same_ `max-age`, so _new_ _places_ _show_ on every _screen_ _within_ that _time_.

### Step 18 · N-19 Answer a kept tile without a query

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | N-18: `max-age`; US-07: the _tile_ |

**In plain words.** When `max-age` _passes_, the _browser_ _asks_ _again_, but it _still_ _holds_ its _copy_. HTTP _lets_ it _say_ so: it _sends_ the _copy_'s _label_, an **ETag**, in an `If-None-Match` _header_, and the _answer_ may be 304 ('Not Modified') with no _body_, which _means_ 'yours is still good'. The _service_ _makes_ _each_ _tile_'s ETag from its _bytes_. Nginx _keeps_ _each_ _tile_ for `max-age`, _answers_ a _second_ _request_ from its _kept_ _copy_, and _answers_ 304 when the _tags_ _match_.

> **Story.** **As** the _maintainer_, **I want** a _tile_ a _browser_ _already_ _holds_ to _cost_ _nothing_, and _each_ _tile_ to be _built_ _once_ a _minute_ at _most_, **so that** a _crowd_ _opening_ the _map_ _costs_ the _database_ _one_ _query_ a _tile_, not _one_ a _visitor_.

**Acceptance criteria**

1. **Given** a _tile_ _answered_ 200, **then** it _carries_ a _weak_ `ETag`, `W/"…"`, _made_ from the _tile_'s _bytes_ _alone_, so the _same_ _bytes_ _carry_ the _same_ _tag_ on _any_ _machine_ and _after_ a _restart_. It is _weak_ _because_ N-20's _packed_ _form_ is the _same_ _tile_.
2. **Given** a _request_ whose `If-None-Match` _equals_ the _tile_'s _tag_, **then** the _answer_ is 304 with no _body_.
3. **Given** a _tile_ _built_ _within_ the _last_ `Tiles.CacheSeconds` _seconds_, **when** _any_ _request_ for it _arrives_, **then** Nginx _answers_ from its _kept_ _copy_, _says_ `X-Cache-Status: HIT`, and _runs_ no _query_.
4. **Given** _kept_ _tiles_, **then** _together_ they _take_ no more than 64 MB, Nginx's `max_size`; when the _room_ is _full_, the _tiles_ _used_ _least_ _go_ _first_.
5. **Given** a _new_ _seed_ _script_, **when** `max-age` _seconds_ _pass_, **then** every _kept_ _tile_ has _expired_, and the _next_ _request_ _builds_ it _again_.

**Tests to write first**

1. **The tag follows the bytes.** The _same_ _bytes_ _give_ the _same_ _tag_, and _two_ _tiles_ that _differ_ _give_ _different_ _tags_.
2. **A second ask is a hit.** _Through_ Nginx, a _second_ _request_ for _one_ _tile_ _says_ `X-Cache-Status: HIT`.
3. **A matching tag is 304.** _Through_ Nginx, a _request_ that _sends_ the _first_ _answer_'s _tag_ _back_ _gets_ 304 and no _body_.

**Critical to success**

- **One clock.** Nginx's _kept_ _tiles_ and the _browser_'s _share_ `Tiles.CacheSeconds`, so _new_ _places_ _show_ on every _screen_ _within_ that _time_.
- **Read the cache's own word.** `X-Cache-Status` _says_ _whether_ a _query_ _ran_; _timing_ _alone_ _proves_ _nothing_.

### Step 19 · N-20 Send tiles and the map's files packed

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | US-07: the _tile_; US-23: the _map_'s _files_; N-19: the ETag |

**In plain words.** The _map_'s _own_ _code_ is _about_ a megabyte of JavaScript, and a _tile_ is _some_ kilobytes. Both shrink when **gzip** _packs_ them: a _tile_ by _about_ _half_, _text_ by _two_ _thirds_ or more. A _browser_ _says_ `Accept-Encoding: gzip` in every _request_; Nginx then _answers_ with the _packed_ _body_ and `Content-Encoding: gzip`, and the _browser_ unpacks it _before_ it _reads_ it.

> **Story.** **As** a _visitor_, **I want** the _map_ and its _tiles_ to _arrive_ _packed_, **so that** the _map_ _opens_ _sooner_ and _each_ _pan_ _costs_ the _network_ _less_.

**Acceptance criteria**

1. **Given** a _request_ with `Accept-Encoding: gzip` for a _tile_, the _page_ or _one_ of the _map_'s _files_, **when** the _answer_ is 200, **then** its _body_ is gzip-packed, it _carries_ `Content-Encoding: gzip`, and the unpacked _bytes_ _equal_ the _plain_ _answer_'s.
2. **Given** a _request_ _without_ `Accept-Encoding`, **then** the _body_ _travels_ _plain_, with no `Content-Encoding`.
3. **Given** either _answer_, **then** it _carries_ `Vary: Accept-Encoding`, so no _browser_ or proxy _hands_ a _packed_ _body_ to a _client_ that _asked_ for _plain_.
4. **Given** a 304 (N-19), **then** the ***weak ETag*** _matches_ whichever _form_ the _browser_ _holds_, _since_ both _forms_ are the _same_ _tile_.

**Tests to write first**

1. **A tile packs and unpacks.** _Through_ Nginx, a _packed_ _tile_ unpacks to the _plain_ _answer_'s _bytes_.
2. **The map's script shrinks.** The _map_'s _main_ _script_ _arrives_ _packed_, at _under_ _half_ its _plain_ _size_.
3. **Plain on request.** _Without_ `Accept-Encoding`, the _body_ is _plain_, and `Vary` is _still_ there.
4. **The map still opens.** The _browser_ _tests_ _pass_ _through_ Nginx; a _browser_ _always_ _sends_ `Accept-Encoding`.

**Open to talk**

- Brotli _beside_ gzip: _browsers_ _prefer_ it, and it _packs_ _text_ _tighter_.
- _Whether_ a _body_ _under_ a kilobyte _travels_ _plain_, _since_ packing it _gains_ _nothing_.

**Critical to success**

- **Pack lightly.** Nginx _packs_ at _level_ 1: _fast_, and a _tile_ _gains_ _little_ from more.

## Part D · Steady when the database is not

_Seven_ _stories_ _keep_ the _service_ steady when the _database_ is _slow_, _busy_, _late_ or _down_, so the _service_ and the _database_ may _start_ in _any_ _order_. Their _settings_ _come_ _first_, _because_ the _later_ _stories_ _read_ them.

### Step 20 · N-21 Read every setting from one file

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | US-00: the _settings_; N-18: `Tiles.CacheSeconds` |

**In plain words.** Every _setting_ but the _database_'s _address_ _lives_ in _one_ _file_, `tessera.json`, _beside_ the _program_, _grouped_ in _sections_. _Each_ _time_ _carries_ its _unit_ in its _name_, such as `Tiles.CacheSeconds`, so no _one_ _reads_ _seconds_ as _milliseconds_. The _database_'s _address_ _comes_ from `DATABASE_URL`, the _name_ pg_tileserv _gives_ it, so no _password_ _sits_ in the _file_.

> **Story.** **As** the _maintainer_, **I want** every _setting_ in _one_ _file_, _each_ _named_ with its _unit_, and _any_ of them overridden from the _environment_, **so that** I can _read_ and _change_ the _service_'s _behaviour_ _without_ _reading_ its _code_.

**Acceptance criteria**

1. **Given** `tessera.json`, **then** its _keys_ _sit_ in _five_ _sections_, `Database`, `Tiles`, `Map`, `Catalogue` and `Area`, _beside_ ASP.NET Core's _own_ `Urls` and `Logging`, and _each_ _time_ _carries_ its _unit_ in its _name_.
2. **Given** a _key_ _left_ out, or the _file_ _missing_, **then** _each_ _key_ _takes_ its _default_.
3. **Given** an _environment_ variable that _names_ a _key_, with `__` for _each_ _step_ _down_, such as `Tiles__CacheSeconds=0`, **then** it _overrides_ the _file_.
4. **Given** a _key_ a _section_ does not _read_, or a _value_ _outside_ its _limits_, **then** the _service_ _refuses_ to _start_ and _names_ every such _problem_ at _once_ (US-00 c4).
5. **Given** the _shipped_ _file_, **then** it _holds_ no _password_ and _listens_ on 127.0.0.1 _alone_.

**Tests to write first**

1. **The shipped file reads.** Its _values_ _reach_ their _records_.
2. **Every key left out takes its default.** An _empty_ _file_ _runs_ on the _defaults_.
3. **A variable overrides a key.**
4. **An unknown key is refused,** and _named_.
5. **No password, and this machine alone.** The _shipped_ _file_ _holds_ no _password_, and its `Urls` _names_ 127.0.0.1.

**Open to talk**

- _Whether_ a _misspelt_ _section_ _name_, which _passes_ and _leaves_ its _section_ at the _defaults_, should _stop_ the _start_ _too_.

### Step 21 · N-22 Ask the browser to wait when the database cannot answer

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | N-21: the _pool_'s _limits_; US-07: _problem_ _documents_ |

**In plain words.** When the _database_ is _down_, _busy_ or _slow_, the _fault_ is not the _service_'s, and it will _pass_. HTTP's 503, 'Service Unavailable', with `Retry-After`, _says_ _exactly_ that: 'try again in 5 seconds'. A 500 _says_ 'something is broken'.

> **Story.** **As** a _map_ developer, **I want** a _passing_ _database_ _problem_ to _answer_ 503 with `Retry-After`, and no _query_ to _run_ for _ever_, **so that** my _map_ _waits_ and _tries_ _again_ _rather_ than _showing_ a _fault_.

**Acceptance criteria**

1. **Given** a _database_ that _cannot_ be _reached_, _refuses_ a _connection_, has no _room_ for another, or is _stopping_ or _starting_, **when** a _tile_ is _asked_ for, **then** the _answer_ is 503 with `Retry-After: 5` and `Cache-Control: no-store`, as a _problem_ _document_.
2. **Given** a _query_ that _runs_ _past_ `Database.QueryTimeoutSeconds` (10), or a _wait_ for a _connection_ _past_ `Database.ConnectionWaitSeconds` (3), **then** the _answer_ is 503.
3. **Given** a _failure_, **then** no _copy_ of it is _kept_; the _next_ _request_ _asks_ _again_.
4. **Given** _any_ _other_ _database_ _failure_, **then** the _answer_ _stays_ 500.

**Tests to write first**

1. **Which failures wait.** SQLSTATE `08006`, `57014`, `57P01` and `53300` _wait_; `42501` and `XX000` do not.
2. **An unreachable database answers 503**, with `Retry-After: 5`.
3. **A slow query answers 503 within its time.** A _view_ that _sleeps_ _three_ _seconds_, with a _limit_ of _one_, _answers_ in _under_ _three_.

### Step 22 · N-23 Serve a newly granted table without a restart

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | US-03: the catalogue; N-22: 503 |

**In plain words.** The _service_ _rereads_ its _list_ of _layers_ every `Catalogue.RefreshSeconds`, 30 by _default_, so a _grant_ _shows_ _within_ _half_ a _minute_. It _also_ _starts_ _before_ the _database_ _answers_, and _asks_ _each_ _browser_ to _wait_ until the _list_ _loads_, so the _two_ may _start_ in _any_ _order_.

> **Story.** **As** a _data_ _publisher_, **I want** a _table_ I _grant_ to be _served_ _within_ _half_ a _minute_, and the _service_ to _start_ _whether_ or not the _database_ is _up_, **so that** I _never_ _restart_ it by _hand_.

**Acceptance criteria**

1. **Given** a _table_ _granted_ _after_ start-up, **then** it is _served_ _within_ `Catalogue.RefreshSeconds` _seconds_.
2. **Given** a _grant_ _revoked_, **then** the _layer_ _leaves_ the _list_ _within_ the _same_ _time_, and a _new_ _tile_ of it _answers_ 404; _tiles_ _already_ _kept_ _stay_ in _browsers_ and in Nginx for _up_ to `Tiles.CacheSeconds`.
3. **Given** the _database_ _down_ at _start_, **then** the _service_ _starts_, _serves_ the _map_, and _answers_ _each_ _tile_ 503 until the _list_ _loads_; it _tries_ every 5 _seconds_.
4. **Given** a _reload_ that _fails_, **then** the _last_ _good_ _list_ _stays_.

**Tests to write first**

1. **No list until the first load; the last good list through a failure.**
2. **A grant and a revoke within one refresh**, _through_ the _real_ _database_.
3. **The program starts with the database down** and _answers_ 503 with `Retry-After: 5`.

### Step 23 · N-24 Serve a crowd from one query

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | N-19: _kept_ _tiles_ |

**In plain words.** At _nine_ o'_clock_ a _crowd_ _opens_ the _map_, and _hundreds_ of _requests_ for the _same_ _tile_ _arrive_ _before_ _any_ _copy_ is _kept_. Nginx _lets_ the _first_ _build_ it, and the _rest_ _wait_ for its _answer_ (`proxy_cache_lock`).

> **Story.** **As** the _maintainer_, **I want** _one_ _query_ for _each_ _tile_ however _many_ _ask_ at _once_, **so that** a _crowd_ _cannot_ _fill_ the _database_'s _pool_.

**Acceptance criteria**

1. **Given** _many_ _requests_ for _one_ _unbuilt_ _tile_ at _once_, **then** _one_ _query_ _runs_, and every _request_ _receives_ the _tile_.
2. **Given** _one_ _browser_ that _leaves_ _while_ it _waits_, **then** the _query_ _goes_ on for the _rest_ (`proxy_ignore_client_abort`).
3. **Given** a _query_ that has _ended_, **then** a _later_ _request_ _finds_ the _kept_ _copy_.

**Tests to write first**

1. **Twenty requests, one query.** _Through_ Nginx, _twenty_ _requests_ at _once_ for a _tile_ of a _view_ that _sleeps_ _two_ _seconds_ all _answer_ 200 in _under_ _four_. _Without_ the _lock_, _twenty_ _queries_ would _share_ the _two_ _copies_' _eight_ _connections_ and _some_ would _answer_ 503.

### Step 24 · N-25 Tell a watcher the service is well

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | N-22 and N-23 |

**In plain words.** A ***health check*** is an _address_ a _watcher_, a _script_ or a _person_, _asks_ to _learn_ _whether_ the _service_ can do its _job_. pg_tileserv _answers_ at `/health`; so does Tessera.

> **Story.** **As** the _maintainer_, **I want** `/health` to _say_ `UP` _only_ when the _service_ can _serve_ _tiles_, **so that** a _script_ can _wait_ for it and a _watcher_ can _see_ it _fail_.

**Acceptance criteria**

1. **Given** the _list_ of _layers_ _loaded_ and the _database_ _answering_ _within_ a _second_, **then** `/health` _answers_ 200 `{"status":"UP"}`.
2. **Given** the _database_ _failing_, **then** it _answers_ 503 `{"status":"DOWN"}`; **given** _only_ the _list_ _unloaded_, 503 `{"status":"OUT_OF_SERVICE"}`. It _names_ no _fault_; the _log_ _holds_ the _detail_.
3. **Given** either _answer_, **then** it _carries_ `Cache-Control: no-store`.
4. **Given** `npm start`, **then** it _says_ the _service_ is _ready_ _only_ _once_ `/health`, _asked_ _through_ Nginx, _says_ `UP`.

**Tests to write first**

1. **UP on the running service.**
2. **DOWN with the database down**, _within_ a _second_, and no _fault_ _named_.

### Step 25 · N-26 Wait in the order of arrival

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | N-22: the _pool_'s _limits_; N-25: `/health` |

**In plain words.** When _many_ _people_ _pan_ the _map_ at _once_, _requests_ for _different_ _tiles_ _wait_ for a _free_ _database_ _connection_. The _first_ to _arrive_ should be the _first_ _served_, or an _unlucky_ _request_ _loses_ its _turn_ _again_ and _again_ and _runs_ out of _time_. Npgsql's _pool_ _keeps_ this _order_ _itself_, so the _service_ _needs_ _only_ _tests_ that _prove_ it. The _health_ _check_'s _ping_ _waits_ in the _same_ _queue_, for a _second_ at _most_.

> **Story.** **As** the _maintainer_, **I want** _queries_ to _wait_ for a _connection_ in the _order_ they _arrive_, and the _health_ _check_ to _give_ _up_ _after_ a _second_ in the _same_ _queue_, **so that** no _request_ _waits_ _far_ _longer_ than the _rest_, and `/health` _says_ `DOWN` when no _tile_ could be _served_ promptly.

**Acceptance criteria**

1. **Given** every _connection_ _busy_, **then** the _pool_ _lends_ the _next_ _free_ _one_ to the _request_ that has _waited_ _longest_.
2. **Given** a _query_ _sent_ with its _request_'s _own_ _cancel_ _signal_, and that _request_ _cancelled_ _while_ the _query_ _waits_, **then** the _query_ _leaves_ the _queue_ at _once_. A _tile_ _query_ is not such a _query_: _see_ _Known_ _limit_.
3. **Given** every _connection_ _busy_ for more than a _second_, **then** the _ping_ _gives_ _up_ _within_ _about_ a _second_, `/health` _says_ `DOWN`, and the _ping_ _leaves_ the _queue_, so it _never_ _takes_ a _connection_ a _tile_ is _waiting_ for.

**Tests to write first**

1. **Five tiles, one connection.** _Five_ _different_ _tiles_ of a _view_ that _sleeps_ 0.4 _seconds_, _asked_ for 50 ms _apart_, _finish_ in the _order_ _asked_.
2. **The ping behind a busy pool.** With the _only_ _connection_ _held_ for _three_ _seconds_, the _ping_ _gives_ _up_ _within_ a _second_ and a _half_; _once_ the _hold_ _ends_, the _next_ _ping_ _answers_.
3. **A cancelled wait.** A _query_ _cancelled_ _after_ 200 ms _leaves_ _within_ a _second_, not _after_ the _pool_'s 30.

**Known limit.** A _tile_ _query_ _runs_ on the _service_'s _stop_ _signal_, not the _browser_'s. When a _person_ _pans_ _away_ _before_ a _tile_ _arrives_, the _map_ _cancels_ the _request_, but the _query_ _still_ _waits_ its _turn_, _runs_, and _lands_ in Nginx's ***cache***. _Nothing_ is _lost_: a _person_ who _pans_ _back_ _gets_ the _tile_ at no _cost_. On a _busy_ _day_, _though_, _abandoned_ _queries_ _hold_ _connections_ that _tiles_ _still_ on _screen_ _need_. At 700 _tiles_ a _second_, _about_ a _third_ of _capacity_, no _request_ is _given_ _up_; _only_ _under_ _overload_ do _requests_ _wait_ _long_ _enough_ to be _given_ _up_, and there the _cure_ is a _limit_ on _load_ (ADR 0007), not a _cancel_.

**Open to talk**

- _Whether_ a _watcher_ that _restarts_ the _service_ on `DOWN` should _allow_ for _busy_ _hours_, _since_ a _pool_ _busy_ for over a _second_ _says_ `DOWN`.

### Step 26 · N-28 Serve kept tiles while the database hangs

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | N-19: _kept_ _tiles_; N-22: 503 with `Retry-After` |

**In plain words.** A _database_ that _hangs_, _behind_ a _broken_ _network_ or a _stuck_ _disk_, _refuses_ _nothing_: _each_ _request_ _waits_ out its _limits_. Nginx _keeps_ the _map_ _working_ meanwhile. It _serves_ every _tile_ it _holds_ _while_ the _service_ or the _database_ _cannot_ _answer_, and an _expired_ _tile_ _while_ it fetches a _fresh_ _one_ _behind_ it.

> **Story.** **As** the _maintainer_, **I want** _kept_ _tiles_ _served_ at _once_ _while_ the _database_ _hangs_ or the _service_ is _down_, and every _other_ _tile_ to _answer_ 503, **so that** the _map_ _stays_ usable and _browsers_ _retry_ _rather_ than _hang_.

**Acceptance criteria**

1. **Given** the _service_ or the _database_ _down_ or _hanging_, **then** Nginx _serves_ every _tile_ it _holds_ at _once_, _even_ _past_ its `max-age`.
2. **Given** an _expired_ _tile_, **then** Nginx _serves_ it _while_ it fetches a _fresh_ _copy_ _behind_ it.
3. **Given** the _service_ _down_ and a _tile_ Nginx _never_ _held_, **then** Nginx _answers_ 503 with `Retry-After: 5`, as a _problem_ _document_.
4. **Given** the _database_ _hanging_ and a _tile_ Nginx _never_ _held_, **then** the _service_ _waits_ _up_ to `Database.ConnectionWaitSeconds` or `Database.QueryTimeoutSeconds`, then _answers_ 503 (N-22).
5. **Given** the _database_ _down_, **then** `/health` _answers_ `DOWN` _within_ a _second_ (N-25).

**Tests to write first**

1. **Kept tiles with the service down.** _Through_ Nginx, a _tile_ _asked_ for _once_ _still_ _arrives_ with the _service_ _stopped_, and a _tile_ _never_ _kept_ _answers_ 503.
2. **No database at start.** `/health` _answers_ _within_ a _second_, and a _tile_ _within_ _half_ a _second_.

**Known limit.** _Requests_ _already_ _waiting_ for a _connection_ when the _database_ _freezes_ _wait_ out their _limits_, so the _first_ _seconds_ _bring_ a _wave_ of _slow_ 503s for _tiles_ Nginx _never_ _held_.

## Part E · Nginx guards the door

_Three_ _stories_ _close_ the _door_ to strangers, _keep_ _any_ _one_ _office_ _machine_ from _crowding_ out the _rest_, and _keep_ _tiles_ _coming_ when _one_ _copy_ of the _service_ _fails_. Nginx does the _work_ (ADR 0007, ADR 0008); _each_ _story_ _needs_ US-35's _mirrored_ _networking_, so Nginx _sees_ which _machine_ _asks_.

### Step 27 · S-07 Answer office machines alone

| | |
|---|---|
| Size | 1 _point_ |
| Builds on | US-35: Nginx _sees_ _each_ _machine_'s _own_ _address_ |

**In plain words.** Every _office_ _machine_ _sits_ on _one_ **network**, a _range_ of _addresses_ _written_ as an _address_, a _slash_ and a **prefix**, such as `192.168.10.0/24`: the prefix _counts_ the leading bits every _address_ in the _range_ _shares_, so `/24` _means_ the _first_ _three_ _numbers_. Nginx _answers_ a _machine_ _inside_ that _range_, and the _Windows_ _machine_ _itself_, and _refuses_ _any_ _other_ with 403 ('Forbidden') _before_ _any_ _other_ _rule_ _runs_.

> **Story.** **As** the _maintainer_, **I want** Nginx to _answer_ _office_ _machines_ _alone_, **so that** a _machine_ _outside_ the _office_ _cannot_ _use_ the _service_.

**Acceptance criteria**

1. **Given** `TESSERA_OFFICE_NETWORK`, such as `192.168.10.0/24`, **when** a _machine_ _inside_ it _asks_, **then** Nginx _answers_ it.
2. **Given** a _machine_ _outside_ it, **when** it _asks_ for _anything_, **then** Nginx _answers_ 403 as a _problem_ _document_ (US-07), with `Cache-Control: no-store`, and _never_ _passes_ the _request_ to the _service_.
3. **Given** the _machine_ Tessera _runs_ on, **then** Nginx _answers_ it _too_, so `npm start` can _ask_ `/health`.
4. **Given** `TESSERA_OFFICE_NETWORK` _missing_, _misspelt_, or with a prefix _below_ 8, such as `0.0.0.0/0`, which _lets_ in every _address_, **then** `npm start` _refuses_ to _start_ and _names_ it.

**Tests to write first**

1. **The setting, checked purely.** `192.168.10.0/24` and `127.0.0.4/30` _pass_; `0.0.0.0/0`, `10.0.0.0/7`, a _range_ with no prefix and a _number_ _above_ 255 _fail_.
2. **A stranger is refused.** _Through_ Nginx, with the _office_ _set_ to `127.0.0.4/30`, a _tile_ _asked_ from `127.0.0.2` _answers_ 403 as a _problem_ _document_, and the _same_ _tile_ _asked_ from `127.0.0.5` _answers_ 200. Linux _answers_ every `127.x.x.x` _address_ on the _machine_ _itself_, so _one_ _machine_ _plays_ both.

**Open to talk**

- _Whether_ the _office_ _needs_ more than _one_ _range_, such as a VPN's _beside_ its _own_.

**Critical to success**

- **Refuse first.** The _check_ _runs_ _before_ the _cache_, so a stranger _gets_ no _kept_ _tile_ either.

### Step 28 · S-30 Cap what one visitor may ask

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | S-07: _only_ _office_ _machines_ _reach_ Nginx; N-19: _kept_ _tiles_ |

**In plain words.** _One_ _busy_ _browser_, or a _script_ in a loop, can _ask_ for _tiles_ _faster_ than the _database_ can _build_ them, and every _other_ _visitor_ _waits_. Nginx _counts_ _each_ _office_ _machine_'s _requests_ by its _address_. It _lets_ a _fresh_ _view_'s burst _through_ at _once_, then _holds_ _each_ _machine_ to a steady _rate_, and _answers_ _anything_ beyond with 429 ('_Too_ _Many_ _Requests_'), which _asks_ the _browser_ to _wait_ a _second_.

> **Story.** **As** a _visitor_, **I want** no _other_ _office_ _machine_ to _crowd_ me out, **so that** my _map_ _stays_ _quick_ on a _busy_ _day_.

**Acceptance criteria**

1. **Given** _one_ _office_ _machine_, **then** Nginx _lets_ it _ask_ for 100 _tiles_ a _second_, with a burst of 300 at _once_: _one_ _fresh_ _view_ of _nine_ _layers_ _asks_ for _about_ a _hundred_.
2. **Given** a _machine_ _past_ that burst, **then** its _extra_ _tile_ _requests_ _answer_ 429 as a _problem_ _document_, with `Retry-After: 1` and `Cache-Control: no-store`, until its _rate_ _falls_ _back_ _within_ the _limit_.
3. **Given** _one_ _machine_, **then** Nginx _handles_ at _most_ 32 of its _requests_ at _once_, and _answers_ the 33rd with 429.
4. **Given** _one_ _machine_ at its _limit_, **then** every _other_ _machine_'s _requests_ _pass_.
5. **Given** the _machine_ Tessera _runs_ on, **then** no _limit_ _applies_ to it, so its _own_ _checks_ _never_ _meet_ _one_.

**Tests to write first**

1. **A flood meets the limit.** _Through_ Nginx, _one_ _address_ _asks_ for 600 _kept_ _tiles_ over _eight_ _connections_: at _least_ 300 _answer_ 200, the _rest_ 429 with `Retry-After: 1`.
2. **Others pass.** _Straight_ _after_ the flood, another _office_ _address_ _gets_ 200.
3. **Thirty-two at once.** _Through_ Nginx, forty _requests_ at _once_ from _one_ _address_, for a _tile_ that _takes_ _two_ _seconds_ to _build_, _leave_ at _least_ _eight_ _answered_ 429 and the _rest_ 200.

**Open to talk**

- The _numbers_. The _service_ _builds_ _about_ 2,400 _tiles_ a _second_ from the _database_, on _two_ _processors_, so 24 _machines_ at the _full_ _rate_ could _fill_ it.
- _Whether_ the _map_ should _ask_ _again_ _after_ a 429 or a 503, _rather_ than _wait_ for the _view_ to _move_.

**Critical to success**

- **Count by machine, not by connection.** A _browser_ _opens_ _several_ _connections_; the _limit_ _follows_ the _address_, so it _cannot_ be dodged by _opening_ more.

### Step 29 · N-29 Keep serving when one copy of the service fails

| | |
|---|---|
| Size | 2 _points_ |
| Builds on | N-25: `/health`; N-28: _kept_ _tiles_ _while_ the _database_ _hangs_ |

**In plain words.** A _program_ can _crash_, or _hang_ on a _fault_ in its _own_ _code_. With _one_ _copy_ of the _service_, every _tile_ Nginx does not _hold_ then _fails_ until _someone_ _restarts_ it. `npm start` _runs_ _two_ _copies_ _instead_, on _two_ _ports_, and Nginx _shares_ the _requests_ _between_ them. When _one_ _copy_ _fails_, Nginx _sends_ its _requests_ to the _other_; when a _copy_ _stops_, `npm start` _starts_ it _again_. Both _copies_ _run_ on the _one_ _machine_, so they _guard_ _against_ a _failed_ _program_, not a _failed_ _machine_.

> **Story.** **As** a _visitor_, **I want** _tiles_ to _keep_ _coming_ when _one_ _copy_ of the _service_ _fails_, **so that** a _crash_ in the _service_ _never_ blanks my _map_.

**Acceptance criteria**

1. **Given** `npm start`, **then** it _runs_ _two_ _copies_ of the _service_, on `TESSERA_SERVICE_PORT`, 7800 by _default_, and the _next_ _port_, and Nginx _shares_ the _requests_ _between_ them in _turn_.
2. **Given** _one_ _copy_ _stopped_ or _crashed_, **when** a _request_ _reaches_ it, **then** Nginx _passes_ the _request_ to the _other_ _copy_, and the _visitor_ _gets_ the _answer_.
3. **Given** _three_ _failed_ _requests_ to _one_ _copy_ _within_ _ten_ _seconds_, **then** Nginx _rests_ that _copy_ for _ten_ _seconds_ and _sends_ _everything_ to the _other_.
4. **Given** a _copy_ that _stops_ on its _own_, **then** `npm start` _starts_ it _again_ _after_ a _second_, and _doubles_ the _wait_ _after_ _each_ _quick_ _failure_, _up_ to 30 _seconds_. A _copy_ that _ran_ for a _minute_ _counts_ as _healthy_, and its _next_ _restart_ _waits_ a _second_ _again_.
5. **Given** the _database_ _down_, **then** neither _copy_ is _rested_: both _answer_ 503 (N-22), which _counts_ as an _answer_, not a _failure_.
6. **Given** both _copies_ _down_, **then** Nginx _serves_ every _tile_ it _holds_ and _answers_ the _rest_ 503 (N-28).
7. **Given** Ctrl+C, **then** `npm start` _stops_ both _copies_, and _starts_ neither _again_.

**Tests to write first**

1. **The wait, purely.** The _restart_ _waits_ 1, 1, 2, 4, 8 and 16 _seconds_ _after_ 0 to 5 _quick_ _failures_, and _never_ more than 30.
2. **One copy stopped.** _Through_ Nginx, with the _first_ _copy_ _stopped_, _six_ _tiles_ Nginx _never_ _held_ _answer_ 200, and `/health` _says_ `UP`.
3. **Both stopped.** Then, with the _second_ _copy_ _stopped_ _too_, a _kept_ _tile_ _still_ _arrives_, and a _tile_ _never_ _kept_ _answers_ 503.
4. **The restart, with fakes.** `scripts/lib/supervise.ts` _drives_ _fake_ _copies_ on a _fake_ _clock_: it _waits_ 1, 2, 4 and 8 _seconds_ _after_ _quick_ _failures_, a _second_ _again_ _after_ a _copy_ that _ran_ for a _minute_, and _starts_ _nothing_ _once_ _told_ to _stop_.

**Open to talk**

- A ***start-up task*** in _Windows_ _runs_ `npm start` when the _machine_ _switches_ on, and _writes_ its _output_ to `start.log`; `npm run boot-task` _prints_ the _steps_ that register it. It _needs_ a _first_ _run_ on the _office_ _machine_. _Running_ _each_ _copy_ as a _systemd_ _service_ would do the _same_ from _inside_ Ubuntu.
- A _second_ _machine_, which would _guard_ _against_ a _failed_ _machine_ _too_.

**Critical to success**

- **Count only crashes and hangs.** A _database_ _outage_ _reaches_ both _copies_ at _once_; if its 503s _counted_ as _failures_, Nginx would _rest_ both and _lose_ the _kept_ _tiles_' fallback for _nothing_.

## Part F · Clean places

_One_ _story_ _cleans_ the _places_ _before_ they _become_ a _script_, so _faults_ in a _publisher_'s _data_ _stop_ at the _pipeline_ _rather_ than _reach_ the _map_.

### Step 30 · US-36 Clean the places before they reach a script

| | |
|---|---|
| Size | 3 _points_ |
| Builds on | US-24: the _pipeline_ and its _scripts_ |

**In plain words.** _Data_ from the _field_ _carries_ _faults_: a _shape_ that is _empty_, an _area_ whose _edge_ _crosses_ _itself_, longitude and latitude _written_ the _wrong_ _way_ _round_, a _name_ padded with _spaces_, a _place_ with no _name_, or _one_ _place_ _entered_ _twice_. The _pipeline_ _file_ _lists_ **filters** for _each_ _table_, _run_ in _turn_. _Each_ _fixes_ what it _safely_ can, and _drops_ or _stops_ at what it _cannot_. Every _run_ _writes_ a **log**, a _line_ for _each_ _row_ a filter touched, so the _publisher_ _sees_ what was _left_ out and why.

> **Story.** **As** a _data_ _publisher_, **I want** my _places_ _cleaned_ by the filters I _choose_, with the _run_ _stopping_ on the _faults_ I _name_ and a _log_ of every _change_, **so that** no faulty _place_ _reaches_ the _map_, and I _know_ what the _pipeline_ did to my _data_.

**Acceptance criteria**

1. **Given** a _pipeline_ _file_, **then** _each_ _table_ _runs_ its filters in the _order_ _listed_, _each_ on the _rows_ the _one_ _before_ _kept_: `dropEmptyShapes`, `repairShapes`, `swapCoordinates`, `insideArea`, `tidyText`, `requireFields`, `dropDuplicateIds` and `dropNearDuplicates`.
2. **Given** a _row_ a filter _cannot_ _pass_, **then** it is _dropped_, or, when the filter _says_ `"onFail": "stop"`, the _run_ _ends_ with no _script_ _written_.
3. **Given** a _table_ that _lost_ more than `droppedOverPercent` of its _rows_, 10 by _default_, or _kept_ _fewer_ than `rowsUnder`, 1 by _default_, **then** the _run_ _ends_ with no _script_ _written_. `stopWhen` _sets_ both for the _whole_ _file_ or for _one_ _table_.
4. **Given** _any_ _run_, _finished_ or _stopped_, **then** it _writes_ a _log_ in `db/seed-logs/`, _named_ _after_ the _script_: _each_ _table_'s _rows_ _read_ and _kept_, _each_ filter's _counts_, and _one_ _line_ for _each_ _row_ _fixed_, _dropped_ or _stopped_ at, with the _reason_.
5. **Given** a filter that is _misspelt_, _lacks_ a _setting_ it _needs_, or a _key_ the _file_ _format_ _lacks_, **then** the _pipeline_ _names_ every _fault_ at _once_ and _runs_ _nothing_.
6. **Given** a _file_ ***geodatabase***, **then** _each_ _table_ _reads_ its ***feature class*** by `layer`, with curves _made_ _straight_, heights _dropped_ and _coded_ _values_ _written_ as their _descriptions_.

**Tests to write first**

`etl/tests` _holds_ these _tests_.

1. **Each filter alone.** _Pure_ _tests_: what _each_ _keeps_, _fixes_ and _drops_, and the _stop_ at the _first_ _bad_ _row_.
2. **The stop rules.** A _table_ that _drops_ 2 of 10 _rows_ _stops_ _under_ the _default_ 10 %, and _passes_ when its _own_ _rule_ _allows_ 25 %.
3. **A geodatabase, end to end.** Over a fixture geodatabase of faulty _places_, the _run_ _keeps_ 3 of 7 cafés, _swaps_ _one_ _back_, _repairs_ a _crossing_ park, and logs _each_ _row_; a _run_ _set_ to _stop_ _writes_ no _script_ and logs why.

**Open to talk**

- More filters as the _data_ _asks_ for them, such as a _list_ of _allowed_ _values_ for a _field_.
- _Whether_ a _log_ should _also_ _go_ to the _Project_, _beside_ the _script_ it _explains_.

**Critical to success**

- **Stop by default.** A _run_ that _drops_ more than a tenth of a _table_ _stops_; a _publisher_ who _wants_ more loss must _say_ so in the _file_.
- **The log is the record.** A _dropped_ _row_ _leaves_ no _trace_ in the _script_, so the _log_, _committed_ _beside_ it, is the _only_ account of what was _left_ out.

## The INVEST check

A tick _means_ the _story_ _meets_ the _test_ in _full_; 'Partly' _names_ the _reason_. _Under_ 'Independent', _each_ _story_ _names_ the _stories_ _above_ it that it _needs_; _none_ _needs_ a _story_ _below_ it.

| Step | Story | Independent | Negotiable | Valuable | Estimable | Small | Testable |
|---|---|---|---|---|---|---|---|
| 1 | US-00 _Walking_ skeleton | ✓ _needs_ _none_ | ✓ | _Partly_: an enabler for the _maintainer_ | ✓ | ✓ 1 | ✓ |
| 2 | US-01 _One_ _database_ | ✓ _after_ US-00 | ✓ | _Partly_: an enabler for the _maintainer_ | ✓ | ✓ 1 | ✓ |
| 3 | US-03 _Publish_ by _grant_ | ✓ _after_ US-01 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |
| 4 | US-24 _Places_ in _nine_ _layers_ | ✓ _after_ US-01, US-03 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 5 | US-07 _Serve_ a _tile_ | ✓ _after_ US-24 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 6 | S-01 SQL injection | ✓ _after_ US-07 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 7 | S-06 _Malformed_ URLs | ✓ _after_ US-07 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 8 | US-35 _Office_ on _port_ 80 | ✓ _after_ US-07 | ✓ | ✓ | ✓ | ✓ 1 | _Partly_: its _first_ _run_ _needs_ the _real_ _machine_ |
| 9 | US-23 _Open_ the _map_ | ✓ _after_ US-07, US-35 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |
| 10 | UI-09 _Levels_ 0 to 22 | ✓ _after_ US-23 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 11 | UI-15 _Calm_ _Material_ _page_ | ✓ _after_ US-23 | ✓ | ✓ | ✓ | ✓ 2 | _Partly_: 'calm' _needs_ a _person_'s _eye_ _beside_ the _checks_ |
| 12 | UI-12 _Shapes_ as _well_ as _colours_ | ✓ _after_ US-23, US-24 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 13 | UI-19 _Scale_ _bar_, compass and coordinates | ✓ _after_ UI-09 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 14 | UI-10 Clusters | ✓ _after_ UI-09 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |
| 15 | UI-11 _Names_ | ✓ _after_ UI-10 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 16 | UI-17 _Names_ that _never_ overlap | ✓ _after_ UI-11 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 17 | N-18 A _browser_ _keeps_ a _tile_ | ✓ _after_ US-07 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 18 | N-19 A _kept_ _tile_ _costs_ no _query_ | ✓ _after_ N-18 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 19 | N-20 _Packed_ _answers_ | ✓ _after_ US-07, US-23, N-19 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 20 | N-21 _One_ ***settings file*** | ✓ _after_ US-00, N-18 | ✓ | _Partly_: for the _maintainer_ | ✓ | ✓ 2 | ✓ |
| 21 | N-22 503 and the _query_ _limit_ | ✓ _after_ N-21, US-07 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 22 | N-23 _Grants_ _without_ a _restart_ | ✓ _after_ US-03, N-22 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 23 | N-24 _One_ _query_ for a _crowd_ | ✓ _after_ N-19 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 24 | N-25 `/health` | ✓ _after_ N-22, N-23 | ✓ | _Partly_: for the _maintainer_ | ✓ | ✓ 1 | ✓ |
| 25 | N-26 _Order_ of _arrival_ | ✓ _after_ N-22, N-25 | ✓ | _Partly_: for the _maintainer_ | ✓ | ✓ 1 | ✓ |
| 26 | N-28 _Kept_ _tiles_ _while_ it _hangs_ | ✓ _after_ N-19, N-22 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 27 | S-07 _Office_ _machines_ _alone_ | ✓ _after_ US-35 | ✓ | _Partly_: for the _maintainer_ | ✓ | ✓ 1 | ✓ |
| 28 | S-30 A _cap_ for _each_ _visitor_ | ✓ _after_ S-07, N-19 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 29 | N-29 _Two_ _copies_ | ✓ _after_ N-25, N-28 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 30 | US-36 _Clean_ _places_ | ✓ _after_ US-24 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |

The thirty _stories_ _come_ to 52 _points_: 13 for the _service_, 16 for the _map_, 4 for _Part_ C, 11 for _Part_ D, 5 for _Part_ E and 3 for _Part_ F. _Some_ _criteria_ _name_ a _later_ _story_: US-03 for its _problem_ _documents_ (US-07), US-07 for the 400 of S-06, and UI-09 for a _click_ on a cluster (UI-10). _Each_ such _criterion_ _holds_ _once_ that _story_ is _built_, and the _later_ _story_'s _tests_ _prove_ it.
