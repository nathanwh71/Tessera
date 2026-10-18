using System.Diagnostics;

namespace Tessera.IntegrationTests;

/// <summary>
/// US-00 c4 and test 2, N-23 c3, and ADR 0009: the program stops when a setting is missing, for
/// serving and for seeding alike, and starts when only the database is down.
/// </summary>
public sealed class StartTests
{
    /// <summary>
    /// Runs the built program in a process of its own, with only the given
    /// <b><i>environment variable</i></b>s, and returns its exit code and error output. Only a real
    /// start shows what a person at the command line sees; the first form passes no variables.
    /// </summary>
    private static Task<(int Code, string Errors)> Run(params string[] arguments) =>
        Run(new Dictionary<string, string>(), arguments);

    private static async Task<(int Code, string Errors)> Run(IReadOnlyDictionary<string, string> variables, params string[] arguments)
    {
        using var process = Process.Start(Start(variables, arguments))!;
        var errors = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return (process.ExitCode, errors);
    }

    /// <summary>
    /// How to start the program, with its output caught so a test can read it. The tests that keep
    /// the program running build on this directly.
    /// </summary>
    private static ProcessStartInfo Start(IReadOnlyDictionary<string, string> variables, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Tessera.dll"));
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

        // With no variables, the program stops and names DATABASE_URL, the one setting for serving
        // that tessera.json leaves out (ADR 0003).
        Assert.Equal(1, code);
        Assert.Contains("DATABASE_URL", errors, StringComparison.Ordinal);
    }

    // US-00 c4 and N-21 c4: one failed start names every problem, a misspelt key among them, and
    // stops with the same exit code as any other settings problem.
    [Fact]
    public async Task Names_a_misspelt_key_with_every_other_problem()
    {
        var (code, errors) = await Run(new Dictionary<string, string>
        {
            ["Tiles__CacheSecond"] = "5",
            ["Tiles__MaxZoom"] = "31",
            ["Database__PoolSize"] = "0",
        });

        Assert.Equal(1, code);
        Assert.Contains("Tiles: 'CacheSecond' is not a setting", errors, StringComparison.Ordinal);
        Assert.Contains("MaxZoom", errors, StringComparison.Ordinal);
        Assert.Contains("PoolSize", errors, StringComparison.Ordinal);
        Assert.Contains("DATABASE_URL", errors, StringComparison.Ordinal);
    }

    // N-23 c3 and test 3, and N-25: with the database down, the program starts anyway and asks each
    // browser to wait, so the service and the database may start in either order.
    [Fact]
    public async Task Starts_with_the_database_down_and_answers_503()
    {
        var port = Http.FreePort();
        var start = Start(new Dictionary<string, string>
        {
            ["Urls"] = $"http://127.0.0.1:{port}",
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

            // N-25 c2 and N-28 c5: /health says DOWN within a second, and names no fault. A tile is
            // refused within half a second. The tile above has borne the first request's warm-up,
            // so the clock times /health alone: 25 to 40 ms in runs on 18 October 2026, with the
            // whole suite running beside it.
            var clock = Stopwatch.StartNew();
            using var health = await client.GetAsync("/health", TestContext.Current.CancellationToken);
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1), $"/health took {clock.Elapsed}");
            Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, health.StatusCode);
            Assert.Equal("""{"status":"DOWN"}""", await health.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

            clock.Restart();
            using var again = await client.GetAsync("/food.cafes/0/0/0.pbf", TestContext.Current.CancellationToken);
            Assert.True(clock.Elapsed < TimeSpan.FromMilliseconds(500), $"the tile took {clock.Elapsed}");
            Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, again.StatusCode);
        }
        finally
        {
            program.Kill(entireProcessTree: true);
            await program.WaitForExitAsync(TestContext.Current.CancellationToken);
        }
    }

    // ADR 0009: 'Tessera seed' signs in as tessera_publisher, a role with more rights than the
    // reader's, so it reads its own variable and never falls back on DATABASE_URL.
    [Fact]
    public async Task Refuses_to_seed_and_names_the_publishers_variable()
    {
        var (code, errors) = await Run("seed");

        Assert.Equal(1, code);
        Assert.Contains("TESSERA_PUBLISHER_DATABASE_URL", errors, StringComparison.Ordinal);
    }

    // The seed reads the publisher's URL alone, so a fault in a section only the service reads,
    // such as a zoom range turned inside out, is the service's to report, not the seed's.
    [Fact]
    public async Task Seeds_without_reading_the_services_settings()
    {
        var (code, errors) = await Run(
            new Dictionary<string, string> { ["Tiles__MinZoom"] = "10", ["Tiles__MaxZoom"] = "2" }, "seed");

        Assert.Equal(1, code);
        Assert.Equal("TESSERA_PUBLISHER_DATABASE_URL: missing or empty", errors.Trim());
    }
}
