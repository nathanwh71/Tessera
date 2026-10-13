using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using Tessera.Catalogue;
using Tessera.Http;
using Tessera.Map;
using Tessera.Settings;
using Tessera.Sql;
using Tessera.Tiles;

namespace Tessera.Service;

/// <summary>
/// Puts the modules together into one web service. This project is the only one that references
/// every module; the modules never reference it (ADR 0001).
/// </summary>
public static class TesseraService
{
    /// <summary>
    /// Builds the service from settings that have passed their checks and a catalogue already
    /// loaded. The order of the middleware matters: each request meets them top to bottom.
    /// </summary>
    /// <remarks>
    /// Middleware is a chain of steps every request passes through on its way in. The error handler
    /// comes first so it can catch a failure in any later step; the target screen comes before any
    /// route, so a disguised address never reaches one (S-06).
    /// The service builds on an empty host, so it reads no appsettings.json: tessera.json and the
    /// environment hold every setting (ADR 0003).
    /// </remarks>
    public static WebApplication Build(ServiceSettings settings, LayerCatalogue catalogue, Database database, string mapFolder)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Any, settings.Port));
        builder.Services.AddRoutingCore();
        builder.Logging.AddConsole();

        var app = builder.Build();
        app.UseProblemDocuments();
        app.UseTargetScreen();
        app.UseMapHeaders();
        app.UseRouting();
        app.MapTiles(catalogue, database, settings.Tiles);
        app.UseMap(settings.Area, settings.Map, mapFolder);
        return app;
    }
}
