# Mapping Tessera onto the modular-monolith template

**The short answer.** Tessera _becomes_ _one_ ***module***, `Modules.Tessera`, in the _template_'s tree. Its _seven_ _projects_ _fold_ into _one_ _project_ with the _template_'s _four_ _folders_: Domain, Infrastructure, _Features_, and a PublicApi that _stays_ _empty_ until a _second_ _module_ _needs_ it. `Common/Api` _takes_ the _problem_ _answers_, and the _template_'s _Host_ _takes_ the _program_, which _calls_ `AddTessera` and `MapTessera` and _nothing_ more. The _code_ _keeps_ ADR 0004's _style_: _records_, ***pure functions*** and `Result` in, ASP.NET Core _answers_ out at the _Features_ _edge_. ASP.NET Core _takes_ over _three_ _jobs_ that Tessera _now_ _writes_ by _hand_, and the _template_'s _resilience_ _handler_ and _any_ heavier _data_ ***layer*** _stay_ out. _Six_ ArchUnitNET _rules_ _replace_ the _compiler_'s _walls_ _between_ _projects_; a sketch _built_ on .NET 10 _shows_ _each_ _rule_ _passing_ on _clean_ _code_ and _failing_ on a planted breach. The URLs, `tessera.json`, `Tessera seed`, the npm _scripts_ and Nginx do not _change_.

## What the move must keep

The _request_ _sets_ _three_ constraints, and _four_ _things_ must not _move_:

| Constraint | What it means here |
|---|---|
| Keep the coding style | ADR 0004 _holds_: read-only _records_, _pure_ _functions_, `Result<T, E>` for _failures_ the _caller_ _expects_, ***nullable types*** and _early_ _returns_, Dapper with _plain_ SQL, DbUp for ***seed scripts***, _comments_ for a _school_ _student_ |
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
Common/
  Api/                         Problems: the problem answers every module shares
  Infrastructure/              Settings.Section<T>: binds and checks one settings section
Modules/
  Tessera/
    Domain/                    TileAddress, Layer, LayerCatalogue, AreaSettings, TileSettings
    Infrastructure/
      Sql/                     Database, Connection, Queries
      Seeding/                 SeedScripts (DbUp)
      LiveCatalogue.cs         reads the layers, and reads them again on a timer
      DatabaseSettings.cs, CatalogueSettings.cs
    Features/
      Tiles.cs                 the tile route
      Map.cs                   the map page and its files
      Health.cs                /health
      MapSettings.cs
    PublicApi/                 empty
    TesseraModule.cs           AddTessera and MapTessera: the module's one door
Host/
  Program.cs                   serve, or seed
tests/
  Tessera.ArchitectureTests/   the six boundary rules
  Tessera.UnitTests/, Tessera.IntegrationTests/   as today, with new namespaces
```

`Modules.Tessera` is _one_ _project_, `Common` _one_ more, and the _Host_ a _third_. The _folders_ _match_ the _template_'s _names_, so a _reader_ who _knows_ the _template_ _finds_ _each_ _file_ where they _expect_ it. _Each_ namespace _matches_ its _folder_, and the _build_ _enforces_ it (_see_ the _rules_ _below_).

The _template_'s _other_ _pieces_, the _four_ _sample_ _modules_, `Common/Application` and `Common/Domain`, _carry_ _nothing_ Tessera _needs_. They _go_, or _stay_ _empty_. Aspire's AppHost _stays_ _optional_: `npm start` _still_ _runs_ the _service_.

## Where each file goes

| Today | After the move | Change |
|---|---|---|
| `Program.cs` | `Host/Program.cs` | _None_: _serve_ or _seed_, as _now_ |
| `Service.cs` | `Modules/Tessera/TesseraModule.cs` | _Splits_ into `AddTessera` (the _services_) and `MapTessera` (the _routes_); the _Host_ _calls_ both |
| `Problems/Problems.cs` | `Common/Api/Problems.cs` | _None_; every _module_ _answers_ _problems_ the _same_ _way_ |
| `Settings/Settings.cs` | _Each_ _record_ _beside_ its _user_; `Section<T>` to `Common/Infrastructure` | `AreaSettings` and `TileSettings` to Domain, `DatabaseSettings` and `CatalogueSettings` to Infrastructure, `MapSettings` to _Features_. The _section_ _names_ in `tessera.json` _stay_ |
| `Sql/*.cs` | `Infrastructure/Sql/` | `TileSource` _goes_: `Queries.Tile` _takes_ a `Layer` and a `TileAddress` |
| `Catalogue/Catalogue.cs` | _Split_ | `Layer`, `LayerCatalogue`, `FromRows` and `Find` to Domain, _since_ they are _pure_; `Load`, `CatalogueRow` and `LiveCatalogue` to Infrastructure |
| `Seeding/SeedScripts.cs` | `Infrastructure/Seeding/` | _None_ |
| `Tiles/Tiles.cs` | _Split_ | `TileAddress` to Domain; `TileServer` and the _route_ to _Features_ |
| `Map/Map.cs` | `Features/Map.cs` | Its _headers_ _move_ from a middleware to an ***endpoint filter*** (_below_) |

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

**The module's door is two extension methods,** `AddTessera(this IHostApplicationBuilder)` and `MapTessera(this IEndpointRouteBuilder)`. This is ASP.NET Core's _own_ idiom, as in `AddHealthChecks` and `MapHealthChecks`. If the _template_ _finds_ its _modules_ _through_ an `IModule` interface and reflection, Tessera does not _join_ that _scheme_: a reflection scan _hides_ who _calls_ whom, which is the very _thing_ the ***boundary rules*** must _see_.

**Kept by hand, since the framework has no equal:** _reading_ `DATABASE_URL` as a URL (Npgsql _reads_ _only_ `key=value` _text_); _placing_ the _area_'s _box_ in the _map_ _page_; `LayerOf`, which _knows_ the _nine_ _layer_ _pages_; and the latch that _lets_ _one_ _reread_ of the catalogue _run_ at a _time_.

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

- **Domain** _holds_ the read-only _records_ and the _pure_ _functions_: `TileAddress.Of`, `LayerCatalogue.FromRows` and `Find`, the _settings_ _records_ with their _own_ _checks_. It _knows_ no _database_ and no _web_, and a _rule_ _says_ so.
- **Infrastructure** _holds_ every _effect_ on the _database_. It _speaks_ `Result<T, DatabaseError>`, as `Database` does _now_, and _never_ _throws_ for a _failure_ the _caller_ _expects_.
- **Features** is the _edge_ where a `Result` _becomes_ an _answer_: `Match` _turns_ _success_ into a _tile_ and _each_ `DatabaseError` into 404, 503 or 500, as `TileServer.Answer` does _now_. `Map.Render` _stays_ a _pure_ _function_ that the _route_ _calls_.
- **One `Result` type** for the _whole_ _program_: CSharpFunctionalExtensions. The _template_'s `Common` _holds_ no `Result` of its _own_, so Tessera _brings_ this _one_, and no _code_ _converts_ _between_ _two_.

The _comments_ _move_ with their _code_. _Each_ _moved_ _file_ _keeps_ its _reasons_, and _each_ _new_ _file_, such as `TesseraModule.cs`, _gets_ _comments_ _written_ the _same_ _way_.

## The boundary rules

**A test project, `tests/Tessera.ArchitectureTests`, holds six rules.** They _replace_ the _compiler_ _check_ of ADR 0001, which _kept_ the _walls_ with _seven_ _projects_ and `DisableTransitiveProjectReferences`. _Each_ _rule_ _reads_ the _built_ _program_ and _fails_ with the _names_ of the _types_ that _break_ it.

```csharp
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Tessera.ArchitectureTests;

/// <summary>
/// The walls between the layers and the modules (ADR 0010). Each rule reads the built program and
/// fails with the name of every type that breaks it.
/// </summary>
public class BoundaryTests
{
    private static readonly System.Reflection.Assembly Ours = typeof(global::Program).Assembly;

    // With its dependencies, since a rule only sees the types it has loaded: loaded alone, the
    // program's use of Npgsql passed unseen.
    private static readonly Architecture Program =
        new ArchLoader().LoadAssembliesIncludingDependencies(Ours).Build();

    // A namespace and every namespace inside it.
    private static GivenTypesConjunction In(string space) =>
        Types().That().ResideInNamespaceMatching($@"^{Regex.Escape(space)}(\..+)?$");

    // Our own types only: Npgsql's types use Npgsql, and must not break the rules.
    private static GivenTypesConjunction OursIn(string space) => In(space).And().ResideInAssembly(Ours);

    private static IObjectProvider<IType> Database =>
        In("Npgsql").Or().ResideInNamespaceMatching(@"^(Dapper|DbUp)(\..+)?$").As("the database's libraries");

    private static IObjectProvider<IType> Web => In("Microsoft.AspNetCore").As("ASP.NET Core");

    private static IObjectProvider<IType> Workings =>
        In("Modules.Tessera.Infrastructure").Or().ResideInNamespaceMatching(@"^Modules\.Tessera\.Features(\..+)?$")
            .As("Tessera's Infrastructure or Features");

    [Fact]
    public void Domain_stands_alone() =>
        OursIn("Modules.Tessera.Domain").Should().NotDependOnAny(Workings)
            .AndShould().NotDependOnAny(Database).AndShould().NotDependOnAny(Web)
            .Check(Program);

    [Fact]
    public void Only_Infrastructure_talks_to_the_database() =>
        Types().That().ResideInAssembly(Ours)
            .And().DoNotResideInNamespaceMatching(@"^Modules\.Tessera\.Infrastructure(\..+)?$")
            .Should().NotDependOnAny(Database)
            .Check(Program);

    [Fact]
    public void Infrastructure_knows_no_web() =>
        OursIn("Modules.Tessera.Infrastructure").Should().NotDependOnAny(Web)
            .AndShould().NotDependOnAny(In("Modules.Tessera.Features"))
            .Check(Program);

    [Fact]
    public void Common_never_reaches_a_module() =>
        OursIn("Common").Should().NotDependOnAny(In("Modules")).Check(Program);

    // With one module, no type is subject to this rule yet; it waits for the second.
    [Fact]
    public void Other_modules_reach_Tessera_through_PublicApi_alone() =>
        OursIn("Modules").And().DoNotResideInNamespaceMatching(@"^Modules\.Tessera(\..+)?$")
            .Should().NotDependOnAny(In("Modules.Tessera").And().DoNotResideInNamespace("Modules.Tessera.PublicApi"))
            .WithoutRequiringPositiveResults()
            .Check(Program);

    // PublicApi is empty for now; the rule waits for its first type.
    [Fact]
    public void PublicApi_shows_no_workings() =>
        OursIn("Modules.Tessera.PublicApi").Should().NotDependOnAny(Workings)
            .WithoutRequiringPositiveResults()
            .Check(Program);
}
```

**The sketch proved each rule.** A .NET 10 _program_ laid out as the tree _above_, with ArchUnitNET 0.13.4 and xUnit v3, _passed_ all _six_ _rules_. _Six_ planted breaches _each_ _failed_ the _rule_ meant to _catch_ them:

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
- **The rules run first.** They _run_ at _test_ _time_, not _compile_ _time_, so `npm test` _runs_ the ***architecture tests*** _before_ the _others_, and a _broken_ _wall_ _fails_ _fast_.

ArchUnitNET does not _see_ _calls_ _made_ _through_ reflection. Tessera _makes_ _none_, and the _module_'s _door_ is _plain_ _method_ _calls_, so the _rules_ _see_ every _path_.

## The steps of the move

_Each_ _step_ is _one_ _commit_ and _leaves_ `npm test` _green_, the URLs the _same_ and `tessera.json` _unchanged_:

1. **Make the tree.** _Move_ the _files_ with `git mv` to their _new_ _folders_ and namespaces, and _fold_ the _seven_ _projects_ into `Modules.Tessera`, `Common` and the _Host_. The _code_ does not _change_.
2. **Add the boundary rules.** The _six_ _rules_ _pass_ on the _moved_ _code_; IDE0130 _joins_ the _build_.
3. **Split `Service.cs`** into `AddTessera` and `MapTessera`, and _point_ the ***integration tests*** at the _Host_.
4. **Hand the ETag and 304 to `TypedResults.Bytes`,** with a _test_ that a _matching_ _tag_ _gets_ 304.
5. **Move `/health`'s status into two checks** and `ResultStatusCodes`.
6. **Move the map's headers into an endpoint filter** on the _map_'s _route_ _group_.
7. **Drop `TileSource`;** `Queries.Tile` _takes_ `Layer`, `TileAddress` and `TileSettings`.
8. **Take OpenTelemetry from `ServiceDefaults`,** if _wanted_, _without_ `MapDefaultEndpoints`.
9. **Update the docs:** ADR 0010 _replaces_ ADR 0001's _compiler_ _check_ with the boundary _rules_; ADR 0008 _notes_ that the _service_ _also_ _answers_ 304; `reading-the-code.md` and the README _take_ the _new_ _paths_.

The ETL in `tools/`, the _scripts_, the _map_ in `web/`, the _seed_ _scripts_ and the Nginx _settings_ _stay_ _outside_ the _template_. The ETL is a _tool_ the _publisher_ _runs_, not a _module_ the _Host_ _serves_.

## What remains unknown

This _design_ _rests_ on the _template_'s tree and _one_ _fact_ _about_ its _code_: `Common` _holds_ no `Result` _type_, so CSharpFunctionalExtensions _serves_ the _whole_ _program_. `Common` may _still_ _carry_ a _way_ to _answer_ _problems_, a _module_ registration or a _settings_ helper of its _own_. Where it does, Tessera _uses_ the _template_'s _piece_ and deletes its _own_ _copy_, _under_ the _same_ _three_ constraints. _Reading_ the _template_'s `Common` _projects_ _settles_ this in an _hour_.
