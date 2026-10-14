using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Tessera.Kernel;

namespace Tessera.Http;

/// <summary>
/// Writes every error the service answers as a problem document: a short report in JSON, a plain
/// text format for data, in the shape RFC 9457 sets (US-07 c5, ADR 0001 rule 4). This class sits in
/// the Http module. The tile and map routes and the target screen call it to refuse a request, and
/// TesseraService calls <see cref="UseProblemDocuments"/> to catch every other error.
/// </summary>
/// <remarks>
/// An RFC ('request for comments') is a published internet standard. RFC 9457 fixes one shape for
/// errors over HTTP, the rules a browser and a web server talk by, so a client, any program that
/// sends requests, reads every failure the same way. Only this module writes the body; a route that
/// fails returns <see cref="Answer"/> or one of its kin and leaves the writing here.
/// </remarks>
public static class Problems
{
    /// <summary>
    /// The content type, the label that tells a client the body is a problem document.
    /// </summary>
    public const string ContentType = "application/problem+json";

    /// <summary>
    /// An answer for a status code, the number that says how a request went, such as 404 for 'not
    /// found', with a sentence of detail where one helps. The answer carries the header, a line of
    /// extra information, <c>Cache-Control: no-store</c>, so no browser keeps a problem document
    /// and shows it again after the problem has gone (N-18 c3).
    /// </summary>
    public static IResult Answer(int status, Option<string> detail) => new ProblemResult(status, detail, Option.None<int>());

    /// <summary>
    /// The seconds a client should wait before it asks again after a 503 (N-22). The catalogue's
    /// first load retries at the same pace.
    /// </summary>
    public const int RetryAfterSeconds = 5;

    /// <summary>
    /// 503 'Service Unavailable', with the header <c>Retry-After: 5</c>, which asks the client to
    /// wait five seconds. The service is well but cannot answer now, such as while the database is
    /// down or busy, so a client should wait and ask again rather than report a fault (N-22).
    /// </summary>
    public static IResult Unavailable(string detail) =>
        new ProblemResult(StatusCodes.Status503ServiceUnavailable, Option.Some(detail), Option.Some(RetryAfterSeconds));

    // The answer itself: headers first, then the JSON body. Only a 503 carries Retry-After.
    private sealed record ProblemResult(int Status, Option<string> Detail, Option<int> RetryAfter) : IResult
    {
        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.Headers.CacheControl = "no-store";
            RetryAfter.Match(
                seconds => context.Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                () => default);
            return Results.Json(Body(Status, Detail), contentType: ContentType, statusCode: Status).ExecuteAsync(context);
        }
    }

    /// <summary>404 'Not Found', with a sentence of detail.</summary>
    public static IResult NotFound(string detail) => Answer(StatusCodes.Status404NotFound, Option.Some(detail));

    /// <summary>400 'Bad Request', with a sentence of detail.</summary>
    public static IResult BadRequest(string detail) => Answer(StatusCodes.Status400BadRequest, Option.Some(detail));

    /// <summary>
    /// Gives a problem document to every error no route wrote one for, such as a 404 for an
    /// address no route knows, or a crash, so no answer escapes the one shape. It adds two steps of
    /// middleware, the chain every request passes through: one catches a crash in any later step
    /// and answers 500; the other fills in an empty error answer.
    /// </summary>
    public static WebApplication UseProblemDocuments(this WebApplication app)
    {
        app.UseExceptionHandler(handler => handler.Run(context =>
        {
            // The body says nothing of the crash: a stack trace, the list of functions that were
            // running when it failed, would show strangers the code's insides.
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Write(context, StatusCodes.Status500InternalServerError, Option.None<string>());
        }));
        // An answer that has started, or already has a body, is left as it is.
        app.UseStatusCodePages(context =>
            context.HttpContext.Response.HasStarted || context.HttpContext.Response.ContentLength > 0
                ? Task.CompletedTask
                : Write(context.HttpContext, context.HttpContext.Response.StatusCode, Option.None<string>()));
        return app;
    }

    // For the steps above and the target screen, which write the answer at once rather than return it.
    internal static Task Write(HttpContext context, int status, Option<string> detail) =>
        Answer(status, detail).ExecuteAsync(context);

    private static ProblemBody Body(int status, Option<string> detail) => new(
        "about:blank",
        // With type about:blank, RFC 9457 asks for the status code's own phrase, such as 'Not Found'.
        ReasonPhrases.GetReasonPhrase(status),
        status,
        detail.Match<string?>(text => text, () => null));

    // The JSON body. Detail is null, and so left out, when no sentence helps. Elsewhere the code marks
    // 'maybe no value' with Option; this record is the edge where Option gives way to null, since
    // the JSON writer knows nothing of Option (ADR 0004).
    private sealed record ProblemBody(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("detail"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Detail);
}
