# ADR 0001: Build the back end as a modular monolith, with the compiler guarding its borders

| | |
|---|---|
| Status | _Accepted_ in _part_. _One_ _service_, _built_ and _shipped_ as _one_ _unit_, _stands_; the _project_ for _each_ ***module***, and the _compiler_ at the _borders_, _gave_ _way_ to ADR 0010 on 16 _October_ 2026 |
| Date | 15 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | The _Phase_ Oct _back_ _end_, in C# 14 on .NET 10, in `nathanwh71/Tessera` |
| Stories | US-00, US-01, US-03, US-24, US-07, S-01, S-06, US-35, US-23 (`docs/user-stories/user-stories.md`) |

## Decision

> _Since_ 16 _October_ 2026 the _service_ is _one_ _project_ laid out as the modular-monolith _template_, and _six_ ***architecture tests*** _guard_ its _borders_ (ADR 0010). The _modules_ _below_ are _now_ _folders_ in its ***layers***; this _record_ _keeps_ the _reasons_ for _one_ _service_ and for the ***roles***.

Tessera's _back_ _end_ is _one_ _service_, _built_ and _shipped_ as _one_ _unit_, and _divided_ _inside_ into _modules_ with _fixed_ _borders_. _Each_ _module_ is a _project_ of its _own_, in a _folder_ of `src/Tessera`, and _names_ _only_ the _modules_ it may _use_. The _compiler_ _guards_ the _borders_: a _module_ that _reaches_ into _one_ it does not _name_, or a _circle_ of _modules_, _fails_ the _build_.

This _record_ _covers_ the _back_ _end_ _alone_. The _map_'s _front_ _end_, in TypeScript, _keeps_ its _own_ _design_ (ADR 0006).

## Context

_Five_ _facts_ _shape_ the _decision_.

- **One maintainer.** _One_ _person_ _builds_, _runs_ and _repairs_ Tessera, and should not have to _remember_ which _module_ may _use_ which.
- **One machine.** The _service_ _runs_ in Ubuntu 24.04 _inside_ WSL2, on _one_ _Windows_ 11 _machine_, _behind_ Nginx on _port_ 80 (US-35, ADR 0008).
- **One program.** The _same_ _service_ _serves_ the ***tiles*** (US-07) and the _map_'s _files_ (US-23), and _ships_ them in _one_ _release_; `npm start` _runs_ _two_ _copies_ of it _behind_ Nginx (N-29).
- **One gate to the database.** S-01 _requires_ that every SQL _text_ _lives_ in _one_ _file_ and that the _service_ _sends_ it _through_ _one_ _module_.
- **One place for the design.** The _stories_ _leave_ the _design_ to the _team_ on _purpose_; this _record_ _holds_ it.

## Options considered

| Option | For | Against |
|---|---|---|
| A project for each module, in the folders of one service (chosen) | The _compiler_ _guards_ every _border_, and the _folders_ _read_ as _one_ _program_. A _module_'s _file_ _names_ its every _neighbour_. | _Eight_ _project_ _files_ to _keep_, _each_ a _few_ _lines_. |
| One project, a folder for each module | _One_ _file_ to _build_. | _Nothing_ but _review_ _guards_ the _borders_, so a _circle_ of _modules_ can _grow_ unnoticed. |
| An architecture test that reads the code | _One_ _project_, and a _failing_ _test_ for a _break_. | A _text_ _test_ misses _some_ _routes_ _round_ it; a _test_ _library_, such as ArchUnitNET, _adds_ a _dependency_. |
| Separate services | The _network_ _enforces_ the _borders_. | _Several_ _releases_ on _one_ _machine_, and _network_ _calls_ _between_ _parts_, for _one_ _maintainer_ who _gains_ _nothing_ from them. |

## The modules

| Project | Owns | May use | Stories |
|---|---|---|---|
| `Tessera.Problems` | The _shape_ of every ***problem document*** | _Nothing_ | US-07 |
| `Tessera.Settings` | _Reading_ `tessera.json` and the _environment_, and _refusing_ to _start_ when a _value_ is _wrong_ (ADR 0003) | _Nothing_ | US-00 |
| `Tessera.Sql` | Every _query_'s _text_, in `Queries.cs`; the _database_'s URL; the _one_ _pool_ and the _one_ _class_, `Database`, that _sends_ _queries_ _through_ Npgsql with Dapper | _Settings_ | S-01, US-03 |
| `Tessera.Catalogue` | The _list_ of _granted_ _tables_ and _views_, _read_ at start-up and _again_ every `Catalogue.RefreshSeconds` | Sql | US-03, N-23 |
| `Tessera.Seeding` | The `seed` _command_: DbUp _runs_ _each_ _new_ _script_ in `db/seed` _once_, as `tessera_publisher` (ADR 0009) | Sql | US-24 |
| `Tessera.Tiles` | The _tile_ _route_, the ***tile query***'s _inputs_, and the _headers_ that _let_ a _browser_ and Nginx _keep_ a _tile_ | ***Catalogue***, _Settings_, Sql, _Problems_ | US-07, N-18 |
| `Tessera.Map` | _Serving_ the _front_ _end_'s _built_ _files_, and _writing_ the _area_ into its _page_ | _Settings_, _Problems_ | US-23 |

`Tessera`, the _program_, _holds_ `Program.cs` and `Service.cs`. It _names_ every _module_, _puts_ them _together_, and is the _only_ _project_ that _ships_. No _module_ _names_ it.

A _module_ _joins_ the _list_ when a _story_ _needs_ _one_, and _leaves_ when _none_ does.

**The seed is a command of the service.** The _same_ _program_ that _serves_ _tiles_ _runs_ the _seed_ when _called_ with `seed`, then _stops_. Its _checks_ and its _tests_ _live_ in C#, _beside_ the _rest_ of the _back_ _end_.

**Each program signs in as its own role.** The _service_ _signs_ in as `tessera_reader`, which _reads_ _granted_ _tables_ _alone_ (US-03). The _seed_ _signs_ in as `tessera_publisher`, the _data_ _publisher_'s _role_, which _owns_ the ***schemas*** and their _tables_. It _creates_ and _fills_ them; the _publisher_ ***grants*** them when _ready_ (US-24, _criterion_ 5).

| Variable | Read by | Holds |
|---|---|---|
| `DATABASE_URL` | The _service_, at every _start_ | `postgresql://tessera_reader:<password>@<host>:<port>/<database>` |
| `TESSERA_PUBLISHER_DATABASE_URL` | `seed`, _only_ when it _runs_ | `postgresql://tessera_publisher:<password>@<host>:<port>/<database>` |
| `TESSERA_READER_PASSWORD` | The _role_ _script_, on the _database_'s _first_ _start_ | `tessera_reader`'s _password_ |
| `TESSERA_PUBLISHER_PASSWORD` | The _role_ _script_, on the _database_'s _first_ _start_ | `tessera_publisher`'s _password_ |

_Each_ URL _carries_ the _machine_'s _name_, _address_ and _port_, as US-00 _requires_; `Sql` _turns_ it into a _connection_ _string_ for the _database_ _client_. The _service_ _never_ _reads_ `TESSERA_PUBLISHER_DATABASE_URL`, so it _never_ _holds_ the _publisher_'s _rights_ _while_ it _serves_ _tiles_.

**`npm start` runs the seed at every start.** The _script_ _starts_ the _database_, _waits_ until it _answers_, and _runs_ `seed`. DbUp _runs_ _only_ the _scripts_ it has not _run_ _before_, so a _start_ with _nothing_ _new_ _changes_ _nothing_.

## Rules at the borders, and what guards each

| Rule | Guard |
|---|---|
| 1. **A small public face.** Each module shows only the types other modules need. | The _compiler_: an `internal` _type_ _stays_ _inside_ its _project_. |
| 2. **One direction.** Modules depend on one another in one direction, with no circle. | The _compiler_: .NET _refuses_ a _circle_ of _project_ _references_. |
| 3. **Named neighbours alone.** A module uses only the modules its project file names. | The _compiler_: `DisableTransitiveProjectReferences`, _set_ in `src/Tessera/Directory.Build.props`, _stops_ a _module_ _seeing_, _say_, _Settings_ _through_ Sql. |
| 4. **One gate to SQL.** Every SQL text lives in `Queries.cs`, and only `Database` sends it, save the seed, which sends the journal's schema through a connection of its own (S-01, criterion 2; ADR 0009). | The _compiler_: `Tessera.Sql` _names_ Npgsql and Dapper, _each_ _marked_ `PrivateAssets="compile;contentfiles;analyzers;build"`, so no _project_ that _names_ Sql can _use_ them; their _files_ _still_ _reach_ the _finished_ _service_. `Tessera.Seeding` is the _one_ _other_ _door_: it _reaches_ Npgsql _through_ DbUp, and _opens_ _one_ _connection_ of its _own_ to _make_ DbUp's ***journal*** schema, with the _text_ from `Queries.cs` (ADR 0009). |
| 5. **One shape for errors.** Every problem document takes its shape from `Tessera.Problems`; no route writes an error body of its own (US-07). | _Review_. |
| 6. **Values, not shared state.** Modules pass immutable values to one another, and report an expected failure as a `Result`, never as a shared object or an exception (ADR 0004). | _Review_. |

The _tile_ _query_ _shows_ _rule_ 2 at _work_. `Queries.Tile` _takes_ a `TileSource`, a _record_ of `Sql`'s _own_, and _plain_ _numbers_ for the _tile_, so `Sql` _needs_ _nothing_ of `Catalogue`'s or `Tiles`'. `Catalogue`'s `Layer` _hands_ over its `Source`.

## Consequences

**What gets easier**

- A _break_ of _rules_ 2, 3 or 4 _fails_ the _build_ at _once_, with the _compiler_'s _own_ _message_.
- _Each_ _module_'s _project_ _file_ _lists_ its _neighbours_, so the _design_ _reads_ from _eight_ _short_ _files_.
- _Each_ _story_ _changes_ _one_ or _two_ _modules_, so its _tests_ _stay_ _small_.
- _One_ _release_ _keeps_ US-35's set-up _simple_: _one_ _program_ _behind_ Nginx.
- A _module_ that _later_ _needs_ its _own_ _machine_ can _leave_ along a _border_ that _already_ _exists_.

**What gets harder**

- _Each_ _story_ _begins_ with a _decision_: which _module_ _owns_ the _new_ _code_.
- A _new_ _use_ of _one_ _module_ by another _needs_ a _line_ in a _project_ _file_, and a _moment_'s _thought_ _about_ _whether_ it _belongs_.
- The _release_ _holds_ _one_ _file_ for _each_ _module_, _seven_ in all, _beside_ the _program_'s _own_.

## When to revisit

The ***modular monolith*** _stands_ until _one_ of _three_ _events_ _happens_. _Each_ _reopens_ the _decision_.

- **A part needs its own machine.** If _tiles_ _draw_ so _many_ _requests_ that the _tile_ _work_ _needs_ a _second_ _machine_ _while_ the _rest_ _stays_ _put_, `Tessera.Tiles` may _leave_ as a _separate_ _service_.
- **A part needs its own release.** If the _map_ must _ship_ _changes_ _weekly_ _while_ the _tile_ API _changes_ _twice_ a _year_, _one_ _release_ for both _starts_ to _hurt_.
- **A second maintainer joins.** If _two_ _people_ _each_ _own_ _part_ of the _back_ _end_, _separate_ _services_ may _suit_ them _better_.

In _each_ _case_, the _module_ _borders_ _mark_ where the _service_ would _divide_.
