using Microsoft.AspNetCore.Http;

namespace Tessera;

/// <summary>
/// Every error the service answers is a problem document, a short report in JSON in the shape
/// RFC 9457 sets (US-07 c5). ASP.NET Core writes them; this class picks the status and adds two
/// headers to each.
/// </summary>
public static class Problems
{
    /// <summary>The seconds a client should wait after a 503 before it asks again (N-22).</summary>
    public const int RetryAfterSeconds = 5;

    public static IResult NotFound(string? detail = null) =>
        TypedResults.Problem(detail, statusCode: StatusCodes.Status404NotFound);

    /// <summary>503: the service is well but cannot answer now, such as while the database is down.</summary>
    public static IResult Unavailable(string detail) =>
        TypedResults.Problem(detail, statusCode: StatusCodes.Status503ServiceUnavailable);

    public static IResult Fault() => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);

    /// <summary>
    /// Shapes every problem document, whether a route, a crash or an unknown address made it. No
    /// browser keeps one (N-18 c3); a 503 asks the client to wait five seconds; and the body says
    /// 'about:blank' and carries no trace id.
    /// </summary>
    public static void Shape(ProblemDetailsContext context)
    {
        context.ProblemDetails.Type = "about:blank";
        context.ProblemDetails.Extensions.Remove("traceId");
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        if (context.ProblemDetails.Status == StatusCodes.Status503ServiceUnavailable)
            context.HttpContext.Response.Headers.RetryAfter =
                RetryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
