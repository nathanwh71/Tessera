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
