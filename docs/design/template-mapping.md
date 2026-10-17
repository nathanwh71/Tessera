# Mapping Tessera onto the modular-monolith template

**The short answer.** Tessera is _one_ ***module***, `Modules.Tessera`, in the _template_'s tree. The _whole_ _service_ is _one_ _project_ with the _template_'s _folders_: Domain, Infrastructure and _Features_, and a PublicApi that _waits_ until a _second_ _module_ _needs_ it. `Common/Api` _holds_ the _problem_ _answers_, and the _template_'s _Host_, `TesseraHost.cs`, _builds_ the _program_, which _calls_ `AddTessera` and `MapTessera` and _nothing_ more. The _code_ _keeps_ ADR 0004's _style_: _records_, ***pure functions*** and `Result` in, ASP.NET Core _answers_ out at the _Features_ _edge_. ASP.NET Core does every _job_ it can, such as the ETag and 304, the _status_ of `/health`, and the _map_'s _headers_; the _template_'s _resilience_ _handler_ and _any_ heavier _data_ ***layer*** _stay_ out. _Six_ ArchUnitNET _rules_ _guard_ the _borders_, and _each_ _fails_ on a planted breach. The URLs, `tessera.json`, `Tessera seed`, the npm _scripts_ and Nginx _stand_ _outside_ the _template_.

## What the layout keeps

_Three_ constraints _shape_ the layout:

| Constraint | What it means here |
|---|---|
| Keep the coding style | ADR 0004 _holds_: read-only _records_, _pure_ _functions_, `Result<T, E>` for _failures_ the _caller_ _expects_, `T?` for _values_ that may be _missing_, smart constructors for _values_ with a _rule_, _early_ _returns_, Dapper with _plain_ SQL, DbUp for ***seed scripts***, _comments_ for a _school_ _student_ |
| Prefer ASP.NET Core to hand-written code | Where the _framework_ does a _job_, Tessera _writes_ no _code_ for it; where it _cannot_, Tessera's _code_ does |
| Guard the boundaries with ArchUnitNET | _Tests_, not _project_ _references_, _keep_ _each_ _layer_ and _each_ _module_ in its _place_ |

_Four_ _fixed_ _points_ _stand_ _outside_ it:

| Fixed point | Why |
|---|---|
| The URLs: `/{layer}/{z}/{x}/{y}.pbf`, `/`, `/{layer}.html`, `/health` | Nginx, the _map_ and every _saved_ _link_ _use_ them |
| `tessera.json` and its sections, and `DATABASE_URL` | ADR 0003; the _machines_' _settings_ _depend_ on them |
| `Tessera seed` and the npm scripts | The runbook and the ***start-up task*** _call_ them |
| Nginx in front, with the limits and the kept tiles | ADR 0007 and ADR 0008 |

## The tree

```
src/Tessera/                   one project; each folder's path is its namespace
  Tessera.csproj               AssemblyName 'Tessera', no root namespace
  .editorconfig                IDE0130: a namespace must match its folder
  Program.cs                   serve, or seed
  TesseraHost.cs               the template's Host: builds the service from Common and the module
  Common/
    Api/Problems.cs            the problem answers every module shares
    Infrastructure/Settings.cs AddFile, Section<T>, Check: reads and checks a settings section
  Modules/Tessera/
    TesseraModule.cs           AddTessera and MapTessera: the module's one door
    Domain/                    Tiles.cs (TileAddress), Layers.cs (Layer, LayerCatalogue),
                               SqlName.cs (SqlName), Settings.cs (TileSettings, AreaSettings)
    Infrastructure/            Sql/ (Connection, Database, Queries), Seeding/ (SeedScripts),
                               Catalogue.cs (CatalogueRow, CatalogueReader), LiveCatalogue.cs,
                               HealthChecks.cs (DatabaseAnswers, CatalogueReady),
                               Settings.cs (DatabaseSettings, CatalogueSettings)
    Features/                  Tiles.cs, Map.cs, Health.cs, Settings.cs (MapSettings)
tests/
  Tessera.ArchitectureTests/   the six boundary rules
  Tessera.UnitTests/           pure functions, with no database
  Tessera.IntegrationTests/    the service and the seed, against PostgreSQL
```

The _whole_ _service_ is _one_ _project_. The _folders_ _match_ the _template_'s _names_, so a _reader_ who _knows_ the _template_ _finds_ _each_ _file_ where they _expect_ it. _Each_ namespace _matches_ its _folder_, and the _build_ _enforces_ it (_see_ the _rules_ _below_).

The _template_'s `Common` is _empty_, so Tessera _fills_ _two_ of its _folders_, `Api` and `Infrastructure`, and _adds_ no _empty_ _ones_; nor does the _module_ _carry_ an _empty_ `PublicApi`. _Each_ _folder_ _appears_ with its _first_ _file_. The _template_'s _four_ _sample_ _modules_ _carry_ _nothing_ Tessera _needs_, and Tessera _leaves_ them out, with Aspire's AppHost: `npm start` _runs_ the _service_.

## Where each part lives

| Part | File | Note |
|---|---|---|
| The program | `Program.cs` | _Serves_ _through_ the _Host_, or _runs_ the _seed_, which _reads_ the _publisher_'s URL _alone_ |
| The Host | `TesseraHost.cs`, at the _project_'s _root_, in the global namespace | _One_ _project_ _holds_ the _whole_ _service_, so the _Host_ is a _file_, not a _project_. It _builds_ the _program_ and _runs_ the _steps_ every _request_ _passes_, such as the exception _handler_ |
| The module's door | `Modules/Tessera/TesseraModule.cs` | _Adds_ the _module_'s _services_ (`AddTessera`) and _routes_ (`MapTessera`) |
| Problem answers | `Common/Api/Problems.cs` | Every _module_ _answers_ _problems_ the _same_ _way_ |
| Settings | _Each_ _record_ _beside_ its _user_; the helpers in `Common/Infrastructure/Settings.cs` | `AreaSettings` and `TileSettings` in Domain, `DatabaseSettings` and `CatalogueSettings` in Infrastructure, `MapSettings` in _Features_; the _database_ variables' _names_ in `DatabaseConnections`. The _section_ _names_ in `tessera.json` _match_ ADR 0003 |
| The tile's address | `Domain/Tiles.cs` | `TileAddress.Of`, a smart constructor, _makes_ _only_ ***tiles*** on the _grid_ |
| Quoted names | `Domain/SqlName.cs` | `SqlName.Quote` and `SqlName.Qualified`, smart constructors, _make_ the _only_ _names_ that _reach_ a _query_'s _text_ |
| The layers | `Domain/Layers.cs` | `Layer`, and `LayerCatalogue` with `Find` |
| Every SQL text, and the database | `Infrastructure/Sql/` | `Queries.Tile` _takes_ a `Layer` and a `TileAddress`; `ConnectionString.FromUrl` _turns_ `DATABASE_URL` into a _connection_ _string_; `Database` _sends_ every _query_ |
| Reading the list of layers | `Infrastructure/Catalogue.cs` | `CatalogueRow` and `CatalogueReader` (`ToLayer`, `FromRows`, `Load`) _sit_ _beside_ the SQL, _since_ they _read_ the _rows_ of `Queries.Layers` and _quote_ their _names_; `LiveCatalogue` has a _file_ of its _own_ |
| The seed | `Infrastructure/Seeding/SeedScripts.cs` | DbUp _runs_ _each_ _new_ _script_ _once_ (ADR 0009) |
| The tile route | `Features/Tiles.cs` | `TileServer` and the _route_ |
| The map | `Features/Map.cs` | Its _headers_ _come_ from an ***endpoint filter*** (_below_) |
| The health checks | `Infrastructure/HealthChecks.cs` | `DatabaseAnswers` and `CatalogueReady` |
| `/health` | `Features/Health.cs` | The _status_ _comes_ from the _checks_ (_below_) |

_Features_ may _use_ Infrastructure, and Infrastructure may _use_ Domain; _nothing_ _points_ the _other_ _way_. The _tile_ _route_ therefore _reads_ `Database` and `Queries` _directly_. `TileServer.Answer` _takes_ the ***catalogue*** and the _reader_ as _functions_, so its _tests_ _need_ no _database_.

## ASP.NET Core in place of hand-written code

Tessera _hands_ the _framework_ every _job_ it can do, and _writes_ by _hand_ _only_ the _few_ it _cannot_.

**Three jobs the framework does:**

| Job | The framework's way | What it saves |
|---|---|---|
| The tile's ETag, and 304 | `TypedResults.Bytes(bytes, ContentType, entityTag: Tag(bytes))`, with `Tag` a _weak_ `EntityTagHeaderValue` | A hand-written _header_ and the _tag_'s _quoting_. The _service_ _answers_ a _matching_ `If-None-Match` with 304 _itself_, as Nginx does from its _kept_ _copy_. `TileResult` _keeps_ _one_ _job_: `Cache-Control` |
| `/health`'s status: UP, OUT_OF_SERVICE or DOWN, and the status code | _Two_ _checks_, `DatabaseAnswers` and `CatalogueReady` (Degraded _while_ no _list_ of _layers_ has _loaded_), and `HealthCheckOptions.ResultStatusCodes`, which _maps_ Degraded and Unhealthy to 503 | The _status_ _code_ and the two-way _test_. The writer _names_ the _status_, _since_ the JSON's _shape_ is _part_ of the _contract_, and _sends_ `no-store`, which the _tests_ _pin_ |
| The map's headers | An endpoint filter on the _map_'s ***route group***, `app.MapGroup("").AddEndpointFilter(...)` | A _check_ of every _answer_'s _content_ _type_. The _map_'s _headers_ _belong_ to the _map_'s _routes_ _alone_, which the _code_ _shows_ |

**Other jobs the framework does:** `AddProblemDetails` with `TypedResults.Problem`; `ValidateDataAnnotations` and `ValidateOnStart` for every _settings_ _section_; `AddHealthChecks`; `BackgroundService` for the catalogue's timer; `UseStaticFiles` for the _map_'s _files_; `UseExceptionHandler` and `UseStatusCodePages`.

**The module's door is two extension methods,** `AddTessera(this IHostApplicationBuilder)` and `MapTessera(this WebApplication)`. This is ASP.NET Core's _own_ idiom, as in `AddHealthChecks` and `MapHealthChecks`. The _template_'s `Common` _offers_ no _module_ _scheme_, and Tessera _adds_ _none_: no `IModule` interface, and no reflection scan to _find_ _modules_, _since_ such a scan _hides_ who _calls_ whom, which the ***boundary rules*** must _see_. A _second_ _module_ _brings_ its _own_ _two_ _methods_, and the _Host_ _calls_ them.

**Kept by hand, since the framework has no equal:** _reading_ `DATABASE_URL` as a URL (Npgsql _reads_ _only_ `key=value` _text_); _placing_ the _area_'s _box_ in the _map_ _page_; and `LayerOf`, which _knows_ the _nine_ _layer_ _pages_.

**`Database.Ping` keeps two limits.** A _cancel_ _after_ _one_ _second_, and `WaitAsync`, which _bounds_ the _ping_ _even_ when a _frozen_ _database_ _holds_ Npgsql _past_ its _own_ _cancel_; `/health` must _answer_ _within_ a _second_ (N-25).

**Taken from the template, or left out:**

| Template piece | Decision | Reason |
|---|---|---|
| `ServiceDefaults`: OpenTelemetry | _Optional_; not in the _code_ | It would _give_ _traces_ and _measures_ for _each_ _request_, at no _cost_ in Tessera's _code_ |
| `ServiceDefaults`: `MapDefaultEndpoints` | _Leave_ out | It _maps_ its _own_ `/health` and `/alive`, which would clash with Tessera's `/health` and its _contract_ |
| `ServiceDefaults`: the resilience handler | _Leave_ out | Tessera _calls_ no _other_ HTTP _service_, and ADR 0007 _puts_ the _limits_ in Nginx |
| EF Core, if the template uses it | _Leave_ out | ADR 0004 and S-01: Dapper and _plain_ SQL, all in `Queries.cs` |
| A mediator, if the template's Features use one | _Leave_ out | A _route_ _calls_ its _function_ _directly_; a mediator _adds_ a hop and _hides_ the _call_ |
| The build's analysers: .NET's own rules with `AnalysisMode` `All`, and Meziantou | _Take_ | They caught missed `ConfigureAwait` _calls_, undisposed catalogues in _tests_ and mutable _tile_ _bytes_. The _root_ `.editorconfig` _switches_ _off_ the _rules_ that clash with this codebase, _each_ with its _reason_ |
| The build's analysers: Sonar and Roslynator | _Leave_ out | On Tessera, Sonar mostly _repeated_ .NET's _rules_ or raised _false_ alarms, and Roslynator raised _nothing_ until _each_ _rule_ is _set_ by _hand_ |

`AddServerHeader = false` is _optional_ _too_, and not in the _code_.

## Keeping the coding style

The _layers_ _fit_ ADR 0004 _without_ strain, _since_ they _divide_ the _code_ where its _style_ _already_ _divides_ it:

- **Domain** _holds_ the read-only _records_ and the _pure_ _functions_: `TileAddress.Of`, `SqlName.Quote`, `LayerCatalogue.Find`, the _settings_ _records_ with their _own_ _checks_. It _knows_ no _database_ and no _web_, and a _rule_ _says_ so.
- **Infrastructure** _holds_ every _effect_ on the _database_. It _speaks_ `Result<T, DatabaseError>`, as `Database` does, and _never_ _throws_ for a _failure_ the _caller_ _expects_. `DatabaseError` is a `[Closed]` _union_ of _four_ _cases_, and the ExhaustiveMatching analyser _stops_ the _build_ when a _switch_ misses _one_.
- **Features** is the _edge_ where a `Result` _becomes_ an _answer_: `Match` _turns_ _success_ into a _tile_ and _each_ `DatabaseError` into 404, 503 or 500, as `TileServer.Answer` does. `MapRoutes.Render` is a _pure_ _function_ that the _route_ _calls_.
- **One `Result`** for the _whole_ repository, from CSharpFunctionalExtensions. A _value_ that may be _missing_ is a `T?`, which the _compiler_ _makes_ every _caller_ _check_, _never_ a stand-in _object_ (ADR 0004).

_Each_ _file_ _carries_ _comments_ that _give_ its _reasons_, _written_ for a _school_ _student_.

## The boundary rules

**A test project, `tests/Tessera.ArchitectureTests`, holds six rules.** _Each_ _rule_ _reads_ the _built_ _program_ and _fails_ with the _names_ of the _types_ that _break_ it.

```csharp
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Modules.Tessera;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Tessera.ArchitectureTests;

/// <summary>
/// The walls between the service's layers and modules (ADR 0010). Each rule reads the built program
/// and fails with the name of every type that breaks it, such as
/// <c>Modules.Tessera.Domain.Bad does depend on "Npgsql.NpgsqlConnection"</c>. The rules trust the
/// namespaces, and the build makes each namespace match its folder (IDE0130, src/Tessera/.editorconfig).
/// </summary>
public sealed class BoundaryTests
{
    // The service itself, found through a public type of its own.
    private static readonly System.Reflection.Assembly Ours = typeof(TesseraModule).Assembly;

    // The program with the libraries it uses. A rule only sees the types it has loaded: loaded alone,
    // the program's use of Npgsql would pass unseen.
    private static readonly Architecture Program =
        new ArchLoader().LoadAssembliesIncludingDependencies(Ours).Build();

    // The types in a namespace and in every namespace inside it, so 'Modules.Tessera.Infrastructure'
    // covers 'Modules.Tessera.Infrastructure.Sql' too.
    private static GivenTypesConjunction In(string space) =>
        Types().That().ResideInNamespaceMatching($@"^{Regex.Escape(space)}(\..+)?$");

    // Our own types alone: Npgsql's own types use Npgsql, and must not break the rules.
    private static GivenTypesConjunction OursIn(string space) => In(space).And().ResideInAssembly(Ours);

    // The libraries that talk to the database (S-01).
    private static IObjectProvider<IType> Database =>
        In("Npgsql").Or().ResideInNamespaceMatching(@"^(Dapper|DbUp)(\..+)?$").As("the database's libraries");

    // ASP.NET Core: requests, answers and routes.
    private static IObjectProvider<IType> Web => In("Microsoft.AspNetCore").As("ASP.NET Core");

    // The parts of the module no one outside it should see.
    private static IObjectProvider<IType> Workings =>
        In("Modules.Tessera.Infrastructure").Or().ResideInNamespaceMatching(@"^Modules\.Tessera\.Features(\..+)?$")
            .As("Tessera's Infrastructure or Features");

    // Rule 1: the Domain holds records and pure functions, so it needs no database, no web, and no
    // other layer of its module (ADR 0004).
    [Fact]
    public void Domain_stands_alone() =>
        OursIn("Modules.Tessera.Domain").Should().NotDependOnAny(Workings)
            .AndShould().NotDependOnAny(Database).AndShould().NotDependOnAny(Web)
            .Check(Program);

    // Rule 2: one gate to the database. Every SQL text and every connection lives in the
    // Infrastructure (S-01 c2).
    [Fact]
    public void Only_Infrastructure_talks_to_the_database() =>
        Types().That().ResideInAssembly(Ours)
            .And().DoNotResideInNamespaceMatching(@"^Modules\.Tessera\.Infrastructure(\..+)?$")
            .Should().NotDependOnAny(Database)
            .Check(Program);

    // Rule 3: one direction. Features use the Infrastructure, never the other way, and only Features
    // speak HTTP.
    [Fact]
    public void Infrastructure_knows_no_web() =>
        OursIn("Modules.Tessera.Infrastructure").Should().NotDependOnAny(Web)
            .AndShould().NotDependOnAny(In("Modules.Tessera.Features"))
            .Check(Program);

    // Rule 4: Common serves every module and reaches none, so a module can leave without taking
    // Common apart.
    [Fact]
    public void Common_never_reaches_a_module() =>
        OursIn("Common").Should().NotDependOnAny(In("Modules")).Check(Program);

    // Rule 5: another module reaches Tessera through its PublicApi alone. With one module, no type is
    // subject to this rule yet, and ArchUnitNET fails an empty rule by default; it waits for the second.
    [Fact]
    public void Other_modules_reach_Tessera_through_PublicApi_alone() =>
        OursIn("Modules").And().DoNotResideInNamespaceMatching(@"^Modules\.Tessera(\..+)?$")
            .Should().NotDependOnAny(In("Modules.Tessera").And().DoNotResideInNamespace("Modules.Tessera.PublicApi"))
            .WithoutRequiringPositiveResults()
            .Check(Program);

    // Rule 6: a module's public face shows no workings. PublicApi is empty for now; the rule waits for
    // its first type.
    [Fact]
    public void PublicApi_shows_no_workings() =>
        OursIn("Modules.Tessera.PublicApi").Should().NotDependOnAny(Workings)
            .WithoutRequiringPositiveResults()
            .Check(Program);
}
```

**The six rules pass on the clean code, and each fails on the breach it is meant to catch.** A breach planted in a _program_ laid out as the tree _above_, with ArchUnitNET 0.13.4 and xUnit v3, _fails_ its _rule_ with these _messages_:

| Breach planted | Rule that fails | ArchUnitNET's message |
|---|---|---|
| Domain makes an `NpgsqlConnection` | `Domain_stands_alone`, `Only_Infrastructure_talks_to_the_database` | `Modules.Tessera.Domain.Bad does depend on "Npgsql.NpgsqlConnection"` |
| Features reads Dapper's settings | `Only_Infrastructure_talks_to_the_database` | `…Features.Bad2 does depend on "Dapper.SqlMapper+Settings"` |
| Infrastructure returns `Results.Ok()` | `Infrastructure_knows_no_web` | `…Infrastructure.Bad3 does depend on "Microsoft.AspNetCore.Http.Results"` |
| Common makes a `TileAddress` | `Common_never_reaches_a_module` | `Common.Api.Bad4 does depend on "Modules.Tessera.Domain.TileAddress"` |
| A second module makes a `Layer` | `Other_modules_reach_Tessera_through_PublicApi_alone` | `Modules.Stops.Stops does depend on "Modules.Tessera.Domain.Layer"` |
| PublicApi exposes `Database` | `PublicApi_shows_no_workings` | `…PublicApi.ILayers does depend on "Modules.Tessera.Infrastructure.Database"` |

A _second_ _module_'s _use_ of `PublicApi` _passes_, as it should. A _file_ in Domain that _claims_ the Infrastructure's namespace _fails_ the _build_ with IDE0130.

**Three choices in the code above close ArchUnitNET's gaps:**

1. **Load the dependencies too.** A _rule_ _cannot_ _match_ a _type_ it has not _loaded_, and it _fails_ _open_: with `LoadAssemblies(ours)` _alone_, Domain's _use_ of Npgsql would _pass_ unseen. `LoadAssembliesIncludingDependencies` _loads_ them.
2. **Judge only our own types.** With Npgsql _loaded_, its _own_ _types_ would _break_ 'only Infrastructure talks to the database'. `ResideInAssembly(Ours)` _limits_ _each_ _rule_ to Tessera's _code_.
3. **Let empty rules wait.** ArchUnitNET _fails_ a _rule_ with no _types_ to _judge_: 'The rule requires positive evaluation'. With _one_ _module_ and an _empty_ PublicApi, _two_ _rules_ have _none_, so `WithoutRequiringPositiveResults()` _lets_ them _wait_ for the _code_ they _guard_.

**Two guards outside ArchUnitNET close the rest:**

- **Each namespace must match its folder.** The _rules_ trust namespaces, so a _file_ in Domain that _claims_ `namespace Modules.Tessera.Infrastructure` would escape them. _Rule_ IDE0130 at _error_ _level_, with `EnforceCodeStyleInBuild`, _makes_ the _build_ _refuse_ such a _file_.
- **The rules run first.** They _run_ at _test_ _time_, not _compile_ _time_, so `npm test` and the _build_ on GitHub _run_ the ***architecture tests*** _before_ the _service_'s _other_ _suites_, and a _broken_ _wall_ _fails_ in _about_ _eight_ _seconds_.

ArchUnitNET does not _see_ _calls_ _made_ _through_ reflection. Tessera _makes_ _none_, and the _module_'s _door_ is _plain_ _method_ _calls_, so the _rules_ _see_ every _path_.

## What stays outside the template

The ***pipeline*** in `etl/` (ADR 0009), the _scripts_, the _map_ in `web/`, the _seed_ _scripts_ and the Nginx _settings_ _stay_ _outside_ the _template_. The _pipeline_ is a _tool_ the _publisher_ _runs_, not a _module_ the _Host_ _serves_.

## What Common holds

The _template_'s `Common` is _empty_, so _nothing_ in it competes with Tessera's _code_. Tessera _supplies_ every _shared_ _piece_: `Problems` in `Common/Api`, and `Section<T>` in `Common/Infrastructure`. `Result` _comes_ from CSharpFunctionalExtensions, and a _missing_ _value_ is a `T?`.
