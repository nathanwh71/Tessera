using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Tessera.Kernel;

namespace Tessera.Http;

/// <summary>
/// Refuses a malformed request target before any route runs (S-06).
/// </summary>
/// <remarks>
/// The request target is the address after GET in the first line of a request, such as
/// <c>/food.cafes/15/27154/14280.pbf</c>. A malformed one is disguised: <c>%2e%2e</c> is "..",
/// percent-encoded, and the web server would tidy it into a step up the folder tree before routing
/// saw it. So this screen reads the raw target, exactly as it arrived, and never repairs it.
/// Percent-encoding writes a character as '%' and two hexadecimal digits: '/' is %2F and '.' is %2E.
/// </remarks>
public static class TargetScreen
{
    /// <summary>The first problem with a raw target, or none if it is clean.</summary>
    public static Option<string> Problem(string rawTarget)
    {
        var path = rawTarget.Split('?', 2)[0];
        return rawTarget.Any(character => character < 0x20 || character == 0x7F) ? Option.Some("a control character")
            : StrayPercent(rawTarget) ? Option.Some("a '%' without two hexadecimal digits")
            : EncodesControl(rawTarget) ? Option.Some("an encoded control character")
            : path.Contains('\\') || path.Contains("%5c", StringComparison.OrdinalIgnoreCase) ? Option.Some("a backslash")
            : path.Contains("%2f", StringComparison.OrdinalIgnoreCase) ? Option.Some("an encoded slash")
            : path.Split('/').Any(DotSegment) ? Option.Some("a dot segment")
            : Option.None<string>();
    }

    /// <summary>
    /// Screens every request first. The answer names the kind of problem but never repeats the
    /// target, since a disguised address can carry anything (S-06 c2).
    /// </summary>
    public static WebApplication UseTargetScreen(this WebApplication app)
    {
        app.Use((context, next) =>
            context.Features.Get<IHttpRequestFeature>() is { } request
                ? Problem(request.RawTarget).Match(
                    problem => Problems.Write(context, StatusCodes.Status400BadRequest, Option.Some($"The address holds {problem}.")),
                    () => next(context))
                : next(context));
        return app;
    }

    private static bool DotSegment(string segment) =>
        segment.Replace("%2e", ".", StringComparison.OrdinalIgnoreCase) is "." or "..";

    private static bool StrayPercent(string target) =>
        target.Select((character, index) => (character, index))
            .Any(pair => pair.character == '%'
                         && (pair.index + 2 >= target.Length
                             || !Uri.IsHexDigit(target[pair.index + 1])
                             || !Uri.IsHexDigit(target[pair.index + 2])));

    // %00 to %1F and %7F decode to control characters, which no tile address holds.
    private static bool EncodesControl(string target) =>
        target.Select((character, index) => (character, index))
            .Any(pair => pair.character == '%'
                         && pair.index + 2 < target.Length
                         && Convert.ToInt32(target.Substring(pair.index + 1, 2), 16) is < 0x20 or 0x7F);
}
