using Common.Api;
using Common.Infrastructure;
using Microsoft.Extensions.Options;
using Modules.Tessera.Domain;
using Modules.Tessera.Features;
using Modules.Tessera.Infrastructure;
using Modules.Tessera.Infrastructure.Sql;

namespace Modules.Tessera;

/// <summary>
/// The Tessera <b><i>module</i></b>'s one door: what the Host calls to add the module to the
/// program. <see cref="AddTessera"/> adds its services and <see cref="MapTessera"/> its routes, as
/// ASP.NET Core's own <c>AddHealthChecks</c> and <c>MapHealthChecks</c> do. A second module would
/// bring a door of its own, and the Host would call it too (ADR 0010).
/// </summary>
public static class TesseraModule
{
    /// <summary>
    /// Binds each of the module's sections of tessera.json to its record (Settings.Section). The
    /// database's URL is read apart, by DatabaseConnections.Register, since only the Infrastructure
    /// knows the database client.
    /// </summary>
    public static void AddSettings(IServiceCollection services)
    {
        Settings.Section<DatabaseSettings>(services, "Database");
        Settings.Section<TileSettings>(services, "Tiles");
        Settings.Section<MapSettings>(services, "Map");
        Settings.Section<CatalogueSettings>(services, "Catalogue");
        Settings.Section<AreaSettings>(services, "Area");
    }

    /// <summary>
    /// Adds the module's settings, its one pool of database connections, the list of layers that
    /// loads in the background, and the database's <b><i>health check</i></b>.
    /// </summary>
    public static IHostApplicationBuilder AddTessera(this IHostApplicationBuilder builder)
    {
        AddSettings(builder.Services);
        DatabaseConnections.Register(builder.Services, builder.Configuration, DatabaseConnections.ReaderDatabase);

        // One Database for the whole program, since it holds the one pool of connections that every
        // request shares within the limits of N-22.
        builder.Services.AddSingleton(services => Database.Open(
            services.GetRequiredService<IOptions<DatabaseConnection>>().Value.Text,
            services.GetRequiredService<IOptions<DatabaseSettings>>().Value));
        // One list of layers, which every request shares, loads in the background, so the service
        // answers from its first moment (N-23 c3). Before the first load it tries every five seconds,
        // as N-23 c3 sets: the same wait Retry-After asks of each browser.
        builder.Services.AddSingleton(services => new LiveCatalogue(
            cancel => CatalogueReader.Load(services.GetRequiredService<Database>(), cancel),
            TimeSpan.FromSeconds(services.GetRequiredService<IOptions<CatalogueSettings>>().Value.RefreshSeconds),
            TimeSpan.FromSeconds(Problems.RetryAfterSeconds),
            services.GetRequiredService<ILoggerFactory>().CreateLogger("Tessera.Catalogue")));
        // The background loop is the same object the tiles read, so a refresh reaches every request.
        builder.Services.AddHostedService(services => services.GetRequiredService<LiveCatalogue>());
        // N-25: the database must answer a ping within a second, through the same pool as the tiles,
        // and the first list of layers must have loaded.
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseAnswers>("database")
            .AddCheck<CatalogueReady>("catalogue");
        return builder;
    }

    /// <summary>
    /// Adds the module's routes: <c>/health</c>, the tiles, and the map, whose files the release keeps
    /// in wwwroot beside the program (Tessera.csproj, US-23 c4). Each route's pattern, not this order,
    /// decides which one answers.
    /// </summary>
    public static WebApplication MapTessera(this WebApplication app)
    {
        var catalogue = app.Services.GetRequiredService<LiveCatalogue>();
        app.MapHealth();
        app.MapTiles(
            catalogue,
            app.Services.GetRequiredService<Database>(),
            app.Services.GetRequiredService<IOptions<TileSettings>>().Value);
        app.UseMap(
            app.Services.GetRequiredService<IOptions<AreaSettings>>().Value,
            app.Services.GetRequiredService<IOptions<MapSettings>>().Value,
            Path.Combine(AppContext.BaseDirectory, "wwwroot"));
        return app;
    }
}
