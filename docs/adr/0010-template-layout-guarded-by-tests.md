# ADR 0010: Lay the back end out as the modular-monolith template, with tests guarding its borders

| | |
|---|---|
| Status | _Accepted_ |
| Date | 16 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | The _service_, `src/Tessera`, and its ***architecture tests***, `tests/Tessera.ArchitectureTests` |
| Related | ADR 0001, ADR 0004, ADR 0008; `docs/design/template-mapping.md` |

## Decision

The _back_ _end_ _follows_ the modular-monolith _template_'s layout: a `Common` _part_ that every ***module*** may _use_, and a _folder_ for _each_ _module_ _under_ `Modules`, _divided_ into Domain, Infrastructure and _Features_. Tessera is the _one_ _module_, `Modules.Tessera`. The _whole_ _service_ is _one_ _project_, `src/Tessera`, and _each_ namespace _matches_ its _folder_.

_Six_ ArchUnitNET _rules_, in `tests/Tessera.ArchitectureTests`, _guard_ the _borders_. `npm test` and the _build_ on GitHub _run_ them _before_ the _service_'s _other_ _suites_.

ADR 0001 _records_ the _choice_ of _one_ _service_, _built_ and _shipped_ as _one_ _unit_; this _record_ _sets_ its layout and its _guards_.

## Context

- **The template.** The _maintainer_ _chose_ the modular-monolith _template_ as the _shape_ for Tessera's _back_ _end_, so a _reader_ who _knows_ it _finds_ _each_ _file_ where they _expect_ it.
- **Project files cost more than they guard.** A _project_ for _each_ _part_, with `DisableTransitiveProjectReferences` and `PrivateAssets` on Npgsql and Dapper, _lets_ the _compiler_ _guard_ _each_ _border_, but the _maintainer_ _finds_ such _files_ _hard_ to _follow_, and the _borders_ they _guard_ are _few_.
- **Layers, not modules, divide the code.** _Within_ Tessera, the _real_ _line_ _runs_ _between_ ***pure functions***, _effects_ on the _database_, and the HTTP _edge_ (ADR 0004). The _template_'s Domain, Infrastructure and _Features_ _draw_ that _line_.
- **A test can check what a compiler would.** _Six_ ArchUnitNET _rules_ _pass_ on _clean_ _code_, and _each_ _fails_ on a planted breach (`docs/design/template-mapping.md`).

## The layout

| Folder | Holds | May use |
|---|---|---|
| `Common/Api` | `Problems`: the _shape_ of every ***problem document*** | ASP.NET Core |
| `Common/Infrastructure` | `Settings`: _reading_ `tessera.json` and the _environment_, _binding_ a _section_ to a _record_, and _checking_ it at _start_ (ADR 0003) | .NET's _options_ |
| `Modules/Tessera/Domain` | The ***tile***'s _address_, the ***layers*** and their ***catalogue***, the _area_ and _tile_ _settings_: _records_ and _pure_ _functions_ | CSharpFunctionalExtensions' `Result` |
| `Modules/Tessera/Infrastructure` | Every SQL _text_, the _database_, the catalogue's timer, the _seed_ | Domain, Common, Npgsql, Dapper, DbUp |
| `Modules/Tessera/Features` | The _tile_ _route_, the _map_, `/health` | Domain, Infrastructure, Common, ASP.NET Core |
| `Modules/Tessera/TesseraModule.cs` | `AddTessera` and `MapTessera`: the _module_'s _one_ _door_ | All of the _module_ |
| `TesseraHost.cs` | The _template_'s _Host_: _builds_ the _service_, and _runs_ the _steps_ every _request_ _passes_, such as the exception _handler_ | Common, and _each_ _module_'s _door_ |
| `Program.cs` | _Serve_, _through_ the _Host_, or _seed_ | The _Host_, and the Infrastructure's _seed_ |

A `PublicApi` _folder_ _joins_ the _module_ when a _second_ _module_ _needs_ _something_ of Tessera's, and _holds_ _only_ what that _module_ _needs_.

## The rules, and what guards each

| Rule | Guard |
|---|---|
| 1. **Domain stands alone.** No database, no web, no Infrastructure or Features. | ArchUnitNET: `Domain_stands_alone` |
| 2. **One gate to SQL.** Only Infrastructure uses Npgsql, Dapper or DbUp (S-01). | ArchUnitNET: `Only_Infrastructure_talks_to_the_database` |
| 3. **One direction.** Infrastructure knows no web and no Features. | ArchUnitNET: `Infrastructure_knows_no_web` |
| 4. **Common serves, never reaches.** Common uses no module. | ArchUnitNET: `Common_never_reaches_a_module` |
| 5. **One door between modules.** Another module reaches Tessera through its PublicApi alone. | ArchUnitNET: `Other_modules_reach_Tessera_through_PublicApi_alone`, _waiting_ for a _second_ _module_ |
| 6. **A public face shows no workings.** PublicApi uses no Infrastructure or Features. | ArchUnitNET: `PublicApi_shows_no_workings`, _waiting_ for PublicApi's _first_ _type_ |
| 7. **A namespace matches its folder,** so the rules above can trust it. | The _compiler_: IDE0130 at _error_ _level_ |
| 8. **One shape for errors, and values rather than shared state.** | _Review_ (ADR 0001) |

## Options considered

| Option | For | Against |
|---|---|---|
| One project, ArchUnitNET rules (chosen) | _One_ _project_ _file_; the _rules_ _name_ _each_ breach and the _type_ that _made_ it. | The _rules_ _run_ at _test_ _time_, not at _compile_ _time_; ArchUnitNET is a _dependency_ of the _tests_. |
| The template's projects: one per layer, with the compiler | The _compiler_ _guards_ the _layers_. | _Three_ or _four_ _project_ _files_ for _one_ _module_, _each_ to _keep_ and to _follow_. |
| One project, review alone | _Nothing_ to _keep_. | _Nothing_ _stops_ a slip. |

## Consequences

**What gets easier**

- _One_ _project_ _builds_ the _service_, and its _folders_ _read_ as the _template_.
- A breach _fails_ a _named_ _test_ with the _types_ that _broke_ it, for instance `Modules.Tessera.Domain.Bad does depend on "Npgsql.NpgsqlConnection"`.
- A _second_ _module_ _joins_ as a _folder_ _under_ `Modules`, and the _rules_ _already_ _cover_ it.

**What gets harder**

- A breach _shows_ at _test_ _time_, not as the _code_ is _typed_. The _rules_ _run_ _before_ the _service_'s _other_ _suites_, and _take_ 8 to 13 _seconds_ _once_ _built_ on a _quiet_ _machine_, and _up_ to 45 on a _busy_ _one_.
- The _rules_ _see_ _only_ the _libraries_ they _load_ and _only_ _calls_ the _code_ _makes_ _directly_. They _load_ the _program_ with its _dependencies_, and Tessera _calls_ _nothing_ _through_ reflection.

## When to revisit

- **A part needs its own machine or its own release.** The _module_'s _folder_ _marks_ where it would _divide_, as ADR 0001 _says_.
- **ArchUnitNET misses a breach.** Then the _layers_ _become_ _projects_, and the _compiler_ _guards_ the _borders_.
