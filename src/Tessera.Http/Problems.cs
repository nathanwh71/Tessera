using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Tessera.Kernel;

namespace Tessera.Http;

/// <summary>
/// Writes every error the HTTP API answers as a problem document, the JSON shape RFC 9457 sets
/// (US-07 c5, ADR 0001 rule 4).
/// </summary>
/// <remarks>
/// An RFC ("request for comments") is a published internet standard. RFC 9457 fixes one shape for
/// errors over HTTP, so a client reads every failure the same way. Only this module writes one; a
/// route that fails returns <see cref="Answer"/> and lets this module write the body.
/// </remarks>
public static class Problems
{
    public const string ContentType = "application/problem+json";

    /// <summary>An answer for a status code, with a sentence of detail where one helps.</summary>
    public static IResult Answer(int status, Option<string> detail) =>
        Results.Json(Body(status, detail), contentType: ContentType, statusCode: status);

    public static IResult NotFound(string detail) => Answer(StatusCodes.Status404NotFound, Option.Some(detail));

    public static IResult BadRequest(string detail) => Answer(StatusCodes.Status400BadRequest, Option.Some(detail));

    /// <summary>
    /// Gives a bare error, such as a 404 for an address no route knows or a crash, a problem
    /// document too, so no answer escapes the one shape.
    /// </summary>
    public static WebApplication UseProblemDocuments(this WebApplication app)
    {
        app.UseExceptionHandler(handler => handler.Run(context =>
        {
            // The body says nothing of the crash: a stack trace would show strangers the code's insides.
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Write(context, StatusCodes.Status500InternalServerError, Option.None<string>());
        }));
        app.UseStatusCodePages(context =>
            context.HttpContext.Response.HasStarted || context.HttpContext.Response.ContentLength > 0
                ? Task.CompletedTask
                : Write(context.HttpContext, context.HttpContext.Response.StatusCode, Option.None<string>()));
        return app;
    }

    internal static Task Write(HttpContext context, int status, Option<string> detail) =>
        Answer(status, detail).ExecuteAsync(context);

    private static ProblemBody Body(int status, Option<string> detail) => new(
        "about:blank",
        // With type about:blank, RFC 9457 asks for the status code's own phrase, such as 'Not Found'.
        ReasonPhrases.GetReasonPhrase(status),
        status,
        detail.Match<string?>(text => text, () => null));

    // The JSON body. Detail is null, so left out, when no sentence helps; this record is the edge
    // where an Option meets the JSON writer, which knows nothing of Option (ADR 0004).
    private sealed record ProblemBody(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("detail"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Detail);
}
