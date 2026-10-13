using System.Collections.Immutable;
using Tessera.Kernel;
using Tessera.Settings;
using Tessera.Sql;

namespace Tessera.Import;

/// <summary>What the import loaded: the number of places in each layer.</summary>
public sealed record ImportSummary(ImmutableSortedDictionary<string, int> PlacesInLayer)
{
    public override string ToString() =>
        string.Join(Environment.NewLine, PlacesInLayer.Select(layer => $"{layer.Key}: {layer.Value} places"));
}

/// <summary>
/// Loads the checked places into nine tables, one for each layer, in one transaction (US-24).
/// </summary>
/// <remarks>
/// <see cref="Plan"/> is pure: it turns the places into the list of queries to run, so a test can
/// read the plan without a database. <see cref="Run"/> sends that list to the database. The import
/// signs in as tessera_publisher, which owns the tables; it never grants them. Publishing is the
/// data publisher's own act (US-24 c5).
/// </remarks>
public static class Importer
{
    /// <summary>The type of each column a table holds, by column name, for each layer's table id.</summary>
    public sealed record ExistingColumns(ImmutableDictionary<string, ImmutableDictionary<string, string>> OfTable)
    {
        public static readonly ExistingColumns None = new(ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty);

        public ImmutableDictionary<string, string> Of(string tableId) =>
            OfTable.GetValueOrDefault(tableId) ?? ImmutableDictionary<string, string>.Empty;
    }

    public static async Task<Result<ImportSummary, ImportError>> Run(Database database, string json, AreaBox box, CancellationToken cancel = default)
    {
        var places = Seed.Read(json, box);
        return await places.Match(
            async found => (await database.InTransaction(
                async session => await (await ReadColumns(session).ConfigureAwait(false))
                    .Bind(existing => Plan(found, existing))
                    .BindAsync(queries => RunAll(session, queries)).ConfigureAwait(false),
                Refused, cancel).ConfigureAwait(false))
                .Map(_ => Summary(found)),
            error => Task.FromResult(Result.Refuse<ImportSummary, ImportError>(error))).ConfigureAwait(false);
    }

    /// <summary>A column's type as information_schema names it: <c>text</c>, <c>boolean</c> or <c>double precision</c>.</summary>
    public sealed record ColumnType(string Name, string Type);

    // Reads the columns inside the transaction, so the plan matches the tables it will change.
    private static async Task<Result<ExistingColumns, ImportError>> ReadColumns(Database.Session session) =>
        (await ColumnTypeQueries()
            .Match(found => found, _ => [])
            .TraverseAsync(async target =>
                (await session.Rows<ColumnType>(target.Query).ConfigureAwait(false))
                    .MapError(Refused)
                    .Map(rows => (target.TableId, Types: rows.ToImmutableDictionary(row => row.Name, row => row.Type, StringComparer.Ordinal))))
            .ConfigureAwait(false))
        .Map(tables => new ExistingColumns(tables.ToImmutableDictionary(table => table.TableId, table => table.Types, StringComparer.Ordinal)));

    private static ImportError Refused(DatabaseError error) =>
        new($"The database refused the import ({error.SqlState}): {error.Message}");

    /// <summary>
    /// Every query the import runs, in order: schemas, tables, columns and indexes first, then the
    /// rows of all nine tables replaced, then fresh statistics. The queries themselves live in
    /// Sql's <see cref="ImportQueries"/>; this function decides which to run, purely.
    /// </summary>
    /// <param name="places">The checked places.</param>
    /// <param name="existing">
    /// The type of each column the database already holds, by table id and column name, so a
    /// property whose values changed kind since the last file gets its column changed, not refused.
    /// </param>
    public static Result<ImmutableArray<Query>, ImportError> Plan(ImmutableArray<Place> places, ExistingColumns existing) =>
        from schemas in Layers.Groups.Traverse(schema => Quote(schema).Map(ImportQueries.CreateSchema))
        from geometry in Quote(Seed.GeometryColumn)
        from id in Quote("id")
        from tables in Layers.GroupOfLayer.Traverse(layer =>
            from table in TableName.Of(layer.Value, layer.Key).MapError(problem => new ImportError(problem))
            from index in Quote($"{layer.Key}_{Seed.GeometryColumn}_index")
            from columns in Columns(places.Where(place => place.Layer == layer.Key))
            select (Layer: layer.Key, Table: new ImportTable(table, id, geometry, index, columns)))
        select ImmutableArray.CreateRange(
            schemas
                .Concat(tables.SelectMany(target => Shape(target.Table, existing.Of(target.Table.Table.Id))))
                .Concat(tables.Select(target => ImportQueries.Empty(target.Table)))
                .Concat(tables.SelectMany(target => places
                    .Where(place => place.Layer == target.Layer)
                    .Select(place => ImportQueries.Insert(target.Table, Row(target.Table, place)))))
                .Concat(tables.Select(target => ImportQueries.Analyse(target.Table))));

    /// <summary>The queries that read each table's column types, in the order of the nine layers.</summary>
    public static Result<ImmutableArray<(string TableId, Query Query)>, ImportError> ColumnTypeQueries() =>
        from geometry in Quote(Seed.GeometryColumn)
        from id in Quote("id")
        from tables in Layers.GroupOfLayer.Traverse(layer =>
            from table in TableName.Of(layer.Value, layer.Key).MapError(problem => new ImportError(problem))
            from index in Quote($"{layer.Key}_{Seed.GeometryColumn}_index")
            select (table.Id, ImportQueries.ColumnTypes(new ImportTable(table, id, geometry, index, []))))
        select tables;

    private static Result<ImmutableArray<ImportColumn>, ImportError> Columns(IEnumerable<Place> places) =>
        places
            .SelectMany(place => place.Properties)
            .GroupBy(property => property.Key, StringComparer.Ordinal)
            .OrderBy(property => property.Key, StringComparer.Ordinal)
            .Traverse(property => Quote(property.Key).Map(name => new ImportColumn(name, TypeOf(property.Select(value => value.Value)))));

    // A column's type follows its values: all true or false makes a boolean, all numbers a number,
    // anything else text. Missing values fit any type.
    private static SqlType TypeOf(IEnumerable<PropertyValue> values)
    {
        var present = values.Where(value => value is not PropertyValue.Missing).ToImmutableArray();
        return present.Length > 0 && present.All(value => value is PropertyValue.Flag) ? SqlType.Boolean
            : present.Length > 0 && present.All(value => value is PropertyValue.Number) ? SqlType.Number
            : SqlType.Text;
    }

    // A column the database holds with another type is changed, so a property that was text in one
    // file and a number in the next loads rather than fails the whole import.
    private static IEnumerable<Query> Shape(ImportTable table, ImmutableDictionary<string, string> existing) =>
        new[] { ImportQueries.CreateTable(table) }
            .Concat(table.Columns.Select(column =>
                existing.TryGetValue(column.Name.Raw, out var type) && type != column.Type.Keyword
                    ? ImportQueries.ChangeColumnType(table, column)
                    : ImportQueries.AddColumn(table, column)))
            .Append(ImportQueries.CreateIndex(table));

    private static ImportRow Row(ImportTable table, Place place) =>
        new(place.Id, place.Longitude, place.Latitude,
            [.. table.Columns.Select(column => Parameter(column, place.Properties.GetValueOrDefault(column.Name.Raw)))]);

    private static object Parameter(ImportColumn column, PropertyValue? value) => value switch
    {
        PropertyValue.Text(var text) when column.Type == SqlType.Text => text,
        PropertyValue.Number(var number) when column.Type == SqlType.Number => number,
        PropertyValue.Flag(var flag) when column.Type == SqlType.Boolean => flag,
        PropertyValue.Number(var number) => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
        PropertyValue.Flag(var flag) => flag ? "true" : "false",
        PropertyValue.Text(var text) => text,
        _ => SqlNull.Value,
    };

    private static ImportSummary Summary(ImmutableArray<Place> places) =>
        new(places.GroupBy(place => place.Layer).ToImmutableSortedDictionary(layer => layer.Key, layer => layer.Count(), StringComparer.Ordinal));

    private static async Task<Result<int, ImportError>> RunAll(Database.Session session, ImmutableArray<Query> queries) =>
        (await queries.TraverseAsync(async query => (await session.Execute(query).ConfigureAwait(false)).MapError(Refused)).ConfigureAwait(false))
            .Map(counts => counts.Length);

    private static Result<QuotedName, ImportError> Quote(string name) =>
        QuotedName.Quote(name).MapError(problem => new ImportError(problem));
}
