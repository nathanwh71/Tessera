using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using CSharpFunctionalExtensions;

namespace Tessera.Etl;

/// <summary>
/// One kind of filter, as a pipeline file names it: its name, what it does, and how it builds a
/// <see cref="Filter"/> from the settings the file gives it. Each filter's own file declares one.
/// </summary>
public sealed record FilterKind(
    string Name,
    string Description,
    Func<JsonObject, Area?, Result<Filter, ImmutableArray<string>>> Build)
{
    /// <summary>
    /// A kind whose settings read into <typeparamref name="TSettings"/>, a record of the filter's
    /// own. A key the record lacks, or a setting it needs and the file leaves out, is refused, so a
    /// setting that belongs to another filter cannot pass unseen. <paramref name="check"/> names
    /// what else is wrong; <paramref name="create"/> makes the filter once nothing is.
    /// </summary>
    public static FilterKind Of<TSettings>(
        string name,
        string description,
        Func<TSettings, Area?, IEnumerable<string>> check,
        Func<TSettings, Area?, Filter> create) =>
        new(name, description, (settings, area) =>
        {
            var unknown = Unknown<TSettings>(settings).ToImmutableArray();
            if (!unknown.IsEmpty)
                return Result.Failure<Filter, ImmutableArray<string>>([.. unknown.Select(problem => $"{name}: {problem}")]);
            TSettings? read;
            try
            {
                read = settings.Deserialize<TSettings>(PipelineFile.Options);
            }
            catch (JsonException error)
            {
                return Result.Failure<Filter, ImmutableArray<string>>([$"{name}: {error.Message}"]);
            }
            if (read is null)
                return Result.Failure<Filter, ImmutableArray<string>>([$"{name}: its settings are missing."]);
            var problems = check(read, area).Select(problem => $"{name}: {problem}").ToImmutableArray();
            return problems.IsEmpty
                ? Result.Success<Filter, ImmutableArray<string>>(create(read, area))
                : Result.Failure<Filter, ImmutableArray<string>>(problems);
        });

    /// <summary>
    /// Names, in plain words, each key the settings record lacks and each setting it needs that the
    /// file leaves out. The record's constructor lists its settings: one with no default is needed.
    /// </summary>
    private static IEnumerable<string> Unknown<TSettings>(JsonObject settings)
    {
        var parameters = typeof(TSettings).GetConstructors().MaxBy(constructor => constructor.GetParameters().Length)?.GetParameters() ?? [];
        var known = parameters.Select(parameter => JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!)).ToList();
        foreach (var (key, _) in settings)
        {
            if (!known.Contains(key))
                yield return known.Count == 0
                    ? $"'{key}' is not a setting; this filter takes none."
                    : $"'{key}' is not a setting; its settings are {string.Join(", ", known)}.";
        }
        foreach (var parameter in parameters.Where(parameter => !parameter.HasDefaultValue))
        {
            var key = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!);
            if (!settings.ContainsKey(key)) yield return $"needs {key}.";
        }
    }

    /// <summary>For a filter that needs no checks beyond its settings' own types.</summary>
    public static IEnumerable<string> NothingToCheck<TSettings>(TSettings settings, Area? area) => [];

    /// <summary>For a filter that needs the area's box.</summary>
    public static IEnumerable<string> NeedsArea<TSettings>(TSettings settings, Area? area) =>
        area is null ? ["needs an area: give one in the pipeline file, or run beside tessera.json."] : [];
}
