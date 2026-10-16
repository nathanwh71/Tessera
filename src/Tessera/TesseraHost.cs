using Common.Api;
using Common.Infrastructure;
using Modules.Tessera;

/// <summary>
/// The template's Host: it builds one web service from <c>Common</c> and each module, and runs the
/// steps every request passes through, whatever module answers it. Program.cs builds it once, then
/// runs it.
/// </summary>
public static class TesseraHost
{
    /// <summary>
    /// Builds the service on ASP.NET Core's standard start-up, which reads tessera.json, the
    /// <b><i>settings file</i></b>, its <c>Urls</c> and <c>Logging</c> sections included, and the
    /// environment (ADR 0003). Every setting is checked here, before anything runs, and a failed
    /// check throws with every problem, so one failed start names all there is to fix (N-21 c4).
    /// </summary>
    /// <param name="overrides">Settings a test lays over the file and the environment.</param>
    public static WebApplication Build(IDictionary<string, string?>? overrides = null)
    {
        var builder = WebApplication.CreateBuilder();
        Settings.AddFile(builder.Configuration);
        builder.Configuration.AddInMemoryCollection(overrides ?? new Dictionary<string, string?>());
        // Every error answers as a problem document of one shape, so no route writes its own (US-07 c5).
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = Problems.Shape);
        builder.AddTessera();

        var app = builder.Build();
        Settings.Check(app.Services);
        // The first catches a crash and the second an empty error, such as a 404 for an address no
        // route knows; both answer with a problem document.
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        // Routing picks each request's route here, so the steps above run for every request first.
        app.UseRouting();
        app.MapTessera();
        return app;
    }
}
