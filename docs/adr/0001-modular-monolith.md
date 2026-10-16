# ADR 0001: Build the back end as a modular monolith

| | |
|---|---|
| Status | _Accepted_ |
| Date | 15 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | The _Phase_ Oct _back_ _end_, in C# 14 on .NET 10, in `nathanwh71/Tessera` |
| Stories | US-00, US-01, US-03, US-24, US-07, S-01, S-06, US-35, US-23 (`docs/user-stories/user-stories.md`) |

## Decision

Tessera's _back_ _end_ is _one_ _service_, _built_ and _shipped_ as _one_ _unit_, and _divided_ _inside_ into ***modules*** with _fixed_ _borders_. The _service_ is _one_ _project_, `src/Tessera`, laid out as the modular-monolith _template_; Tessera is its _one_ _module_, `Modules.Tessera`, and ***architecture tests*** _guard_ the _borders_ _between_ its ***layers***. ADR 0010 _sets_ out the layout, the _rules_ and the _tests_.

This _record_ _covers_ the _back_ _end_ _alone_. The _map_'s _front_ _end_, in TypeScript, _keeps_ its _own_ _design_ (ADR 0006).

## Context

_Five_ _facts_ _shape_ the _decision_.

- **One maintainer.** _One_ _person_ _builds_, _runs_ and _repairs_ Tessera, and should not have to _remember_ which _part_ may _use_ which.
- **One machine.** The _service_ _runs_ in Ubuntu 24.04 _inside_ WSL2, on _one_ _Windows_ 11 _machine_, _behind_ Nginx on _port_ 80 (US-35, ADR 0008).
- **One program.** The _same_ _service_ _serves_ the ***tiles*** (US-07) and the _map_'s _files_ (US-23), and _ships_ them in _one_ _release_; `npm start` _runs_ _two_ _copies_ of it _behind_ Nginx (N-29).
- **One gate to the database.** S-01 _requires_ that every SQL _text_ _lives_ in _one_ _file_ and that the _service_ _sends_ it _through_ _one_ _module_.
- **One place for the design.** The _stories_ _leave_ the _design_ to the _team_ on _purpose_; this _record_ _holds_ it.

## Options considered

| Option | For | Against |
|---|---|---|
| One service, divided into modules with guarded borders (chosen) | _One_ _release_, and the _folders_ _read_ as _one_ _program_. A _broken_ _border_ _fails_ a _named_ _test_ (ADR 0010). | _Each_ _story_ _begins_ by _choosing_ the _module_ and the _layer_ that _own_ its _code_. |
| One service, with borders kept by review alone | _Nothing_ to _keep_. | _Nothing_ _stops_ a slip, so a _circle_ of _modules_ can _grow_ unnoticed. |
| Separate services | The _network_ _enforces_ the _borders_. | _Several_ _releases_ on _one_ _machine_, and _network_ _calls_ _between_ _parts_, for _one_ _maintainer_ who _gains_ _nothing_ from them. |

## The parts

| Part | Where | Owns |
|---|---|---|
| The program | `src/Tessera/Program.cs` | _Serving_, or _running_ the _seed_ |
| The Host | `src/Tessera/TesseraHost.cs` | _Building_ the _service_ from `Common` and _each_ _module_, and the _steps_ every _request_ _passes_ |
| Shared parts | `src/Tessera/Common` | The _shape_ of every ***problem document*** (`Api/Problems.cs`); _reading_ and _checking_ `tessera.json` and the _environment_ (`Infrastructure/Settings.cs`, ADR 0003) |
| The Tessera module | `src/Tessera/Modules/Tessera` | The _tiles_, the _map_, `/health`, the _list_ of _layers_, every SQL _text_ and the _seed_, in _three_ _layers_: Domain, Infrastructure and _Features_ |

A _module_ _joins_ when a _story_ _needs_ _one_, as a _folder_ _under_ `Modules`, and _leaves_ when _none_ does.

**The seed is a command of the service.** The _same_ _program_ that _serves_ _tiles_ _runs_ the _seed_ when _called_ with `seed`, then _stops_. Its _checks_ and its _tests_ _live_ in C#, _beside_ the _rest_ of the _back_ _end_.

**Each program signs in as its own role.** The _service_ _signs_ in as `tessera_reader`, which _reads_ _granted_ _tables_ _alone_ (US-03). The _seed_ _signs_ in as `tessera_publisher`, the _data_ _publisher_'s ***role***, which _owns_ the ***schemas*** and their _tables_. It _creates_ and _fills_ them; the _publisher_ ***grants*** them when _ready_ (US-24, _criterion_ 5).

| Variable | Read by | Holds |
|---|---|---|
| `DATABASE_URL` | The _service_, at every _start_ | `postgresql://tessera_reader:<password>@<host>:<port>/<database>` |
| `TESSERA_PUBLISHER_DATABASE_URL` | `seed`, _only_ when it _runs_ | `postgresql://tessera_publisher:<password>@<host>:<port>/<database>` |
| `TESSERA_READER_PASSWORD` | The _role_ _script_, on the _database_'s _first_ _start_ | `tessera_reader`'s _password_ |
| `TESSERA_PUBLISHER_PASSWORD` | The _role_ _script_, on the _database_'s _first_ _start_ | `tessera_publisher`'s _password_ |

_Each_ URL _carries_ the _machine_'s _name_, _address_ and _port_, as US-00 _requires_; `ConnectionString.FromUrl` _turns_ it into a _connection_ _string_ for the _database_ _client_. The _service_ _never_ _reads_ `TESSERA_PUBLISHER_DATABASE_URL`, so it _never_ _holds_ the _publisher_'s _rights_ _while_ it _serves_ _tiles_.

**`npm start` runs the seed at every start.** The _script_ _starts_ the _database_, _waits_ until it _answers_, and _runs_ `seed`. DbUp _runs_ _only_ the _scripts_ it has not _run_ _before_, so a _start_ with _nothing_ _new_ _changes_ _nothing_.

## Rules at the borders

ADR 0010 _lists_ the _rules_ and what _guards_ _each_. In _short_: the Domain _stands_ _alone_; _only_ the Infrastructure _talks_ to the _database_ (S-01); the Infrastructure _knows_ no _web_; `Common` _reaches_ no _module_; another _module_ _reaches_ Tessera _through_ its `PublicApi` _alone_; and _each_ namespace _matches_ its _folder_. _Two_ _rules_ _rest_ on _review_:

- **One shape for errors.** Every _problem_ _document_ _takes_ its _shape_ from `Common/Api/Problems.cs`; no _route_ _writes_ an _error_ _body_ of its _own_ (US-07).
- **Values, not shared state.** _Parts_ _pass_ immutable _values_ to _one_ another, and _report_ an _expected_ _failure_ as a `Result`, _never_ as a _shared_ _object_ or an exception (ADR 0004).

The ***tile query*** _shows_ the _layers_ at _work_. `Queries.Tile`, in the Infrastructure, _takes_ a `Layer` and a `TileAddress` from the Domain, and the _tile_ _route_, in _Features_, _sends_ it _through_ `Database`.

## Consequences

**What gets easier**

- A _broken_ _border_ _fails_ a _named_ _test_, with the _types_ that _broke_ it (ADR 0010).
- _Each_ _story_ _changes_ _one_ or _two_ _layers_, so its _tests_ _stay_ _small_.
- _One_ _release_ _keeps_ US-35's set-up _simple_: _one_ _program_ _behind_ Nginx.
- A _module_ that _needs_ its _own_ _machine_ can _leave_ along a _border_ that _already_ _exists_.

**What gets harder**

- _Each_ _story_ _begins_ with a _decision_: which _module_ and which _layer_ _own_ the _new_ _code_.
- A _new_ _use_ of _one_ _layer_ by another _needs_ a _moment_'s _thought_ _about_ _whether_ it _belongs_, and the _tests_ _decide_ _whether_ it may.

## When to revisit

The ***modular monolith*** _stands_ until _one_ of _three_ _events_ _happens_. _Each_ _reopens_ the _decision_.

- **A part needs its own machine.** If _tiles_ _draw_ so _many_ _requests_ that the _tile_ _work_ _needs_ a _second_ _machine_ _while_ the _rest_ _stays_ _put_, the _tile_ _route_ may _leave_ as a _separate_ _service_.
- **A part needs its own release.** If the _map_ must _ship_ _changes_ _weekly_ _while_ the _tile_ API _changes_ _twice_ a _year_, _one_ _release_ for both _starts_ to _hurt_.
- **A second maintainer joins.** If _two_ _people_ _each_ _own_ _part_ of the _back_ _end_, _separate_ _services_ may _suit_ them _better_.

In _each_ _case_, the _module_ _borders_ _mark_ where the _service_ would _divide_.
