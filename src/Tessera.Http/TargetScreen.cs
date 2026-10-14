using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Tessera.Kernel;

namespace Tessera.Http;

/// <summary>
/// Refuses a malformed request target before any route, the code that answers one kind of address,
/// runs (S-06). This class sits in the Http module; TesseraService calls
/// <see cref="UseTargetScreen"/> once at start-up, and from then on every request passes this screen.
/// </summary>
/// <remarks>
/// <para>
/// The request target is the address after GET in the first line of a request, such as
/// <c>/food.cafes/15/27154/14280.pbf</c>. An attacker may disguise a harmful target by encoding it.
/// Percent-encoding writes a character as '%' and two hexadecimal (base 16) digits: '/' is %2F and
/// '.' is %2E. So <c>%2e%2e</c> is '..', and the web server would tidy it into a step up the folder
/// tree before routing saw it. This screen therefore reads the raw target, exactly as it arrived,
/// and never repairs it.
/// </para>
/// <para>
/// A letter, a digit, '-', '.', '_' or '~' never needs encoding (RFC 3986 section 2.3; an RFC is a
/// published internet standard), so an encoded one, such as %61 for 'a', serves only to disguise an
/// address, and the screen refuses it too. A space or a quote may be encoded, since a layer's name
/// may hold one.
/// </para>
/// </remarks>
public static class TargetScreen
{
    /// <summary>
    /// The first problem found in a raw target, or none if it is clean. Three checks look at the
    /// path alone, the part before any '?': a plain backslash, a dot segment and an encoded plain
    /// character. Every other check covers the whole target.
    /// </summary>
    public static Option<string> Problem(string rawTarget)
    {
        var path = rawTarget.Split('?', 2)[0];
        return !rawTarget.StartsWith('/') ? Option.Some("no leading slash")
            : rawTarget.Any(character => character < 0x20 || character == 0x7F) ? Option.Some("a control character")
            : StrayPercent(rawTarget) ? Option.Some("a '%' without two hexadecimal digits")
            : EncodesControl(rawTarget) ? Option.Some("an encoded control character")
            // An encoded slash or backslash is refused after the '?' too, as the Java build, this
            // service's twin written in Java, refuses it (ADR 0006 rule 7).
            : path.Contains('\\') || rawTarget.Contains("%5c", StringComparison.OrdinalIgnoreCase) ? Option.Some("a backslash")
            : rawTarget.Contains("%2f", StringComparison.OrdinalIgnoreCase) ? Option.Some("an encoded slash")
            : path.Split('/').Any(DotSegment) ? Option.Some("a dot segment")
            : EncodesPlain(path) ? Option.Some("an encoded plain character")
            : Option.None<string>();
    }

    /// <summary>
    /// Adds the screen as a step of middleware, the chain of steps every request passes through on
    /// its way in, so it screens each request before any route sees it. A bad target gets the status
    /// code 400 'Bad Request', the number that tells the browser the request was malformed, with no
    /// detail at all, as in the Java build. The answer never repeats the
    /// target, since a disguised address can carry anything (S-06 c2), and it never names the kind
    /// of problem, which would tell an attacker which trick the screen caught (ADR 0006 rule 7).
    /// <see cref="Problem"/> still names it, for the tests.
    /// </summary>
    public static WebApplication UseTargetScreen(this WebApplication app)
    {
        app.Use((context, next) =>
            context.Features.Get<IHttpRequestFeature>() is { } request
                ? Problem(request.RawTarget).Match(
                    _ => Problems.Write(context, StatusCodes.Status400BadRequest, Option.None<string>()),
                    () => next(context))
                : next(context));
        return app;
    }

    // A dot segment is '.' or '..' between slashes, plain or encoded: a step that stays put or climbs.
    private static bool DotSegment(string segment) =>
        segment.Replace("%2e", ".", StringComparison.OrdinalIgnoreCase) is "." or "..";

    // A '%' must have two hexadecimal digits after it; anything else is malformed.
    private static bool StrayPercent(string target) =>
        target.Select((character, index) => (character, index))
            .Any(pair => pair.character == '%'
                         && (pair.index + 2 >= target.Length
                             || !Uri.IsHexDigit(target[pair.index + 1])
                             || !Uri.IsHexDigit(target[pair.index + 2])));

    // %00 to %1F and %7F decode to control characters, invisible codes such as 'new line', which no
    // tile address holds.
    private static bool EncodesControl(string target) =>
        Encoded(target).Any(code => code is < 0x20 or 0x7F);

    // The characters RFC 3986 says never need encoding.
    private static bool EncodesPlain(string path) =>
        Encoded(path).Any(code => code is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '.' or '_' or '~');

    // The code of each percent-encoded character, in order; the caller has already refused a stray '%'.
    private static IEnumerable<int> Encoded(string target) =>
        target.Select((character, index) => (character, index))
            .Where(pair => pair.character == '%' && pair.index + 2 < target.Length)
            .Select(pair => Convert.ToInt32(target.Substring(pair.index + 1, 2), 16));
}
