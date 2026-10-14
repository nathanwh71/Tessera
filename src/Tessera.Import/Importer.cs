using System.Collections.Immutable;
using Tessera.Kernel;
using Tessera.Settings;
using Tessera.Sql;

namespace Tessera.Import;

/// <summary>
/// What the import loaded: the number of places in each layer, which the program prints when it ends.
/// </summary>
public sealed record ImportSummary(ImmutableSortedDictionary<string, int> PlacesInLayer)
{
    public override string ToString() =>
        string.Join(Environment.NewLine, PlacesInLayer.Select(layer => $"{layer.Key}: {layer.Value} places"));
}

/// <summary>
/// Loads the checked places into nine tables, one for each layer, in one transaction, a group of
/// changes the database makes all together or not at all (US-24). It belongs to the Import module:
/// Program.cs calls <see cref="Run"/> for the command <c>Tessera.Service import &lt;file&gt;</c>, and
/// <see cref="Seed"/> reads and checks the file first.
/// </summary>
/// <remarks>
/// <see cref="Plan"/> is pure, a function that only computes its answer and touches nothing outside:
/// it turns the places into the list of queries to run, so a test can read the plan without a
/// database. <see cref="Run"/> sends that list to the database.
/// <para>
/// Publishing is the data publisher's own act (US-24 c5), so the import signs in as
/// tessera_publisher, the role (database user) that owns the tables, and never grants them, that is,
/// never gives the service permission to read them.
/// </para>
/// </remarks>
public static class Importer
{
    /// <summary>The type of each column a table holds, by column name, for each layer's table id.</summary>
    public sealed record ExistingColumns(ImmutableDictionary<string, ImmutableDictionary<string, string>> OfTable)
    {
        /// <summary>No tables at all, as in a database the import has never filled.</summary>
        public static readonly ExistingColumns None = new(ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty);

        /// <summary>The columns of one table; none for a table the database lacks.</summary>
        public ImmutableDictionary<string, string> Of(string tableId) =>
            OfTable.GetValueOrDefault(tableId) ?? ImmutableDictionary<string, string>.Empty;
    }

    /// <summary>
    /// Reads and checks the GeoJSON text, then replaces every layer's rows in one transaction. Returns
    /// the number of places in each layer, or the reason the file or the database refused; on a
    /// refusal the database keeps its old rows (US-24 c6). A place outside <paramref name="box"/>, the
    /// area's bounds in degrees, is refused (US-24 c4).
    /// </summary>
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

    /// <summary>
    /// A column's type as information_schema names it: <c>text</c>, <c>boolean</c> or
    /// <c>double precision</c>.
    /// </summary>
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

    // SQLSTATE is the database's five-character code for the kind of failure; it goes in the message
    // so the maintainer can look it up.
    private static ImportError Refused(DatabaseError error) =>
        new($"The database refused the import ({error.SqlState}): {error.Message}");

    /// <summary>
    /// Every query the import runs, in order: schemas and tables; then the old rows of all nine
    /// tables emptied; then each table's columns made to match the file, its index made; then the
    /// new rows; then fresh statistics. The queries themselves live in
    /// Sql's <see cref="ImportQueries"/>; this function decides which to run, and touches no database.
    /// </summary>
    /// <remarks>
    /// The <c>from ... select</c> form runs each step in turn and stops at the first refusal, like a
    /// chain of checks; here a refusal means a name that will not quote.
    /// <para>
    /// The rows go before the columns change, so a change of type meets an empty table: no old
    /// value has to turn into the new type, which a word such as 'closed' cannot do into a number.
    /// A column the file no longer carries goes, so no tile carries a column of nothing but NULLs.
    /// All of it runs in one transaction, so a reader sees the old table or the new, never between.
    /// </para>
    /// </remarks>
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
            let target = new ImportTable(table, id, geometry, index, columns)
            from dead in Dead(target, existing.Of(table.Id))
            select (Layer: layer.Key, Table: target, Dead: dead))
        select ImmutableArray.CreateRange(
            schemas
                .Concat(tables.Select(target => ImportQueries.CreateTable(target.Table)))
                .Concat(tables.Select(target => ImportQueries.Empty(target.Table)))
                .Concat(tables.SelectMany(target => target.Dead.Select(column => ImportQueries.DropColumn(target.Table, column))))
                .Concat(tables.SelectMany(target => Shape(target.Table, existing.Of(target.Table.Table.Id))))
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

    // One column for each property name any place in the layer carries, sorted by name.
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

    // The columns a table holds that the file no longer carries, beside the id and the geometry,
    // which every table keeps. Each name came from the database itself, and is quoted all the same.
    private static Result<ImmutableArray<QuotedName>, ImportError> Dead(ImportTable table, ImmutableDictionary<string, string> existing) =>
        existing.Keys
            .Where(name => name != table.Id.Raw && name != table.Geometry.Raw && !table.Columns.Any(column => column.Name.Raw == name))
            .Order(StringComparer.Ordinal)
            .Traverse(Quote);

    // A property that was text in one file and a number in the next should load rather than fail the
    // whole import, so a column the database holds with another type is changed. The table is empty
    // by now, so the change meets no old value.
    private static IEnumerable<Query> Shape(ImportTable table, ImmutableDictionary<string, string> existing) =>
        table.Columns.Select(column =>
                existing.TryGetValue(column.Name.Raw, out var type) && type != column.Type.Keyword
                    ? ImportQueries.ChangeColumnType(table, column)
                    : ImportQueries.AddColumn(table, column))
            .Append(ImportQueries.CreateIndex(table));

    private static ImportRow Row(ImportTable table, Place place) =>
        new(place.Id, place.Longitude, place.Latitude,
            [.. table.Columns.Select(column => Parameter(column, place.Properties.GetValueOrDefault(column.Name.Raw)))]);

    // A value whose kind matches its column goes as it is. In a text column, which holds mixed
    // kinds, a number or a flag goes as its text. A place that lacks the property, or gives null,
    // gets the database's NULL, its mark for an empty cell.
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

    // Runs the queries one after another and stops at the first that fails.
    private static async Task<Result<int, ImportError>> RunAll(Database.Session session, ImmutableArray<Query> queries) =>
        (await queries.TraverseAsync(async query => (await session.Execute(query).ConfigureAwait(false)).MapError(Refused)).ConfigureAwait(false))
            .Map(counts => counts.Length);

    private static Result<QuotedName, ImportError> Quote(string name) =>
        QuotedName.Quote(name).MapError(problem => new ImportError(problem));
}
