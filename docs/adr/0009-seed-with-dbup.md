# ADR 0009: Clean the places in a C# pipeline, and seed the database with DbUp

| | |
|---|---|
| Status | _Accepted_ |
| Date | 15 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | The _service_, `nathanwh71/Tessera`; its ***pipeline***, `tools/Tessera.Etl`; and its ***seed scripts*** in `db/seed` |
| Related | ADR 0001, ADR 0002, ADR 0004, ADR 0008; US-01, US-03, US-24 |

## Decision

A _pipeline_ in C#, `tools/Tessera.Etl`, _turns_ _each_ _file_ of _places_ into a SQL _script_ in _three_ _steps_. GDAL's `ogr2ogr` **extracts** _each_ _table_ to GeoJSON; ***filters*** the ***pipeline file*** _names_ **transform** its _rows_, _cleaning_ them; and the _pipeline_ **loads** the _cleaned_ _rows_ as SQL, in the _form_ GDAL's PGDump _driver_ _wrote_ until 16 _October_ 2026 (`TableSql.cs`). The _maintainer_ _reads_ the _script_ and its _log_, and _commits_ both. `Tessera seed`, _signed_ in as `tessera_publisher`, _runs_ _each_ _script_ DbUp has not _run_ _before_, _once_, in the _order_ of the _scripts_' _names_. A ***grant*** _publishes_ _each_ _table_.

## Why

- **Many shapes, no new code.** The _maintainer_ _owns_ spatial _data_ in _several_ _shapes_, _each_ with _columns_ of its _own_, from ArcGIS _file_ ***geodatabases*** to GeoJSON. `ogr2ogr` _reads_ more than eighty _formats_, _makes_ a _table_ to _fit_ _each_ _file_, and reprojects as it _reads_.
- **Clean before it lands.** _Real_ _data_ _holds_ _empty_ _shapes_, invalid _areas_, _swapped_ longitude and latitude, stray _spaces_, _missing_ _names_ and _places_ _entered_ _twice_. A filter _catches_ _each_ _before_ it _reaches_ a _script_, and the _log_ _names_ every _row_ it touched.
- **Stop rather than guess.** A filter _set_ to _stop_, or a _table_ that _loses_ _too_ _many_ _rows_, _ends_ the _run_ with no _script_ _written_, so _bad_ _data_ _never_ _reaches_ the _database_ by _default_.
- **C#, beside the service.** The filters are ***pure functions*** in the _style_ of ADR 0004, _tested_ in _seconds_ with no _files_. NetTopologySuite, the .NET _port_ of the geometry _library_ _behind_ PostGIS, _measures_ and _repairs_ the _shapes_.
- **SQL to read, once.** DbUp _runs_ _each_ _script_ _once_ and _records_ it; its PostgreSQL _package_ has 24.6 million _downloads_. A _script_ is _plain_ SQL, so the _maintainer_ _reads_ _exactly_ what will _enter_ the _database_ _before_ it does.

## How it works

| Step | Run by | What happens |
|---|---|---|
| Inspect | `npm run inspect -- SOURCE [--split FIELD]`, a _script_ in TypeScript with zx (ADR 0002) | GDAL's `ogrinfo` _describes_ _each_ ***layer***: its _rows_, its _shape_, its _system_ and its _fields_; the _command_ _prints_ a _starting_ _pipeline_ _file_, with the _next_ _script_'s _number_ |
| Write the pipeline file | The _maintainer_ | _Edits_ the _starting_ _file_: _each_ _table_'s ***schema***, _name_ and filters, such as `data/macau.pipeline.json` |
| Run it | `npm run etl -- PIPELINE.json` | Extracts, _cleans_ and _loads_ _each_ _table_; _writes_ the _script_, such as `db/seed/0002-parks.sql`, and its _log_ in `db/seed-logs/` |
| Review | The _maintainer_ | _Reads_ the _log_ and the _script_, then _commits_ both; _together_ they _record_ what _entered_ the _database_, and what was _left_ out and why |
| Seed | `Tessera seed`, which `npm start` _runs_ at every _start_ | DbUp _runs_ _each_ _new_ _script_ in a _transaction_ of its _own_ and _records_ it in `tessera.schemaversions` |
| Publish | `npm run publish` | _Grants_ the schemas and their _tables_ to `tessera_reader` (US-24 c5) |

The filters, which _run_ in the _order_ the _pipeline_ _file_ _lists_ them:

| Filter | Kind | What it does |
|---|---|---|
| `dropEmptyShapes` | _Shape_ | A _row_ with no _shape_, or an _empty_ _one_, _fails_ |
| `repairShapes` | _Shape_ | An invalid _shape_ is rebuilt with NetTopologySuite's `GeometryFixer`; _one_ that _comes_ out _empty_ _fails_ |
| `swapCoordinates` | _Shape_ | A _shape_ _outside_ the _area_ that _falls_ _inside_ it with its _two_ _numbers_ _swapped_ is _swapped_ _back_ |
| `insideArea` | _Shape_ | A _shape_ that does not _touch_ the _area_'s _box_ _fails_ |
| `tidyText` | _Text_ | _Text_ is _put_ into Unicode's NFC _form_, _runs_ of _spaces_ _become_ _one_, _ends_ are trimmed, and _empty_ _text_ _becomes_ _null_ |
| `requireFields` | _Fields_ | A _row_ _missing_ _any_ of the _named_ _fields_, or _holding_ _only_ _spaces_ there, _fails_ |
| `dropDuplicateIds` | Duplicates | A _row_ that _repeats_ an earlier _row_'s id _fails_ |
| `dropNearDuplicates` | Duplicates | A _row_ whose _name_ _matches_ an earlier _row_'s, ignoring _case_ and _spaces_, _within_ the _given_ _metres_, _fails_ |

A _row_ that _fails_ is _dropped_ and logged, or, when its filter _says_ `"onFail": "stop"`, _ends_ the _run_. _After_ its filters, a _table_ _stops_ the _run_ when more than `droppedOverPercent` of its _rows_ were _dropped_, 10 by _default_, or _fewer_ than `rowsUnder` _remain_, 1 by _default_.

_Each_ _table_ is _cleaned_ by _one_ _chain_ of _steps_, _built_ in Giraffe's _shape_: a _step_ is a _handler_, a _function_ that _receives_ the _rest_ of the _chain_ and the _table_'s context, its _rows_, its _log_ and _any_ _reason_ to _stop_ (`Pipes.cs`). A _step_ _changes_ the context and _passes_ it on, or _ends_ the _chain_ by not _passing_ it on, as a _stop_ does. _Each_ filter is a _judge_, a _function_ that _gives_ _one_ verdict for _each_ _row_: _keep_ it, _keep_ a mended _copy_, or _fail_ it; `Steps.Filter` _turns_ a _judge_ into a _step_ that _drops_ and logs a _failed_ _row_, or _stops_ at it. _Steps_ _join_ with `>>`, an operator that C# 14's extension _members_ _add_ to the _handler_ _type_: `a >> b` _runs_ `a`, then `b` on the _rows_ `a` _kept_. The _pipeline_ _file_'s _list_ of filters _folds_ into _one_ _chain_, and the ***stop rules*** are its _last_ _step_. _Code_ may _build_ the _same_ _chain_ by _hand_ from _each_ filter's _named_ `Create` _function_. _Each_ filter _lives_ in a _file_ of its _own_ in `tools/Tessera.Etl/Filters/`, with a _record_ of its _own_ _settings_, so the _pipeline_ _file_ _refuses_ a _setting_ that _belongs_ to another filter. The _script_ _takes_ its _columns_ from the _cleaned_ _rows_, so a filter may _add_ a _column_ or _remove_ _one_.

_Each_ _choice_ of the extract and the _load_:

- **Longitude and latitude, flat.** Extraction _stores_ every _shape_ in EPSG:4326, _whatever_ _system_ the _source_ _uses_, and _drops_ heights and _measures_ (`-dim XY`), which a _flat_ ***tile*** _cannot_ _carry_.
- **Straight lines.** _True_ curves, such as a geodatabase's arcs, _become_ _short_ _straight_ _lines_ (`CONVERT_TO_LINEAR`), which PostGIS's _tile_ _functions_ _read_.
- **Words, not codes.** A geodatabase's _coded_ _value_ _becomes_ its _description_ (`-resolveDomains`), so a _tile_ _carries_ 'Museum', not 3.
- **INSERT, not COPY.** DbUp _sends_ a _script_ as _plain_ SQL, so the _script_ _holds_ _one_ `INSERT` for _each_ _row_.
- **One transaction for each script.** The _script_ _holds_ no `BEGIN` or `COMMIT` of its _own_, so DbUp's _transaction_ _holds_ the _whole_ _script_ and its ***journal*** _record_. A _script_ that _fails_ _changes_ _nothing_, and _runs_ _again_ _once_ _fixed_.
- **A numbered key.** The _rows_ are _numbered_ 1, 2, 3 in the _order_ the filters _kept_ them, in a `fid` _column_. It is the _primary_ _key_, which _fixes_ the _order_ of _features_ in a _tile_ (US-24 _test_ 2). An `id` in the _source_ _stays_ a _column_ of its _own_.
- **One kind of shape for lines and areas.** A _table_ _marked_ `lines` _stores_ _one_ _part_ and _many_ _parts_ alike as many-part _shapes_, as GDAL's `PROMOTE_TO_MULTI` did, so its _column_ _takes_ _one_ _type_, such as `MULTIPOLYGON`.
- **Names and types as GDAL chose them.** _Column_ _names_ _lose_ capitals A to Z and _turn_ ' - # into _; _whole_ _numbers_ _become_ `INTEGER` or `INT8`, fractions `FLOAT8`, _true_ and _false_ `BOOLEAN`; _text_ that _holds_ _only_ _dates_, _times_ or _dates_ with _times_ _becomes_ `date`, `time` or `timestamp with time zone`. _Two_ _choices_ _differ_ from GDAL's on _purpose_: a fraction _keeps_ every digit, where GDAL _kept_ _fifteen_, and _text_ mixed with _other_ _kinds_ _stays_ _text_, where GDAL _made_ it `JSON`, which the ***tile query*** _leaves_ out.
- **A new script replaces a table whole.** _Each_ _script_ _drops_ a _table_ _before_ it _makes_ it _again_. The _grant_ _goes_ with the _old_ _table_, so the _new_ _one_ _waits_ for the _publisher_.
- **Fresh statistics.** _Each_ _table_ _ends_ with `ANALYZE`, so PostgreSQL _plans_ the _tile_ _query_ _well_ from the _first_ _tile_.
- **A name used once.** The _pipeline_ _refuses_ a _script_ _name_ that _exists_, _since_ DbUp _knows_ a _script_ by its _name_ and would _never_ _run_ a _changed_ _one_.

## Options considered

| Option | Why not |
|---|---|
| Python, with GeoPandas and Shapely | Rich _cleaning_, but a _third_ _runtime_ and its _packages_ _beside_ .NET and Node.js, and a _second_ _language_ for the _back_ _end_ |
| One `switch` on the filter's name, with one record of settings shared by every filter | A _new_ filter _touches_ _four_ _places_ in _one_ _file_, and a _setting_ meant for _one_ filter _passes_ unseen on every _other_ |
| `ogr2ogr` alone, straight to SQL | No _place_ for _cleaning_ beyond a `-where`, and no _log_ of what was _left_ out |
| GDAL's own .NET bindings in place of `ogr2ogr` | _Native_ _libraries_ to _match_ to _each_ _machine_; `ogr2ogr` as a _program_ is _one_ _install_, or _one_ Docker _image_ |
| Run `ogr2ogr` straight into the database | No _script_ to _review_ or _commit_, and _nothing_ _records_ what _ran_ |
| Flyway, Liquibase or Sqitch | _Each_ _needs_ a _runtime_ or a _program_ of its _own_ _beside_ .NET; DbUp is a _package_ the _service_ _loads_ |
| `COPY` in the scripts | _Faster_ to _load_, but DbUp _cannot_ _feed_ `COPY ... FROM STDIN` |

## Consequences

- The _database_ has a _second_ _door_. DbUp and the _seed_ _open_ their _own_ _connections_, so _rule_ 4 of ADR 0001, _one_ _gate_ to SQL, has _two_ _doors_. The _seed_'s _one_ SQL _text_ of its _own_, the journal's schema, _lives_ in `Queries.cs` with every _other_.
- _Each_ _table_ has a `fid` _column_, which its _tiles_ _carry_ as a _property_, _beside_ _any_ `id` from the _source_.
- A _replaced_ _table_ _leaves_ the _map_ until `npm run publish`. Meanwhile Nginx _serves_ its _kept_ _tiles_ for _up_ to `Tiles.CacheSeconds`.
- A _script_'s SQL _never_ _changes_ _once_ it has _run_; a _comment_ may, _since_ DbUp _knows_ a _script_ by its _name_ _alone_. _New_ or _corrected_ _data_ _goes_ in a _new_ _script_, with the _next_ _number_.
- The _pipeline_ _adds_ _two_ _packages_: NetTopologySuite 2.6.0 and NetTopologySuite.IO.GeoJSON4STJ 4.0.0. The _service_ _itself_ _loads_ neither.
- `npm run etl` _uses_ the _machine_'s _own_ `ogr2ogr`, or _else_ GDAL's Docker _image_, `ghcr.io/osgeo/gdal:alpine-small-3.11.4`. The _tests_ and the _automatic_ _build_ _run_ with Ubuntu's GDAL 3.8.4; the _image_ is untested.
- GDAL _writes_ a _text_ _field_ _named_ `id` as a GeoJSON _feature_'s _own_ id; the _pipeline_ _reads_ it _back_ as the `id` _column_.

## When to revisit

- A _source_ _too_ _large_ for `INSERT` _statements_, such as a million _rows_: _load_ it with `COPY`, _outside_ DbUp, and _record_ the _load_ by _hand_.
- A _cleaning_ _rule_ that _needs_ the _database_, such as a _check_ _against_ _places_ _already_ _published_: _run_ it _after_ the _seed_, as SQL, _before_ the _grant_.
- An enterprise geodatabase, _held_ in SQL _Server_ or Oracle: _read_ it _through_ GDAL's _driver_ for that _database_, or _export_ a _file_ geodatabase from ArcGIS Pro _first_.
