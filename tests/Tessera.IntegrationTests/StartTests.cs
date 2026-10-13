using System.Diagnostics;

namespace Tessera.IntegrationTests;

/// <summary>US-00 c4 and test 2, and N-23 c4: the program stops when a setting is missing, and starts when only the database is.</summary>
public sealed class StartTests
{
    private static Task<(int Code, string Errors)> Run(params string[] arguments) =>
        Run(new Dictionary<string, string>(), arguments);

    private static async Task<(int Code, string Errors)> Run(IReadOnlyDictionary<string, string> variables, params string[] arguments)
    {
        using var process = Process.Start(Start(variables, arguments))!;
        var errors = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return (process.ExitCode, errors);
    }

    private static ProcessStartInfo Start(IReadOnlyDictionary<string, string> variables, params string[] arguments)
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

        foreach (var (name, value) in variables)
            start.Environment[name] = value;
        return start;
    }

    [Fact]
    public async Task Refuses_to_serve_and_names_each_missing_variable()
    {
        var (code, errors) = await Run();

        Assert.Equal(1, code);
        Assert.Contains("TESSERA_HTTPPORT", errors, StringComparison.Ordinal);
        Assert.Contains("DATABASE_URL", errors, StringComparison.Ordinal);
    }

    // N-23 c4 and test 4: with the database down, the program starts anyway and asks each browser
    // to wait, so the service and the database may start in either order.
    [Fact]
    public async Task Starts_with_the_database_down_and_answers_503()
    {
        var port = Http.FreePort();
        var start = Start(new Dictionary<string, string>
        {
            ["TESSERA_HTTPPORT"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["DATABASE_URL"] = $"postgresql://tessera_reader:x@127.0.0.1:{Http.FreePort()}/tessera",
        });
        using var program = Process.Start(start)!;
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            HttpResponseMessage? answer = null;
            for (var attempt = 0; attempt < 100 && answer is null; attempt++)
            {
                try
                {
                    answer = await client.GetAsync("/food.cafes/0/0/0.pbf", TestContext.Current.CancellationToken);
                }
                catch (HttpRequestException)
                {
                    // Not listening yet: the program is still starting.
                    await Task.Delay(100, TestContext.Current.CancellationToken);
                }
            }

            Assert.NotNull(answer);
            Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, answer.StatusCode);
            Assert.Equal(TimeSpan.FromSeconds(5), answer.Headers.RetryAfter?.Delta);
            Assert.False(program.HasExited);
            answer.Dispose();
        }
        finally
        {
            program.Kill(entireProcessTree: true);
            await program.WaitForExitAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Refuses_to_import_and_names_the_publishers_variable()
    {
        var (code, errors) = await Run("import", "data/macau-pois.geojson");

        Assert.Equal(1, code);
        Assert.Contains("TESSERA_PUBLISHER_DATABASE_URL", errors, StringComparison.Ordinal);
    }
}
