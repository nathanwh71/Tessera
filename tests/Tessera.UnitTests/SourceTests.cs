using System.Text.RegularExpressions;
using Modules.Tessera.Features;

namespace Tessera.UnitTests;

/// <summary>
/// Rules about the source code itself, checked by reading the files as text: what the service's
/// code may name (SR-25), where its SQL may live (SR-06), and that the service and the map list
/// the same nine <b><i>layers</i></b> (US-23 c7). The compiler sees none of these, so a test reads
/// them.
/// </summary>
public sealed partial class SourceTests
{
    /// <summary>
    /// The repository's top folder: the first folder above the running tests that holds
    /// Tessera.slnx. The tests run from tests/*/bin/..., inside a checkout, so the search always
    /// ends.
    /// </summary>
    private static readonly string Root = FindRoot(new DirectoryInfo(AppContext.BaseDirectory));

    private static string FindRoot(DirectoryInfo? folder) =>
        folder is null ? throw new DirectoryNotFoundException("No Tessera.slnx above the tests")
        : File.Exists(Path.Combine(folder.FullName, "Tessera.slnx")) ? folder.FullName
        : FindRoot(folder.Parent);

    /// <summary>
    /// The service's own text files, each with its path from the repository's top, written with
    /// '/' on every system. bin and obj hold the build's output, not code anyone wrote, so they
    /// stay out.
    /// </summary>
    private static IEnumerable<SourceFile> ServiceFiles(params string[] extensions) =>
        Directory.EnumerateFiles(Path.Combine(Root, "src"), "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Root, file).Replace('\\', '/'))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal))
            .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.Ordinal))
            .Select(path => new SourceFile(path, File.ReadAllText(Path.Combine(Root, path))));

    /// <summary>One file of code: its path from the repository's top, and its text.</summary>
    private sealed record SourceFile(string Path, string Text);

    // Every pattern below gives up after a second, so a pathological line cannot hang the tests
    // (MA0009), and keeps only the groups it names (MA0023).
    private const RegexOptions Options = RegexOptions.ExplicitCapture;
    private const int Second = 1000;

    // ---- SR-25: the code names no machine ----

    // An address in dotted form, such as 192.168.10.5.
    [GeneratedRegex(@"\b\d{1,3}(?:\.\d{1,3}){3}\b", Options, Second)]
    private static partial Regex Ip();

    // The machine in a URL: the part after '//' and any 'user:password@', up to a colon, a slash
    // or a quote.
    [GeneratedRegex(@"[A-Za-z][A-Za-z0-9+.-]*://(?:[^/@\s""'<>]*@)?(?<machine>\[[^\]]*\]|[^/:\s""'<>]+)", Options, Second)]
    private static partial Regex UrlHost();

    // A machine named in a connection string, such as Host=db.office or Server=db.office.
    [GeneratedRegex(@"\b(?:Host|Server|Data Source)\s*=\s*(?<machine>[A-Za-z0-9._-]+)\s*;", Options | RegexOptions.IgnoreCase, Second)]
    private static partial Regex SettingHost();

    /// <summary>
    /// The machines code may name: the <b><i>loopback</i></b> address, by which a computer reaches
    /// itself, and the word 'host' that stands for a machine in the URL pattern Connection.cs
    /// prints. Every other machine comes from <c>DATABASE_URL</c> (SR-25, US-00 c4).
    /// </summary>
    private static bool Allowed(string machine) =>
        machine is "localhost" or "host" or "[::1]" || machine.StartsWith("127.", StringComparison.Ordinal);

    /// <summary>Every machine a text names, in the three forms the rule looks for.</summary>
    private static IEnumerable<string> Machines(string text) =>
        Ip().Matches(text).Select(match => match.Value)
            .Concat(UrlHost().Matches(text).Select(match => match.Groups["machine"].Value))
            .Concat(SettingHost().Matches(text).Select(match => match.Groups["machine"].Value))
            .Where(machine => !Allowed(machine));

    // The search itself must find a machine in each form, or the test below would pass by finding
    // nothing at all.
    [Theory]
    [InlineData("var url = \"postgresql://tessera:pw@db.office.lan:5432/tessera\";", "db.office.lan")]
    [InlineData("const string Server = \"192.168.10.5\";", "192.168.10.5")]
    [InlineData("\"Host=pg01;Port=5432;\"", "pg01")]
    public void Finds_a_machine_in_any_form(string text, string machine) =>
        Assert.Contains(machine, Machines(text));

    [Theory]
    [InlineData("\"Urls\": \"http://127.0.0.1:7800\"")]
    [InlineData("postgresql://user:password@host:port/database")]
    public void Lets_the_loopback_address_pass(string text) =>
        Assert.Empty(Machines(text));

    // SR-25 and US-00 c4: the service reads its database's machine from DATABASE_URL alone, so the
    // same build runs on the laptop and the office machine. No C# file, project file or settings
    // file names a machine, the loopback address aside.
    [Fact]
    public void Names_no_machine_in_the_code()
    {
        var found = ServiceFiles(".cs", ".csproj", ".json")
            .Append(new SourceFile("tessera.json", File.ReadAllText(Path.Combine(Root, "tessera.json"))))
            .SelectMany(file => Machines(file.Text).Select(machine => $"{file.Path}: {machine}"));

        Assert.Empty(found);
    }

    // ---- SR-06: every SQL text lives in Queries.cs ----

    /// <summary>
    /// The files allowed to hold SQL text (S-01 c2). Queries.cs holds every query, so a reviewer
    /// reads them all in one place; a new entry here is a decision, made in a commit of its own.
    /// </summary>
    private static readonly string[] SqlFiles = ["src/Tessera/Modules/Tessera/Infrastructure/Sql/Queries.cs"];

    // The start of an SQL statement, in capitals as Tessera writes SQL, such as 'SELECT ' or
    // 'DROP TABLE'.
    [GeneratedRegex(@"\b(?:SELECT\s|INSERT\s+INTO|UPDATE\s+\S+\s+SET|DELETE\s+FROM|(?:CREATE|DROP|ALTER)\s+(?:TABLE|SCHEMA|VIEW|INDEX|ROLE)|GRANT\s|REVOKE\s|TRUNCATE\s)", Options, Second)]
    private static partial Regex Statement();

    // A comment: a whole line that starts with '//' or '///', or the rest of a line after ' // '.
    // Comments may name SQL, such as 'SELECT 1' in a description, since no one sends them.
    [GeneratedRegex(@"^\s*//.*$|\s//\s.*$", Options | RegexOptions.Multiline, Second)]
    private static partial Regex Comment();

    private static bool HoldsSql(string code) => Statement().IsMatch(Comment().Replace(code, ""));

    // The search must see the SQL in Queries.cs, or the test below would pass by finding nothing.
    [Fact]
    public void Finds_the_sql_in_queries_cs() =>
        Assert.True(HoldsSql(File.ReadAllText(Path.Combine(Root, SqlFiles[0]))));

    [Theory]
    [InlineData("    // Sends SELECT 1 to wake the pool.", false)]
    [InlineData("    /// <c>x\"; DROP TABLE cafes</c> stays one odd name.", false)]
    [InlineData("    var sql = \"SELECT name FROM places\"; // the names", true)]
    [InlineData("    await Run(\"DROP TABLE food.cafes\");", true)]
    public void Tells_sql_from_a_comment_about_sql(string code, bool sql) =>
        Assert.Equal(sql, HoldsSql(code));

    // SR-06 and S-01 c2: every SQL text the service sends lives in Queries.cs, the seed's own
    // scripts in db/seed aside, which DbUp reads from files.
    [Fact]
    public void Keeps_every_sql_text_in_queries_cs() =>
        Assert.Empty(ServiceFiles(".cs")
            .Where(file => !SqlFiles.Contains(file.Path, StringComparer.Ordinal) && HoldsSql(file.Text))
            .Select(file => file.Path));

    // ---- US-23 c7: the service and the map list the same nine layers ----

    // A layer's id in the map's list, such as 'id: "food.cafes"'.
    [GeneratedRegex(@"\bid:\s*""(?<id>[^""]+)""", Options, Second)]
    private static partial Regex LayerId();

    // US-23 c2 and c7: the service opens a page for each layer the map names, and for no other.
    // The two lists live in two languages, MapRoutes.LayerIds in C# and LAYERS in
    // web/src/layers/layers.ts, so this test reads the map's file and compares the two, sorted.
    [Fact]
    public void Lists_the_same_nine_layers_as_the_map()
    {
        var map = LayerId().Matches(File.ReadAllText(Path.Combine(Root, "web", "src", "layers", "layers.ts")))
            .Select(match => match.Groups["id"].Value)
            .Order(StringComparer.Ordinal);

        Assert.Equal(map, MapRoutes.LayerIds.Order(StringComparer.Ordinal));
        Assert.Equal(9, MapRoutes.LayerIds.Count);
    }
}
