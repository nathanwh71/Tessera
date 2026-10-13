using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Tessera.ArchitectureTests;

/// <summary>
/// The rules at the module borders that the compiler cannot check alone (ADR 0001). Each test reads
/// the compiled projects, finds every type and every call between them, and fails the build if a
/// rule breaks.
/// </summary>
public sealed class BorderTests
{
    // The compiled modules and the two database packages, loaded once for every rule.
    private static readonly Architecture Tessera = new ArchLoader().LoadAssemblies(
        typeof(Kernel.Option).Assembly,
        typeof(Settings.SettingsReader).Assembly,
        typeof(Sql.Query).Assembly,
        typeof(Catalogue.LayerCatalogue).Assembly,
        typeof(Import.Importer).Assembly,
        typeof(Tiles.TileQuery).Assembly,
        typeof(Http.Problems).Assembly,
        typeof(Map.MapRoutes).Assembly,
        typeof(Service.TesseraService).Assembly,
        typeof(Npgsql.NpgsqlConnection).Assembly,
        typeof(Dapper.SqlMapper).Assembly,
        // ASP.NET Core's own problem-document helpers, so the rules can see a call to one.
        typeof(Microsoft.AspNetCore.Http.Results).Assembly,
        typeof(Microsoft.AspNetCore.Mvc.ProblemDetails).Assembly,
        typeof(Microsoft.AspNetCore.Http.IProblemDetailsService).Assembly).Build();

    // Dependencies are matched by assembly, not namespace: some types a dependency names, such as a
    // generic type's placeholder, have no namespace at all.
    private static readonly IObjectProvider<IType> DatabaseClient =
        Types().That().ResideInAssembly(typeof(Npgsql.NpgsqlConnection).Assembly, typeof(Dapper.SqlMapper).Assembly);

    private static readonly IObjectProvider<IType> Modules =
        Types().That().ResideInAssembly(
            typeof(Settings.SettingsReader).Assembly, typeof(Sql.Query).Assembly, typeof(Catalogue.LayerCatalogue).Assembly,
            typeof(Import.Importer).Assembly, typeof(Tiles.TileQuery).Assembly, typeof(Http.Problems).Assembly,
            typeof(Map.MapRoutes).Assembly, typeof(Service.TesseraService).Assembly);

    // Rule 3, S-01 c2: only Sql uses the database client. The compiler already hides Npgsql and
    // Dapper from every other project; this test repeats the check, as S-01 asks for one.
    [Fact]
    public void Only_sql_touches_the_database_client() =>
        Types().That().ResideInNamespaceMatching(@"^Tessera(\.|$)").And().DoNotResideInAssembly(typeof(Sql.Query).Assembly)
            .Should().NotDependOnAny(DatabaseClient)
            .Check(Tessera);

    // Rule 4, US-07: only Http writes a problem document, so every error keeps one shape.
    [Fact]
    public void Only_http_writes_problem_documents() =>
        Types().That().ResideInNamespaceMatching(@"^Tessera(\.|$)").And().DoNotResideInNamespaceMatching(@"^Tessera\.Http(\.|$)")
            .Should().NotDependOnAny(Types().That().HaveNameMatching("^(ProblemDetails|ProblemHttpResult|IProblemDetailsService|HttpValidationProblemDetails|ValidationProblem)$"))
            .AndShould().NotCallAny(MethodMembers().That().HaveNameMatching(@"^(Problem|ValidationProblem)\b"))
            .Check(Tessera);

    // Every module may lean on the Kernel; the Kernel leans on none of them.
    [Fact]
    public void The_kernel_depends_on_no_module() =>
        Types().That().ResideInAssembly(typeof(Kernel.Option).Assembly)
            .Should().NotDependOnAny(Modules)
            .Check(Tessera);

    // The two cases of Option, the two of Result and the three of Page, which only their own Match may take apart.
    private static readonly IObjectProvider<IType> Cases =
        Types().That().ResideInAssembly(typeof(Kernel.Option).Assembly, typeof(Map.Page).Assembly)
            .And().HaveNameMatching("^(Some|None|Ok|Refused|Shown|Off|Missing)$");

    // ADR 0004: no code outside a type takes its cases apart with a switch or an "is"; it calls
    // Match, so a new case cannot be forgotten anywhere. The provider is counted first, since a rule
    // over no types would pass for nothing.
    [Fact]
    public void Only_match_takes_option_result_and_page_apart()
    {
        Assert.Equal(7, Cases.GetObjects(Tessera).Count());
        Types().That().ResideInNamespaceMatching(@"^Tessera(\.|$)")
            .And().DoNotResideInAssembly(typeof(Kernel.Option).Assembly)
            .And().AreNot(typeof(Map.Page)).And().AreNot(Cases)
            .Should().NotDependOnAny(Cases)
            .Check(Tessera);
    }
}
