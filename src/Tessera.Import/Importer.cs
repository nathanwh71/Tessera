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
    public static async Task<Result<ImportSummary, ImportError>> Run(Database database, string json, AreaBox box, CancellationToken cancel = default)
    {
        var places = Seed.Read(json, box);
        return await places.Bind(Plan).Match(
            async queries => (await database.InTransaction(session => RunAll(session, queries), cancel).ConfigureAwait(false))
                .MapError(error => new ImportError($"The database refused the import ({error.SqlState}): {error.Message}"))
                .Bind(_ => places.Map(Summary)),
            error => Task.FromResult(Result.Refuse<ImportSummary, ImportError>(error))).ConfigureAwait(false);
    }

    /// <summary>
    /// Every query the import runs, in order: schemas, tables, columns and indexes first, then the
    /// rows of all nine tables replaced, then fresh statistics.
    /// </summary>
    /// <remarks>
    /// "IF NOT EXISTS" lets the import run again on a database it has filled before. DELETE empties
    /// each table inside the one transaction, and readers keep seeing the old rows until the new
    /// ones are all in, so no tile ever carries half a file (US-24 c6). TRUNCATE would be faster, but
    /// it locks each table until the import ends, so every tile request would wait. A spatial index (GIST) sorts shapes by
    /// where they lie, so a tile finds its few places without reading the whole table; ANALYZE then
    /// gathers the statistics the database uses to choose that index.
    /// </remarks>
    public static Result<ImmutableArray<Query>, ImportError> Plan(ImmutableArray<Place> places) =>
        from schemas in Layers.Groups.Traverse(schema => Quote(schema).Map(name => Query.Of($"CREATE SCHEMA IF NOT EXISTS {name}")))
        from geometry in Quote(Seed.GeometryColumn)
        from id in Quote("id")
        from tables in Layers.GroupOfLayer.Traverse(layer =>
            from table in TableName.Of(layer.Value, layer.Key).MapError(problem => new ImportError(problem))
            from index in Quote($"{layer.Key}_{Seed.GeometryColumn}_index")
            from columns in Columns(places.Where(place => place.Layer == layer.Key))
            select new Target(layer.Key, table, index, columns))
        select ImmutableArray.CreateRange(
            schemas
                .Concat(tables.SelectMany(table => Shape(table, id, geometry)))
                .Concat(tables.Select(table => Query.Of($"DELETE FROM {table.Table}")))
                .Concat(tables.SelectMany(table => places
                    .Where(place => place.Layer == table.Layer)
                    .Select(place => Insert(table, id, geometry, place))))
                .Concat(tables.Select(table => Query.Of($"ANALYZE {table.Table}"))));

    /// <summary>One layer's table, the name of its spatial index, and its property columns.</summary>
    private sealed record Target(string Layer, TableName Table, QuotedName Index, ImmutableArray<Column> Columns);

    /// <summary>A column for each property any place of the layer carries, with the type its values share.</summary>
    public sealed record Column(QuotedName Name, SqlType Type);

    private static Result<ImmutableArray<Column>, ImportError> Columns(IEnumerable<Place> places) =>
        places
            .SelectMany(place => place.Properties)
            .GroupBy(property => property.Key, StringComparer.Ordinal)
            .OrderBy(property => property.Key, StringComparer.Ordinal)
            .Traverse(property => Quote(property.Key).Map(name => new Column(name, TypeOf(property.Select(value => value.Value)))));

    // A column's type follows its values: all true or false makes a boolean, all numbers a number,
    // anything else text. Missing values fit any type.
    private static SqlType TypeOf(IEnumerable<PropertyValue> values)
    {
        var present = values.Where(value => value is not PropertyValue.Missing).ToImmutableArray();
        return present.Length > 0 && present.All(value => value is PropertyValue.Flag) ? SqlType.Boolean
            : present.Length > 0 && present.All(value => value is PropertyValue.Number) ? SqlType.Number
            : SqlType.Text;
    }

    private static IEnumerable<Query> Shape(Target target, QuotedName id, QuotedName geometry) =>
        new[]
            {
                Query.Of($"CREATE TABLE IF NOT EXISTS {target.Table} ({id} {SqlType.Text} PRIMARY KEY, {geometry} {SqlType.Point4326} NOT NULL)"),
            }
            .Concat(target.Columns.Select(column => Query.Of($"ALTER TABLE {target.Table} ADD COLUMN IF NOT EXISTS {column.Name} {column.Type}")))
            .Append(Query.Of($"CREATE INDEX IF NOT EXISTS {target.Index} ON {target.Table} USING GIST ({geometry})"));

    private static Query Insert(Target target, QuotedName id, QuotedName geometry, Place place)
    {
        // The values travel as bound parameters, in the column list's order.
        var names = new NameList([.. target.Columns.Select(column => column.Name)], LeadingComma: true);
        var values = new ValueList([.. target.Columns.Select(column => Parameter(column, place.Properties.GetValueOrDefault(column.Name.Raw)))]);
        return Query.Of($"INSERT INTO {target.Table} ({id}, {geometry}{names}) VALUES ({place.Id}, ST_SetSRID(ST_MakePoint({place.Longitude}, {place.Latitude}), 4326){values})");
    }

    private static object Parameter(Column column, PropertyValue? value) => value switch
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

    private static async Task<Result<int, DatabaseError>> RunAll(Database.Session session, ImmutableArray<Query> queries)
    {
        var count = 0;
        foreach (var query in queries)
        {
            switch (await session.Execute(query).ConfigureAwait(false))
            {
                case Result<int, DatabaseError>.Refused(var error):
                    return Result.Refuse<int, DatabaseError>(error);
                default:
                    count++;
                    break;
            }
        }

        return Result.Ok<int, DatabaseError>(count);
    }

    private static Result<QuotedName, ImportError> Quote(string name) =>
        QuotedName.Quote(name).MapError(problem => new ImportError(problem));
}
