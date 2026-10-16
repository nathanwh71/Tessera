# Mapping Tessera onto the modular-monolith template

**Status.** _Built_ on 16 _October_ 2026, with every _test_ _passing_; 'As built' at the _end_ _records_ where the _code_ _differs_ from this _design_, and why.

**The short answer.** Tessera _becomes_ _one_ ***module***, `Modules.Tessera`, in the _template_'s tree. Its _seven_ _projects_ _fold_ into _one_ _project_ with the _template_'s _four_ _folders_: Domain, Infrastructure, _Features_, and a PublicApi that _stays_ _empty_ until a _second_ _module_ _needs_ it. `Common/Api` _takes_ the _problem_ _answers_, and the _template_'s _Host_ _takes_ the _program_, which _calls_ `AddTessera` and `MapTessera` and _nothing_ more. The _code_ _keeps_ ADR 0004's _style_: _records_, ***pure functions*** and `Result` in, ASP.NET Core _answers_ out at the _Features_ _edge_. ASP.NET Core _takes_ over _three_ _jobs_ that Tessera _now_ _writes_ by _hand_, and the _template_'s _resilience_ _handler_ and _any_ heavier _data_ ***layer*** _stay_ out. _Six_ ArchUnitNET _rules_ _replace_ the _compiler_'s _walls_ _between_ _projects_; a sketch _built_ on .NET 10 _shows_ _each_ _rule_ _passing_ on _clean_ _code_ and _failing_ on a planted breach. The URLs, `tessera.json`, `Tessera seed`, the npm _scripts_ and Nginx do not _change_.

## What the move must keep

The _request_ _sets_ _three_ constraints, and _four_ _things_ must not _move_:

| Constraint | What it means here |
|---|---|
| Keep the coding style | ADR 0004 _holds_: read-only _records_, _pure_ _functions_, `Result<T, E>` for _failures_ the _caller_ _expects_, `Maybe<T>` for _values_ that may be _missing_, _early_ _returns_, Dapper with _plain_ SQL, DbUp for ***seed scripts***, _comments_ for a _school_ _student_ |
| Prefer ASP.NET Core to hand-written code | Where the _framework_ _already_ does a _job_, Tessera's _own_ _code_ for it _goes_; where it does not, Tessera's _code_ _stays_ |
| Guard the boundaries with ArchUnitNET | _Tests_, not _project_ _references_, _keep_ _each_ _layer_ and _each_ _module_ in its _place_ |

| Must not move | Why |
|---|---|
| The URLs: `/{layer}/{z}/{x}/{y}.pbf`, `/`, `/{layer}.html`, `/health` | Nginx, the _map_ and every _saved_ _link_ _use_ them |
| `tessera.json` and its sections, and `DATABASE_URL` | ADR 0003; the _machines_' _settings_ must _keep_ _working_ |
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
                               Settings.cs (TileSettings, AreaSettings)
    Infrastructure/            Sql/ (Connection, Database, Queries), Seeding/ (SeedScripts),
                               Catalogue.cs (CatalogueRow, CatalogueReader), LiveCatalogue.cs,
                               HealthChecks.cs (DatabaseAnswers, CatalogueReady),
                               Settings.cs (DatabaseSettings, CatalogueSettings)
    Features/                  Tiles.cs, Map.cs, Health.cs, Settings.cs (MapSettings)
tests/
  Tessera.ArchitectureTests/   the six boundary rules
  Tessera.UnitTests/, Tessera.IntegrationTests/   as before, with new namespaces
```

The _whole_ _service_ is _one_ _project_. The _folders_ _match_ the _template_'s _names_, so a _reader_ who _knows_ the _template_ _finds_ _each_ _file_ where they _expect_ it. _Each_ namespace _matches_ its _folder_, and the _build_ _enforces_ it (_see_ the _rules_ _below_).

The _template_'s `Common` is _empty_, so Tessera _fills_ _two_ of its _folders_, `Api` and `Infrastructure`, and _adds_ no _empty_ _ones_; nor does the _module_ _carry_ an _empty_ `PublicApi`. _Each_ _folder_ _appears_ with its _first_ _file_. The _four_ _sample_ _modules_ _carry_ _nothing_ Tessera _needs_, and _go_. Aspire's AppHost _stays_ _optional_: `npm start` _still_ _runs_ the _service_.

## Where each file goes

| Before | After the move | Change |
|---|---|---|
| `Program.cs` | `Program.cs` | The _seed_ _reads_ the _publisher_'s URL _alone_ |
| `Service.cs` | `TesseraHost.cs` and `Modules/Tessera/TesseraModule.cs` | The _Host_ _builds_ the _program_ and _runs_ the _steps_ every _request_ _passes_; the _module_'s _door_ _adds_ its _services_ (`AddTessera`) and _routes_ (`MapTessera`) |
| `Problems/Problems.cs` | `Common/Api/Problems.cs` | _None_; every _module_ _answers_ _problems_ the _same_ _way_ |
| `Settings/Settings.cs` | _Each_ _record_ _beside_ its _user_; the helpers to `Common/Infrastructure/Settings.cs` | `AreaSettings` and `TileSettings` to Domain, `DatabaseSettings` and `CatalogueSettings` to Infrastructure, `MapSettings` to _Features_; the _database_ variables' _names_ to `DatabaseConnections`. The _section_ _names_ in `tessera.json` _stay_ |
| `Sql/*.cs` | `Infrastructure/Sql/` | `TileSource` _goes_: `Queries.Tile` _takes_ a `Layer` and a `TileAddress` |
| `Catalogue/Catalogue.cs` | _Split_ | `Layer` and `LayerCatalogue` with `Find` to Domain; `CatalogueRow` and `CatalogueReader` (`ToLayer`, `FromRows`, `Load`) to Infrastructure, _since_ _turning_ a _row_ into a _layer_ _quotes_ its _names_ for SQL; `LiveCatalogue` to a _file_ of its _own_ |
| `Seeding/SeedScripts.cs` | `Infrastructure/Seeding/` | _None_ |
| `Tiles/Tiles.cs` | _Split_ | `TileAddress` to Domain; `TileServer` and the _route_ to _Features_ |
| `Map/Map.cs` | `Features/Map.cs` | Its _headers_ _move_ from a middleware to an ***endpoint filter*** (_below_) |
| The database's health check, in `Service.cs` | `Infrastructure/HealthChecks.cs` | _Joined_ by `CatalogueReady` |
| `/health`'s route, in `Service.cs` | `Features/Health.cs` | The _status_ _comes_ from the _checks_ (_below_) |

_Features_ may _use_ Infrastructure, and Infrastructure may _use_ Domain; _nothing_ _points_ the _other_ _way_. The ***tile*** _route_ therefore _reads_ `Database` and `Queries` _directly_, as it does _now_. `TileServer.Answer` _still_ _takes_ the ***catalogue*** and the _reader_ as _functions_, so its _tests_ _still_ _need_ no _database_.

## ASP.NET Core in place of hand-written code

Tessera _already_ leans on the _framework_ for _most_ of its plumbing. The _move_ _keeps_ all of that, _takes_ _three_ more _jobs_ from it, and _keeps_ the _few_ _pieces_ the _framework_ _cannot_ do.

**Three jobs the framework takes over:**

| Today's code | The framework's way | What goes |
|---|---|---|
| `TileResult` writes the ETag header by hand, and Nginx alone answers 304 | `TypedResults.Bytes(bytes, ContentType, entityTag: new EntityTagHeaderValue(tag, isWeak: true))` | The hand-written _header_ and the _tag_'s _quoting_. The _service_ _now_ _answers_ a _matching_ `If-None-Match` with 304 _itself_; the sketch _shows_ 200, then 304. `TileResult` _keeps_ _one_ _job_: `Cache-Control`. Nginx _still_ _answers_ 304 from its _kept_ _copy_, so ADR 0008 _changes_ _one_ _sentence_ |
| `/health` works out UP, OUT_OF_SERVICE or DOWN, and the status code, in its writer | _Two_ _checks_, `DatabaseAnswers` and `CatalogueReady` (Degraded _while_ no _list_ of _layers_ has _loaded_), and `HealthCheckOptions.ResultStatusCodes`, which _maps_ Degraded and Unhealthy to 503 | The _status_ _code_ and the two-way _test_. The writer shrinks to _naming_ the _status_, _since_ the JSON's _shape_ is _part_ of the _contract_, and _keeps_ its `no-store` _line_, which the _tests_ _pin_ |
| `UseMapHeaders`, a middleware that inspects every answer for `text/html` | An endpoint filter on the _map_'s ***route group***, `app.MapGroup("").AddEndpointFilter(...)` | The _check_ on the _content_ _type_. The _map_'s _headers_ _belong_ to the _map_'s _routes_ _alone_, which the _code_ then _shows_ |

**Already the framework's, and kept:** `AddProblemDetails` with `TypedResults.Problem`; `ValidateDataAnnotations` and `ValidateOnStart` for every _settings_ _section_; `AddHealthChecks`; `BackgroundService` for the catalogue's timer; `UseStaticFiles` for the _map_'s _files_; `UseExceptionHandler` and `UseStatusCodePages`.

**The module's door is two extension methods,** `AddTessera(this IHostApplicationBuilder)` and `MapTessera(this WebApplication)`. This is ASP.NET Core's _own_ idiom, as in `AddHealthChecks` and `MapHealthChecks`. The _template_'s `Common` _offers_ no _module_ _scheme_, and Tessera _adds_ _none_: no `IModule` interface, and no reflection scan to _find_ _modules_, _since_ such a scan _hides_ who _calls_ whom, the very _thing_ the ***boundary rules*** must _see_. A _second_ _module_ _brings_ its _own_ _two_ _methods_, and the _Host_ _calls_ them.

**Kept by hand, since the framework has no equal:** _reading_ `DATABASE_URL` as a URL (Npgsql _reads_ _only_ `key=value` _text_); _placing_ the _area_'s _box_ in the _map_ _page_; and `LayerOf`, which _knows_ the _nine_ _layer_ _pages_. The latch that _let_ _one_ _reread_ of the catalogue _run_ at a _time_ _went_ with the _reread_ _itself_ (_below_).

**Taken from the template, or left out:**

| Template piece | Decision | Reason |
|---|---|---|
| `ServiceDefaults`: OpenTelemetry | _Take_ | _Traces_ and _measures_ for _each_ _request_, at no _cost_ in Tessera's _code_ |
| `ServiceDefaults`: `MapDefaultEndpoints` | _Leave_ out | It _maps_ its _own_ `/health` and `/alive`, which would clash with Tessera's `/health` and its _contract_ |
| `ServiceDefaults`: the resilience handler | _Leave_ out | Tessera _calls_ no _other_ HTTP _service_, and ADR 0007 _puts_ the _limits_ in Nginx |
| EF Core, if the template uses it | _Leave_ out | ADR 0004 and S-01: Dapper and _plain_ SQL, all in `Queries.cs` |
| A mediator, if the template's Features use one | _Leave_ out | A _route_ _calls_ its _function_ _directly_; a mediator _adds_ a hop and _hides_ the _call_ |

## Keeping the coding style

The _layers_ _fit_ ADR 0004 _without_ strain, _since_ they _divide_ the _code_ where its _style_ _already_ _divides_ it:

- **Domain** _holds_ the read-only _records_ and the _pure_ _functions_: `TileAddress.Of`, `LayerCatalogue.Find`, the _settings_ _records_ with their _own_ _checks_. It _knows_ no _database_ and no _web_, and a _rule_ _says_ so.
- **Infrastructure** _holds_ every _effect_ on the _database_. It _speaks_ `Result<T, DatabaseError>`, as `Database` does _now_, and _never_ _throws_ for a _failure_ the _caller_ _expects_.
- **Features** is the _edge_ where a `Result` _becomes_ an _answer_: `Match` _turns_ _success_ into a _tile_ and _each_ `DatabaseError` into 404, 503 or 500, as `TileServer.Answer` does _now_. `Map.Render` _stays_ a _pure_ _function_ that the _route_ _calls_.
- **One `Result` and one `Maybe`** for the _whole_ repository, both from CSharpFunctionalExtensions. A _value_ that may be _missing_ is a `Maybe<T>`, _never_ `null` or a stand-in _object_; a _null_ from a _library_ _becomes_ a `Maybe` at that _edge_ (ADR 0004).

The _comments_ _move_ with their _code_. _Each_ _moved_ _file_ _keeps_ its _reasons_, and _each_ _new_ _file_, such as `TesseraModule.cs`, _gets_ _comments_ _written_ the _same_ _way_.

## The boundary rules

**A test project, `tests/Tessera.ArchitectureTests`, holds six rules.** They _replace_ the _compiler_ _check_ of ADR 0001, which _kept_ the _walls_ with _seven_ _projects_ and `DisableTransitiveProjectReferences`. _Each_ _rule_ _reads_ the _built_ _program_ and _fails_ with the _names_ of the _types_ that _break_ it.

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

**The sketch proved each rule, and the service proves two again.** In the _built_ _service_, a _class_ planted in Domain that _makes_ an `NpgsqlConnection` _failed_ `Domain_stands_alone` and `Only_Infrastructure_talks_to_the_database`, and a _file_ in Domain that claimed the Infrastructure's namespace _failed_ the _build_ with IDE0130. A .NET 10 _program_ laid out as the tree _above_, with ArchUnitNET 0.13.4 and xUnit v3, _passed_ all _six_ _rules_. _Six_ planted breaches _each_ _failed_ the _rule_ meant to _catch_ them:

| Breach planted | Rule that failed | ArchUnitNET's message |
|---|---|---|
| Domain makes an `NpgsqlConnection` | `Domain_stands_alone`, `Only_Infrastructure_talks_to_the_database` | `Modules.Tessera.Domain.Bad does depend on "Npgsql.NpgsqlConnection"` |
| Features reads Dapper's settings | `Only_Infrastructure_talks_to_the_database` | `…Features.Bad2 does depend on "Dapper.SqlMapper+Settings"` |
| Infrastructure returns `Results.Ok()` | `Infrastructure_knows_no_web` | `…Infrastructure.Bad3 does depend on "Microsoft.AspNetCore.Http.Results"` |
| Common makes a `TileAddress` | `Common_never_reaches_a_module` | `Common.Api.Bad4 does depend on "Modules.Tessera.Domain.TileAddress"` |
| A second module makes a `Layer` | `Other_modules_reach_Tessera_through_PublicApi_alone` | `Modules.Stops.Stops does depend on "Modules.Tessera.Domain.Layer"` |
| PublicApi exposes `Database` | `PublicApi_shows_no_workings` | `…PublicApi.ILayers does depend on "Modules.Tessera.Infrastructure.Database"` |

The _same_ _module_'s _use_ of `PublicApi` _passed_, as it should. The _six_ _rules_ _ran_ in _about_ _six_ _seconds_, _most_ of it _loading_ the _program_ and its _libraries_.

**The sketch taught three lessons, now in the code above:**

1. **Load the dependencies too.** With `LoadAssemblies(ours)` _alone_, Domain's _use_ of Npgsql _passed_ unseen: a _rule_ _cannot_ _match_ a _type_ it has not _loaded_, and it _fails_ _open_. `LoadAssembliesIncludingDependencies` _fixed_ it.
2. **Judge only our own types.** _Once_ Npgsql was _loaded_, its _own_ _types_ _broke_ 'only Infrastructure talks to the database'. `ResideInAssembly(Ours)` _limits_ _each_ _rule_ to Tessera's _code_.
3. **Empty rules fail by default.** With _one_ _module_ and an _empty_ PublicApi, _two_ _rules_ had no _types_ to _judge_, and ArchUnitNET _failed_ them: 'The rule requires positive evaluation'. `WithoutRequiringPositiveResults()` _lets_ them _wait_ for the _code_ they _guard_.

**Two guards outside ArchUnitNET close its gaps:**

- **Each namespace must match its folder.** The _rules_ trust namespaces, so a _file_ in Domain that _claims_ `namespace Modules.Tessera.Infrastructure` would escape them. _Rule_ IDE0130 at _error_ _level_, with `EnforceCodeStyleInBuild`, _makes_ the _build_ _refuse_ such a _file_.
- **The rules run first.** They _run_ at _test_ _time_, not _compile_ _time_, so `npm test` and the _build_ on GitHub _run_ the ***architecture tests*** _before_ the _service_'s _other_ _suites_, and a _broken_ _wall_ _fails_ in _about_ _eight_ _seconds_.

ArchUnitNET does not _see_ _calls_ _made_ _through_ reflection. Tessera _makes_ _none_, and the _module_'s _door_ is _plain_ _method_ _calls_, so the _rules_ _see_ every _path_.

## The steps of the move, as done

_Each_ _step_ is _one_ _commit_, and _left_ every _test_ _passing_, the URLs the _same_ and `tessera.json` _unchanged_:

1. **ADR 0010** _records_ the _decision_.
2. **The tree.** The _eight_ _projects_ folded into _one_, the _files_ _moved_ with `git mv`, and `TileSource` _went_.
3. **The boundary rules,** with IDE0130 in `src/Tessera/.editorconfig`, _run_ _first_ by `npm test` and the _build_ on GitHub.
4. **The split start-up:** `TesseraHost` _builds_ the _program_; `AddTessera` and `MapTessera` are the _module_'s _door_.
5. **A home-made `Result`,** _later_ undone: CSharpFunctionalExtensions _came_ _back_ the _same_ _day_, with `Maybe` in _place_ of `null` (_step_ 12).
6. **The ETag and 304** from `TypedResults.Bytes`, with a _test_ that a _matching_ _tag_ _gets_ 304.
7. **`/health`'s status** from _two_ ***health checks*** and `ResultStatusCodes`, with a _test_ of `CatalogueReady`.
8. **The map's headers** from an endpoint filter on the _page_'s _routes_.
9. **The dropped-column reread cut:** 42703 _still_ _answers_ 503, and the _list_ heals at its _next_ refresh.
10. **The leaner seed:** `Tessera seed` _reads_ the _publisher_'s URL _alone_, with a _test_ that a _bad_ `Tiles` _section_ does not _stop_ it.
11. **The docs:** ADR 0001 superseded in _part_, ADR 0004 and ADR 0008 _amended_, the README and the _reading_ _guides_ on the _new_ _paths_.
12. **CSharpFunctionalExtensions back,** with its `Result`, and its `Maybe` in _place_ of `null` and stand-in _objects_, _first_ in the _service_, then in the ETL; ADR 0004 _amended_.

OpenTelemetry from `ServiceDefaults` and `AddServerHeader = false` _stay_ _optional_, and are not _done_.

The ETL, _now_ a repository of its _own_, the _scripts_, the _map_ in `web/`, the _seed_ _scripts_ and the Nginx _settings_ _stay_ _outside_ the _template_. The ETL is a _tool_ the _publisher_ _runs_, not a _module_ the _Host_ _serves_.

## What Common holds

The _template_'s `Common` is _empty_, so _nothing_ in it competes with Tessera's _code_, and the _design_ _keeps_ no _open_ _question_ _about_ it. Tessera _supplies_ every _shared_ _piece_: `Problems` in `Common/Api`, `Section<T>` in `Common/Infrastructure`, and CSharpFunctionalExtensions for `Result` and `Maybe`. _Each_ _piece_ _moves_ as it _stands_, so the _move_ _adds_ no _shared_ _code_. _Only_ the _sample_ _modules_ may _still_ _use_ EF _Core_ or a mediator, and they _go_.

## As built

The _code_ _follows_ this _design_, with _four_ _differences_:

| Design | Built | Reason |
|---|---|---|
| A Host project beside the module's | `TesseraHost.cs` at the _project_'s _root_, in the global namespace | _One_ _project_ _holds_ the _whole_ _service_, so the _Host_ is a _file_, not a _project_ |
| A home-made `Result` in `Common/Result.cs` | CSharpFunctionalExtensions' `Result` and `Maybe` | The _maintainer_ _chose_ the _library_, so `Maybe` could _replace_ `null` and stand-in _objects_ _too_; a home-made _type_ would have had to _give_ both |
| `Database.Ping` keeps one limit | Both _limits_ _stay_ | The _second_, `WaitAsync`, _bounds_ the _ping_ _even_ when a _frozen_ _database_ _holds_ Npgsql _past_ its _own_ _cancel_; `/health` must _answer_ _within_ a _second_ (N-25) |
| `Layer`, `LayerCatalogue` and `FromRows` in Domain | `FromRows` and `ToLayer` in the Infrastructure, as `CatalogueReader` | _Turning_ a _row_ into a _layer_ _quotes_ its _names_ with `Queries.Quote`, so it _belongs_ _beside_ the SQL; the _first_ _rule_ would _refuse_ it in Domain |
