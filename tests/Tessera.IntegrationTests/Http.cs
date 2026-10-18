using System.Net;
using System.Net.Sockets;
using Common.Api;
using Microsoft.AspNetCore.Http;
// The project file imports CSharpFunctionalExtensions into every file here, and it has an IResult of
// its own; this names ASP.NET Core's.
using IResult = Microsoft.AspNetCore.Http.IResult;
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

    /// <summary>
    /// Sends one request written byte for byte and returns the whole answer, headers and body, as
    /// text. HttpClient tidies an address before it sends it: it turns <c>%2e%2e</c> into <c>..</c>
    /// and drops the folder before it, and refuses a control character. An attacker's own program
    /// does neither, so the tests of disguised and malformed addresses (S-06) send them this way.
    /// </summary>
    /// <param name="port">The port the service listens on, on this machine.</param>
    /// <param name="target">The request target exactly as it travels, such as <c>/x/%2e%2e/a</c>.</param>
    /// <param name="cancel">The test's own cancel signal, so a test stopped early stops waiting.</param>
    public static async Task<string> Raw(int port, string target, CancellationToken cancel)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, cancel);
        await using var stream = client.GetStream();
        // Latin-1 maps each character to the one byte of the same number, so a control character
        // such as \u0001 travels as the single byte 0x01, untouched.
        var request = $"GET {target} HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(System.Text.Encoding.Latin1.GetBytes(request), cancel);
        using var answer = new MemoryStream();
        await stream.CopyToAsync(answer, cancel);
        return System.Text.Encoding.Latin1.GetString(answer.ToArray());
    }

    /// <summary>A port no other program is using, so each test's service has its own.</summary>
    public static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
