using System.Net;
using System.Net.Sockets;
using Common.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tessera.IntegrationTests;

/// <summary>
/// Helpers that run a route's answer without a whole web server, so a test of one route needs no
/// port and starts in a moment, and that find a free port for the tests that do start one.
/// </summary>
public static class Http
{
    /// <summary>
    /// Runs an answer and returns its status code alone, for the tests that check nothing more.
    /// The three helpers run an answer against an empty request, since the answer reads nothing
    /// from it.
    /// </summary>
    public static async Task<int> Status(IResult answer) => (await StatusAndType(answer)).Status;

    /// <summary>
    /// Runs an answer and returns its status code and content type. The services hold
    /// <c>Problems.Shape</c>, as the service's own do, so a <b><i>problem document</i></b> comes out
    /// as the service would send it (US-07 c5, N-22 c1).
    /// </summary>
    public static async Task<(int Status, string Type)> StatusAndType(IResult answer)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance).AddProblemDetails(options => options.CustomizeProblemDetails = Problems.Shape).BuildServiceProvider(),
        };
        context.Response.Body = new MemoryStream();
        await answer.ExecuteAsync(context);
        return (context.Response.StatusCode, context.Response.ContentType ?? "");
    }

    /// <summary>
    /// Runs an answer, shaped as above, and returns its status code and headers, for the tests of
    /// <c>Retry-After</c> and <c>Cache-Control</c> on a 503 (N-22 c1).
    /// </summary>
    public static async Task<(int Status, IHeaderDictionary Headers)> StatusAndHeaders(IResult answer)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance).AddProblemDetails(options => options.CustomizeProblemDetails = Problems.Shape).BuildServiceProvider(),
        };
        context.Response.Body = new MemoryStream();
        await answer.ExecuteAsync(context);
        return (context.Response.StatusCode, context.Response.Headers);
    }

    /// <summary>A port no other program is using, so each test's service has its own.</summary>
    public static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
