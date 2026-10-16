namespace Common.Api;

/// <summary>
/// Every error the service answers is a <b><i>problem document</i></b>, a short report in JSON in the
/// shape RFC 9457 sets (US-07 c5), so every client reads every error the same way. ASP.NET Core
/// writes them; this class picks the status and adds two headers to each.
/// </summary>
public static class Problems
{
    /// <summary>The seconds a client should wait after a 503 before it asks again (N-22).</summary>
    public const int RetryAfterSeconds = 5;

    /// <summary>
    /// 404: no such <b><i>layer</i></b>, <b><i>tile</i></b> or page (US-07 c4), with no detail, since
    /// the status alone often says enough.
    /// </summary>
    public static IResult NotFound() => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound);

    /// <summary>404, with a detail that says what was not found.</summary>
    public static IResult NotFound(string detail) =>
        TypedResults.Problem(detail, statusCode: StatusCodes.Status404NotFound);

    /// <summary>503: the service is well but cannot answer now, such as while the database is down.</summary>
    public static IResult Unavailable(string detail) =>
        TypedResults.Problem(detail, statusCode: StatusCodes.Status503ServiceUnavailable);

    /// <summary>
    /// 500: the fault is the service's own (N-22 c4). It carries no detail, so a stranger learns
    /// nothing of the service's insides; the log holds the cause.
    /// </summary>
    public static IResult Fault() => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);

    /// <summary>
    /// Shapes every problem document, whether a route, a crash or an unknown address made it. No
    /// browser keeps one (N-18 c3); a 503 asks the client to wait five seconds (N-22 c1). The type
    /// says 'about:blank', so the title is the status's own phrase, such as 'Not Found' (US-07 c5),
    /// and the body carries no trace id, which would tell a stranger of the service's insides.
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
