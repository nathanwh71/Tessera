# Reading the three harder files

**The short answer.** `Settings.cs`, `Database.cs` and `LiveCatalogue.cs` _each_ lean on _one_ _idea_ that ASP.NET Core or Npgsql _supplies_. _Learn_ the _idea_ _first_, and _each_ _file_ _reads_ as _plain_ C#. `Settings.cs` _uses_ the _options_ _pattern_, `Database.cs` a ***connection pool***, and `LiveCatalogue.cs` a _background_ _service_. The _first_ _sits_ in `src/Tessera/Common/Infrastructure/`; the _other_ _two_ _sit_ in `src/Tessera/Modules/Tessera/Infrastructure/`, `Database.cs` _under_ `Sql/`. _Each_ _section_ _below_ _names_ the _idea_, _shows_ where the _file_ _uses_ it, and _lists_ what surprises a _reader_ _new_ to ASP.NET Core.

## Settings.cs: the options pattern

**The idea.** ASP.NET Core _reads_ every _setting_ into _one_ tree of _keys_, such as `Tiles:CacheSeconds`, from `tessera.json` and the _environment_. The _options_ _pattern_ _copies_ _one_ _branch_ of that tree into a C# _record_, _checks_ it, and _hands_ it to _any_ _code_ that _asks_. The static _class_ `Settings` _holds_ the _steps_; _each_ _record_ _lives_ _beside_ the _code_ that _reads_ it, in the `Settings.cs` of `Domain/`, `Infrastructure/` or `Features/`, and `TesseraModule.AddSettings` _binds_ the _five_ _sections_.

| Part | What it does |
|---|---|
| `AddFile` | _Adds_ `tessera.json`, then the _environment_ _again_, so a variable _wins_ over the _file_ |
| `Section<T>` | _Binds_ _one_ _section_ to its _record_, _refuses_ a _key_ the _record_ _lacks_, and _checks_ _each_ `[Range]` |
| `ValidateOnStart` | _Runs_ the _checks_ when the _program_ _starts_, _rather_ than on _first_ _use_ |
| `Check` | _Runs_ every _check_ at _once_; a _failure_ _throws_ with every _problem_ it _found_ |
| `Problems` | _Turns_ that _failure_ into the _list_ of _problems_ `Program.cs` _prints_ |
| `IOptions<T>` | How _other_ _code_ _asks_ for a _record_, such as `GetRequiredService<IOptions<TileSettings>>().Value` |

What surprises a newcomer:

- **`services` is a table, not a set of running programs.** It is the dependency-injection _container_: a _table_ of 'when code asks for X, give it this'. `AddTessera` in `TesseraModule.cs` _fills_ the _table_, and `builder.Build()` in `TesseraHost.cs` _freezes_ it.
- **The records hold no code to read JSON.** The binder _fills_ _each_ `init` _property_ by its _name_; `[Range]` and `Validate` _carry_ the _rules_.
- **The database's URL is not here.** `DatabaseConnection`, in `Infrastructure/Sql/Connection.cs`, is a _class_ with setters: `DatabaseConnections.Register` _sets_ its _two_ _values_ _after_ .NET _creates_ it, which a _record_ with `init` _properties_ would _refuse_. It _lives_ in the Infrastructure, the _one_ ***layer*** that may _use_ Npgsql (ADR 0010, _rule_ 2), _beside_ the _names_ of the _two_ variables that _hold_ the URLs, `ReaderDatabase` and `PublisherDatabase`.

## Database.cs: one pool, and every failure as a value

**The idea.** _Opening_ a _connection_ to PostgreSQL _takes_ _time_, so Npgsql _keeps_ a _few_ _open_ in a _pool_, an `NpgsqlDataSource`, and _lends_ _one_ to _each_ _query_. When all are _busy_, a _query_ _waits_ in a _queue_, _first_ _come_, _first_ _served_ (N-26).

| Part | What it does |
|---|---|
| `Open` | _Makes_ the _pool_ from the _connection_ _string_ and the `Database` _section_: its _size_, how _long_ a _query_ may _run_, how _long_ a _request_ may _wait_ |
| `Run` | _Borrows_ a _connection_, _runs_ the _work_, _returns_ the _connection_, and _turns_ an `NpgsqlException` into a `DatabaseError` |
| `Rows` and `Single` | The _two_ _ways_ the _service_ _queries_; Dapper _fills_ a _record_ from _each_ _row_ by _column_ _name_ |
| `Ping` | _Sends_ `SELECT 1` and _waits_ _one_ _second_ at _most_, for `/health` (N-25) |
| `DatabaseError` | PostgreSQL's five-character SQLSTATE _code_, sorted _once_ by `DatabaseError.Of` into _one_ of _four_ _kinds_: the _table_ has _gone_, a _column_ has _gone_, _waiting_ may _cure_ it, or _any_ _other_ _fault_. It is a _union_, so a `switch` must _handle_ all _four_ |

What surprises a newcomer:

- **No method throws for a database failure.** _Each_ _returns_ a `Result`, from the CSharpFunctionalExtensions _library_, which _holds_ the _answer_ or the _reason_ (ADR 0004). _Read_ `Result<T, DatabaseError>` as 'a `T`, or a `DatabaseError`'.
- **`await using` returns the connection.** When the _block_ _ends_, _even_ _through_ an _error_, the _connection_ _goes_ _back_ to the _pool_. No _code_ _closes_ _one_ by _hand_.
- **`Ping` keeps two clocks.** A _cancel_ _signal_ _takes_ it out of the _pool_'s _queue_ _after_ a _second_, and `WaitAsync` _stops_ the _wait_ _after_ a _second_ _even_ when a _frozen_ _database_ _ignores_ the _cancel_.

## LiveCatalogue.cs: a list that refreshes in the background

**The idea.** A `BackgroundService` is a loop that ASP.NET Core _starts_ with the _program_ and _stops_ with it. `LiveCatalogue` _uses_ _one_ to _reread_ the _list_ of _layers_ every `Catalogue.RefreshSeconds`, so a _table_ _granted_ _later_ _appears_ _without_ a _restart_ (N-23).

| Part | What it does |
|---|---|
| `Queries.Layers` | _Asks_ PostgreSQL's _own_ _tables_, `pg_class` and `pg_attribute`, which _tables_ and _views_ the _reader_ may _read_, and which _hold_ a _typed_ geometry _column_ |
| `CatalogueReader.Load` | In `Catalogue.cs`, _runs_ that _query_ and _builds_ a `LayerCatalogue` from its _rows_ |
| `LayerCatalogue` | In `Domain/Layers.cs`, the _list_ at _one_ _moment_; it _never_ _changes_, and a refresh _builds_ a _new_ _one_ |
| `Refresh` | _Loads_ a _list_; on _success_ it _swaps_ the _list_ in _whole_, and on _failure_ it _keeps_ the _old_ _one_ and logs a _warning_ |
| `ExecuteAsync` | The loop: refresh, _wait_, _repeat_; until the _first_ _list_ _loads_, it _waits_ `retry` _rather_ than `every` |

What surprises a newcomer:

- **Many requests read the list while the loop replaces it.** `Interlocked.Exchange` and `Volatile.Read` _make_ the _swap_ a _single_ _step_, so a _request_ _sees_ the _old_ _list_ or the _new_ _one_, _never_ _half_ of _each_.
- **The SQL lives elsewhere.** `Queries.Layers` _sits_ in `Queries.cs`, _since_ every SQL _text_ _lives_ in _one_ _file_ (S-01).
- **`TesseraModule.cs` registers `LiveCatalogue` twice.** _Once_ as a _plain_ _object_, so the ***tile*** _route_ and the ***health check*** `CatalogueReady` can _read_ it, and _once_ as a ***hosted service***, so ASP.NET Core _runs_ its loop.

## Where to read more

- In this repository: ADR 0003 _lists_ every _setting_, ADR 0004 _explains_ `Result`, ADR 0010 _explains_ the _folders_ and the _rules_ in `tests/Tessera.ArchitectureTests` that _guard_ them, and N-23 and N-26 in the _user_ _stories_ _give_ the _rules_ the _list_ and the _pool_ _follow_.
- Microsoft's [_options_ _pattern_](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options) and [_hosted_ _services_](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services) _guides_, and Npgsql's [basic usage](https://www.npgsql.org/doc/basic-usage.html) _page_, which _covers_ _data_ _sources_.
