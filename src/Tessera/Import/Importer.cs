using System.Collections.Immutable;
using System.Globalization;
using CSharpFunctionalExtensions;
using Tessera.Settings;
using Tessera.Sql;

namespace Tessera.Import;

/// <summary>What the import loaded: the number of places in each layer, which the program prints.</summary>
public sealed record ImportSummary(ImmutableSortedDictionary<string, int> PlacesInLayer)
{
    public override string ToString() =>
        string.Join(Environment.NewLine, PlacesInLayer.Select(layer => $"{layer.Key}: {layer.Value} places"));
}

/// <summary>
/// Loads the checked places into nine tables, one for each layer, in one transaction: all of it takes
/// effect, or none does (US-24). The command <c>Tessera import &lt;file&gt;</c> runs it.
/// </summary>
/// <remarks>
/// <see cref="Plan"/> is pure: it turns the places into the list of queries to run, so a test can read
/// the plan without a database. Publishing is the publisher's own act (US-24 c5), so the import signs
/// in as tessera_publisher and never grants the tables to the service.
/// </remarks>
public static class Importer
{
    /// <summary>The type of each column a table holds, by column name, for each table id such as <c>food.cafes</c>.</summary>
    public sealed record ExistingColumns(ImmutableDictionary<string, ImmutableDictionary<string, string>> OfTable)
    {
        /// <summary>No tables at all, as in a database the import has never filled.</summary>
        public static readonly ExistingColumns None = new(ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty);

        public ImmutableDictionary<string, string> Of(string tableId) =>
            OfTable.GetValueOrDefault(tableId) ?? ImmutableDictionary<string, string>.Empty;
    }

    /// <summary>A column's type as information_schema names it: text, boolean or double precision.</summary>
    public sealed record ColumnType(string Name, string Type);

    // One layer's table: the layer, the table's id, its quoted name, and each column's quoted name,
    // stored name and type.
    private sealed record Target(string Layer, string Id, string Table, ImmutableArray<(string Quoted, string Name, string Type)> Columns);

    /// <summary>
    /// Reads and checks the GeoJSON text, then replaces every layer's rows in one transaction. On a
    /// refusal the database keeps its old rows (US-24 c6).
    /// </summary>
    public static async Task<Result<ImportSummary, ImportError>> Run(Database database, string json, AreaSettings box, CancellationToken cancel = default)
    {
        var places = Seed.Read(json, box);
        if (places.IsFailure)
            return places.Error;

        return await database.InTransaction(
            async session =>
            {
                // The columns are read inside the transaction, so the plan matches the tables it changes.
                foreach (var query in Plan(places.Value, await ReadColumns(session).ConfigureAwait(false)))
                    await session.Execute(query).ConfigureAwait(false);
                return Result.Success<ImportSummary, ImportError>(new ImportSummary(places.Value
                    .GroupBy(place => place.Layer)
                    .ToImmutableSortedDictionary(layer => layer.Key, layer => layer.Count(), StringComparer.Ordinal)));
            },
            // SQLSTATE goes in the message so the maintainer can look it up.
            error => new ImportError($"The database refused the import ({error.SqlState}): {error.Message}"),
            cancel).ConfigureAwait(false);
    }

    /// <summary>
    /// Every query the import runs, in order: schemas and tables; every table emptied; columns the
    /// file no longer carries dropped; each column made to match the file, and the index made; the
    /// new rows; fresh statistics.
    /// </summary>
    /// <remarks>
    /// The rows go before any column changes, so a change of type meets an empty table: no old value
    /// has to turn into the new type, which a word such as 'closed' cannot do into a number.
    /// </remarks>
    /// <param name="places">The checked places.</param>
    /// <param name="existing">The columns the database already holds, so a changed type is changed, not refused.</param>
    public static ImmutableArray<Query> Plan(ImmutableArray<Place> places, ExistingColumns existing)
    {
        var targets = Layers.GroupOfLayer.Select(layer => TargetOf(layer.Key, layer.Value, places)).ToImmutableArray();
        return
        [
            .. Layers.Groups.Select(group => Queries.CreateSchema(Queries.Quote(group))),
            .. targets.Select(target => Queries.CreateTable(target.Table)),
            .. targets.Select(target => Queries.Empty(target.Table)),
            .. targets.SelectMany(target => Dead(target, existing.Of(target.Id)).Select(column => Queries.DropColumn(target.Table, Queries.Quote(column)))),
            .. targets.SelectMany(target => Shape(target, existing.Of(target.Id))),
            .. targets.SelectMany(target => places.Where(place => place.Layer == target.Layer).Select(place => Insert(target, place))),
            .. targets.Select(target => Queries.Analyse(target.Table)),
        ];
    }

    // One column for each property name any place in the layer carries, sorted by name.
    private static Target TargetOf(string layer, string group, ImmutableArray<Place> places) => new(
        layer,
        $"{group}.{layer}",
        $"{Queries.Quote(group)}.{Queries.Quote(layer)}",
        [.. places.Where(place => place.Layer == layer)
            .SelectMany(place => place.Properties)
            .GroupBy(property => property.Key, StringComparer.Ordinal)
            .OrderBy(property => property.Key, StringComparer.Ordinal)
            .Select(property => (Queries.Quote(property.Key), property.Key, TypeOf(property.Select(value => value.Value))))]);

    // All true or false makes a boolean, all numbers a number, anything else text; a missing value fits any type.
    private static string TypeOf(IEnumerable<PropertyValue> values)
    {
        var present = values.Where(value => value is not PropertyValue.Missing).ToImmutableArray();
        return present.Length > 0 && present.All(value => value is PropertyValue.Flag) ? "boolean"
            : present.Length > 0 && present.All(value => value is PropertyValue.Number) ? "double precision"
            : "text";
    }

    // The columns a table holds that the file no longer carries, beside the id and the geometry.
    private static IEnumerable<string> Dead(Target target, ImmutableDictionary<string, string> existing) =>
        existing.Keys
            .Where(name => name != "id" && name != Seed.GeometryColumn && !target.Columns.Any(column => column.Name == name))
            .Order(StringComparer.Ordinal);

    // A column the database holds with another type is changed; a new one is added.
    private static IEnumerable<Query> Shape(Target target, ImmutableDictionary<string, string> existing) =>
        target.Columns.Select(column =>
                existing.TryGetValue(column.Name, out var type) && type != column.Type
                    ? Queries.ChangeColumnType(target.Table, column.Quoted, column.Type)
                    : Queries.AddColumn(target.Table, column.Quoted, column.Type))
            .Append(Queries.CreateIndex(target.Table, Queries.Quote($"{target.Layer}_{Seed.GeometryColumn}_index")));

    private static Query Insert(Target target, Place place) =>
        Queries.Insert(target.Table, [.. target.Columns.Select(column => column.Quoted)], place.Id, place.Longitude, place.Latitude,
            [.. target.Columns.Select(column => Parameter(column.Type, place.Properties.GetValueOrDefault(column.Name)))]);

    // A value whose kind matches its column goes as it is. In a text column, which holds mixed kinds, a
    // number or a flag goes as its text. A missing value becomes the database's NULL.
    private static object? Parameter(string type, PropertyValue? value) => value switch
    {
        PropertyValue.Number(var number) when type == "double precision" => number,
        PropertyValue.Flag(var flag) when type == "boolean" => flag,
        PropertyValue.Number(var number) => number.ToString(CultureInfo.InvariantCulture),
        PropertyValue.Flag(var flag) => flag ? "true" : "false",
        PropertyValue.Text(var text) => text,
        _ => null,
    };

    private static async Task<ExistingColumns> ReadColumns(Database.Session session)
    {
        var tables = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var (layer, group) in Layers.GroupOfLayer)
            tables[$"{group}.{layer}"] = (await session.Rows<ColumnType>(Queries.ColumnTypes(group, layer)).ConfigureAwait(false))
                .ToImmutableDictionary(column => column.Name, column => column.Type, StringComparer.Ordinal);
        return new ExistingColumns(tables.ToImmutable());
    }
}
