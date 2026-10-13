using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tessera.IntegrationTests;

/// <summary>Helpers for reading an answer, without a whole web server, and for raw requests.</summary>
public static class Http
{
    /// <summary>Runs an answer against an empty request and returns its status code.</summary>
    public static async Task<int> Status(IResult answer) => (await StatusAndType(answer)).Status;

    /// <summary>Runs an answer against an empty request and returns its status code and content type.</summary>
    public static async Task<(int Status, string Type)> StatusAndType(IResult answer)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance).BuildServiceProvider(),
        };
        context.Response.Body = new MemoryStream();
        await answer.ExecuteAsync(context);
        return (context.Response.StatusCode, context.Response.ContentType ?? "");
    }

    /// <summary>Runs an answer against an empty request and returns its status code and headers.</summary>
    public static async Task<(int Status, IHeaderDictionary Headers)> StatusAndHeaders(IResult answer)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance).BuildServiceProvider(),
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

    /// <summary>
    /// Sends a request byte for byte over a plain network connection, so nothing tidies the target
    /// on the way, as a browser or HttpClient would (S-06 test 1).
    /// </summary>
    public static async Task<string> Raw(int port, string target)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        await using var stream = client.GetStream();
        var request = Encoding.Latin1.GetBytes($"GET {target} HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(request);
        using var reader = new StreamReader(stream, Encoding.Latin1);
        return await reader.ReadToEndAsync();
    }
}
