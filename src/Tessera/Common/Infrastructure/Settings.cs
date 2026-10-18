using System.Reflection;
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
    /// Binds one section to its record, with the record's own limits, and checks every key as the
    /// program starts, so one failed start names every problem (US-00 c4, N-21 c4). A key the
    /// record lacks, most often a misspelling, and a value that is not of the key's kind, such as
    /// a word where a number belongs, are problems in the same list as a value out of its limits.
    /// A misspelt section name passes unseen: its keys bind to nothing, so the section keeps its
    /// defaults.
    /// </summary>
    /// <remarks>
    /// .NET's binder can refuse an unknown key itself, but it then throws at once, so the first
    /// slip would hide every other. Here the binder sees only the keys that read cleanly, and a
    /// check of its own, <see cref="KeyCheck{T}"/>, names the rest.
    /// </remarks>
    public static void Section<T>(IServiceCollection services, string name) where T : class
    {
        services.AddOptions<T>()
            .Configure<IConfiguration>((options, configuration) => Bind(options, configuration.GetSection(name)))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<T>>(provider =>
            new KeyCheck<T>(provider.GetRequiredService<IConfiguration>().GetSection(name)));
    }

    /// <summary>Checks every section at once; throws with every problem found.</summary>
    public static void Check(IServiceProvider services) => services.GetRequiredService<IStartupValidator>().Validate();

    /// <summary>
    /// Each problem a failed check names: a key or a value from <see cref="KeyCheck{T}"/>, or a
    /// value out of its limits, from the record's own checks. Several sections that fail come as
    /// one AggregateException.
    /// </summary>
    public static IEnumerable<string> Problems(Exception error) => error switch
    {
        OptionsValidationException failed => failed.Failures,
        AggregateException many => many.InnerExceptions.SelectMany(Problems),
        _ => [error.Message],
    };

    /// <summary>
    /// The problem with one key of a section, or null for a key that reads cleanly: a key the
    /// record lacks, or a value that is not of the key's kind. A key matches its property whatever
    /// its case, as .NET's binder matches it.
    /// </summary>
    private static string? Problem(Type record, IConfigurationSection key)
    {
        var section = key.Path[..key.Path.LastIndexOf(':')];
        var property = record.GetProperty(
            key.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null)
        {
            var known = string.Join(", ", record.GetProperties().Select(each => each.Name));
            return $"{section}: '{key.Key}' is not a setting; {section} holds {known}.";
        }

        try
        {
            key.Get(property.PropertyType);
            return null;
        }
        // The binder's own refusal of a value it cannot convert, such as 'soon' for a number.
        catch (InvalidOperationException)
        {
            return $"{section}: {property.Name} must be {Kind(property.PropertyType)}, not '{key.Value}'.";
        }
    }

    // The kind of value each type of setting takes, in words a person editing the file knows.
    private static string Kind(Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.Int32 => "a whole number",
        TypeCode.Double => "a number",
        TypeCode.Boolean => "true or false",
        _ => $"a {type.Name}",
    };

    // Binds the keys that read cleanly and leaves the rest at their defaults, for KeyCheck to
    // name. The binder works on a copy that holds only those keys, so it never throws.
    private static void Bind<T>(T options, IConfigurationSection section) where T : class =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(section.GetChildren()
                .Where(key => Problem(typeof(T), key) is null)
                .Select(key => KeyValuePair.Create(key.Key, key.Value)))
            .Build()
            .Bind(options);

    /// <summary>
    /// Names each key of one section that the binder left out (<see cref="Problem"/>). It runs
    /// with the record's own checks, so its problems join theirs in one list.
    /// </summary>
    private sealed class KeyCheck<T>(IConfigurationSection section) : IValidateOptions<T> where T : class
    {
        public ValidateOptionsResult Validate(string? name, T options) =>
            section.GetChildren().Select(key => Problem(typeof(T), key)).OfType<string>().ToList() is
                { Count: > 0 } problems
                ? ValidateOptionsResult.Fail(problems)
                : ValidateOptionsResult.Success;
    }
}
