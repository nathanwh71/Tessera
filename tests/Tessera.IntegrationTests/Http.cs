using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using IResult = Microsoft.AspNetCore.Http.IResult;
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
            RequestServices = new ServiceCollection().AddLogging().AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance).AddProblemDetails(options => options.CustomizeProblemDetails = Problems.Shape).BuildServiceProvider(),
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
