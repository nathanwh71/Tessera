using Microsoft.Extensions.Options;

// How every module reads its settings (US-00, ADR 0003). ASP.NET Core reads tessera.json and the
// environment; each module binds its own sections to records, with their defaults and limits, and the
// program checks them all when it starts. Each record lives beside the code that reads it.

namespace Common.Infrastructure;

/// <summary>
/// Reads tessera.json, the <b><i>settings file</i></b>, and each <b><i>environment variable</i></b>
/// through ASP.NET Core's own configuration, binds a section to its record, and checks every value
/// when the program starts (US-00 c4).
/// </summary>
public static class Settings
{
    /// <summary>
    /// Adds tessera.json, beside the program, then the environment again, so a variable such as
    /// <c>Tiles__CacheSeconds=0</c> wins over the file; each '__' steps down a level.
    /// </summary>
    public static void AddFile(IConfigurationBuilder configuration) =>
        configuration
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "tessera.json"), optional: true)
            .AddEnvironmentVariables();

    /// <summary>
    /// Binds one section to its record, with the record's own limits. A key the service does not read
    /// is refused, since it is most often a misspelling. A misspelt section name passes unseen: its
    /// keys bind to nothing, so the section keeps its defaults. Every limit is checked as the program
    /// starts, so one failed start names every problem (N-21 c4).
    /// </summary>
    public static void Section<T>(IServiceCollection services, string name) where T : class =>
        services.AddOptions<T>()
            .BindConfiguration(name, binder => binder.ErrorOnUnknownConfiguration = true)
            .ValidateDataAnnotations()
            .ValidateOnStart();

    /// <summary>Checks every section at once; throws with every problem found.</summary>
    public static void Check(IServiceProvider services) => services.GetRequiredService<IStartupValidator>().Validate();

    /// <summary>
    /// Each problem a failed check names: a value out of its limits, from the record's checks, or a
    /// key that will not bind, from the binder.
    /// </summary>
    public static IEnumerable<string> Problems(Exception error) => error switch
    {
        OptionsValidationException failed => failed.Failures,
        AggregateException many => many.InnerExceptions.SelectMany(Problems),
        _ => [error.Message],
    };
}
