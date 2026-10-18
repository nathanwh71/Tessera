using Microsoft.AspNetCore.Http.Features;

namespace Common.Api;

/// <summary>
/// Refuses a <b><i>request target</i></b>, the address after the method in a request's first line,
/// that hides what it is (S-06). Nginx refuses these first (nginx/default.conf.template); this
/// rule guards a copy of the service that a request reaches directly, past Nginx.
/// </summary>
/// <remarks>
/// Kestrel, ASP.NET Core's web server, tidies every address before a route sees it: it decodes
/// <c>%2e</c> to a dot, then drops each <c>..</c> folder with the folder before it. So
/// <c>/x/%2e%2e/food.cafes/0/0/0.pbf</c> once reached the tile route as <c>/food.cafes/0/0/0.pbf</c>
/// and answered 200, though S-06 c3 says a disguised target must reach no route. The rule here
/// reads the target as it was sent, before any tidying, so the disguise still shows.
/// </remarks>
public static class Targets
{
    /// <summary>
    /// True for a target that holds a raw control character (S-06 c1), or a folder of one or two
    /// dots with at least one written as <c>%2e</c>, in any case (S-06 c3). A dot folder written
    /// plainly is no disguise, and an encoded dot inside a longer name climbs nowhere, so both pass.
    /// The query string, after '?', names no folder, so only the path before it is read for dots.
    /// </summary>
    public static bool IsDisguised(string target) =>
        target.Any(char.IsControl) || PathOf(target).Split('/').Any(IsDotFolderInDisguise);

    /// <summary>The part of a target before its query string or fragment.</summary>
    private static string PathOf(string target) =>
        target.IndexOfAny(['?', '#']) is var end and >= 0 ? target[..end] : target;

    /// <summary>
    /// True for one folder of the path that is '.' or '..' once each <c>%2e</c> becomes a dot, and
    /// that held a '%' before: a plain '.' or '..' is no disguise.
    /// </summary>
    private static bool IsDotFolderInDisguise(string folder) =>
        folder.Contains('%', StringComparison.Ordinal)
        && folder.Replace("%2e", ".", StringComparison.OrdinalIgnoreCase) is "." or "..";

    /// <summary>
    /// Adds the step that answers a disguised target with a 400 problem document, before routing
    /// picks a route, so no route ever sees it. The answer names no part of the target (S-06 c2).
    /// </summary>
    public static IApplicationBuilder UseDisguisedTargetRefusal(this IApplicationBuilder app) =>
        app.Use((context, next) =>
            context.Features.Get<IHttpRequestFeature>() is { } request && IsDisguised(request.RawTarget)
                ? Problems.BadRequest().ExecuteAsync(context)
                : next(context));
}
