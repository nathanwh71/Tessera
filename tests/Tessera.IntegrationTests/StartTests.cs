using System.Diagnostics;

namespace Tessera.IntegrationTests;

/// <summary>US-00 c4 and test 2: started with no variables set, the program stops and names each one it lacks.</summary>
public sealed class StartTests
{
    private static async Task<(int Code, string Errors)> Run(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardError = true, RedirectStandardOutput = true };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Tessera.Service.dll"));
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        // Start from an empty environment, keeping only what .NET itself needs to run.
        start.Environment.Clear();
        foreach (var name in new[] { "PATH", "HOME", "DOTNET_ROOT" })
        {
            if (Environment.GetEnvironmentVariable(name) is { } value)
                start.Environment[name] = value;
        }

        using var process = Process.Start(start)!;
        var errors = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return (process.ExitCode, errors);
    }

    [Fact]
    public async Task Refuses_to_serve_and_names_each_missing_variable()
    {
        var (code, errors) = await Run();

        Assert.Equal(1, code);
        Assert.Contains("TESSERA_HTTP_PORT", errors, StringComparison.Ordinal);
        Assert.Contains("DATABASE_URL", errors, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refuses_to_import_and_names_the_publishers_variable()
    {
        var (code, errors) = await Run("import", "data/macau-pois.geojson");

        Assert.Equal(1, code);
        Assert.Contains("TESSERA_PUBLISHER_DATABASE_URL", errors, StringComparison.Ordinal);
    }
}
