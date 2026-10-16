using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Tessera.Etl;

/// <summary>
/// Writes the SQL that makes and fills one table from its cleaned rows, in the form GDAL's PGDump
/// driver wrote before (ADR 0009): drop the table, make it with a <c>fid</c> key and a
/// <c>geom</c> column in SRID 4326, one INSERT for each row in order, a spatial index, and fresh
/// statistics. The program used to hand GDAL a cleaned GeoJSON file for this; writing the SQL here
/// saves that pass, a GDAL run for each table, and the checks on GDAL's text.
/// </summary>
/// <remarks>
/// It keeps GDAL's choices where the service depends on them, its typing of dates, times and
/// date-times included, and departs from them in two places on purpose. A number with a fraction keeps all its digits, where GDAL kept fifteen. A column
/// that mixes text with numbers or true and false becomes text, where GDAL made it JSON, which the
/// tile query leaves out of every tile.
/// </remarks>
public static class TableSql
{
    // PostgreSQL keeps a name to 63 bytes, and cuts a longer one; GDAL cuts it first, and so does this.
    private const int NameBytes = 63;

    // The shape's bytes as PostGIS reads them: least significant byte first, with the SRID inside,
    // and x and y alone, since the extract dropped heights (-dim XY).
    private static readonly WKBWriter Shapes = new(ByteOrder.LittleEndian, handleSRID: true, emitZ: false, emitM: false);

    /// <summary>
    /// The table's statements, one after another, ready for the script; or why there are none: no
    /// rows, or two columns whose names become one.
    /// </summary>
    /// <param name="table">The table's plan: its name, and whether its shapes are lines or areas.</param>
    /// <param name="rows">The rows the filters kept, in the source's order.</param>
    /// <param name="sourceColumns">The source's columns, in its order (GeoJson.Read).</param>
    public static Result<ImmutableArray<string>, string> Statements(TablePlan table, ImmutableArray<Row> rows, ImmutableArray<string> sourceColumns)
    {
        // A table with no rows is refused (US-24, criterion 3).
        if (rows.IsEmpty)
            return Result.Failure<ImmutableArray<string>, string>($"{table.Table}: no rows to write.");

        var (schema, name) = SeedScript.SchemaAndName(table);
        var columns = GeoJson.Columns(rows, sourceColumns);
        var names = columns.Select(Launder).ToImmutableArray();
        // Two columns that become one name, such as 'Name' and 'name', or a column named as the key or
        // the shape, would make a script the database refuses; the run stops here instead.
        if (names.Concat(["fid", "geom"]).GroupBy(column => column).FirstOrDefault(group => group.Count() > 1) is { } clash)
            return Result.Failure<ImmutableArray<string>, string>($"{table.Table}: two columns become \"{clash.Key}\"; rename one in the source.");

        var types = columns.Select(column => TypeOf(rows.Select(row => row.Value(column)))).ToImmutableArray();
        var shapes = rows.Select(row => row.Shape.Where(shape => !shape.IsEmpty).Map(shape => table.Lines ? Multi(shape) : shape)).ToImmutableArray();
        var kinds = shapes.Where(shape => shape.HasValue).Select(shape => shape.Value.GeometryType.ToUpperInvariant()).Distinct().ToList();
        var quoted = $"{Quote(schema)}.{Quote(name)}";

        var statements = ImmutableArray.CreateBuilder<string>();
        statements.Add("SET standard_conforming_strings = ON;");
        statements.Add($"DROP TABLE IF EXISTS {quoted} CASCADE;");
        statements.Add($"CREATE TABLE {quoted}();");
        // The key numbers the rows 1, 2, 3 in the order they are inserted, which is the source's, and
        // fixes the order of features in a tile (US-24 test 2).
        statements.Add($"ALTER TABLE {quoted} ADD COLUMN \"fid\" SERIAL CONSTRAINT {Quote($"{name}_pk")} PRIMARY KEY;");
        // One kind of shape gives a column of that kind; a mix, or none, gives GEOMETRY, which takes any.
        statements.Add($"SELECT AddGeometryColumn({Text(schema)},{Text(name)},'geom',4326,'{(kinds.Count == 1 ? kinds[0] : "GEOMETRY")}',2);");
        for (var i = 0; i < columns.Length; i++)
            statements.Add($"ALTER TABLE {quoted} ADD COLUMN {Quote(names[i])} {types[i]};");

        var list = string.Join(", ", names.Select(Quote));
        for (var r = 0; r < rows.Length; r++)
        {
            var values = string.Join(", ", columns.Select((column, i) => Literal(rows[r].Value(column), types[i])));
            // A row with no shape leaves the column out, so it holds NULL, as GDAL wrote it.
            statements.Add(shapes[r].TryGetValue(out var shape)
                ? $"INSERT INTO {quoted} (\"geom\", {list}) VALUES ('{Ewkb(shape)}', {values});"
                : $"INSERT INTO {quoted} ({list}) VALUES ({values});");
        }

        // The spatial index lets the tile query read only the rows inside one tile (US-07).
        statements.Add($"CREATE INDEX {Quote($"{name}_geom_geom_idx")} ON {quoted} USING GIST (\"geom\");");
        // Fresh statistics let PostgreSQL plan the tile query well from the first request.
        statements.Add($"ANALYZE {quoted};");
        return Result.Success<ImmutableArray<string>, string>(statements.ToImmutable());
    }

    /// <summary>
    /// A column's name as GDAL wrote it, which the service and its tests already know: letters A to Z
    /// made lower case, and each ' - # made _, then cut to 63 bytes. Other characters, such as 'é',
    /// stay as they are.
    /// </summary>
    public static string Launder(string column)
    {
        var name = new StringBuilder(column.Length);
        foreach (var letter in column)
            name.Append(letter is '\'' or '-' or '#' ? '_' : letter <= 127 ? char.ToLowerInvariant(letter) : letter);
        var laundered = name.ToString();
        // Cut at a whole character, so no letter is split in two.
        while (Encoding.UTF8.GetByteCount(laundered) > NameBytes)
            laundered = laundered[..^1];
        return laundered;
    }

    /// <summary>
    /// The column's type, from the values its rows hold, as GDAL chose it: whole numbers become
    /// INTEGER, or INT8 past 32 bits; any fraction makes FLOAT8; true and false become BOOLEAN, or
    /// a number beside other numbers; text that is all dates, times or date-times takes that type
    /// (<see cref="TimeTypeOf"/>); anything mixed with text, and a column of nulls, becomes text.
    /// </summary>
    public static string TypeOf(IEnumerable<Maybe<object>> values)
    {
        var held = values.Where(value => value.HasValue).Select(value => value.Value).ToList();
        if (held.Count > 0 && held.All(value => value is string) && TimeTypeOf(held.Cast<string>()).TryGetValue(out var time))
            return time;
        var text = held.Any(value => value is not (long or double or bool));
        var fraction = held.Any(value => value is double);
        var whole = held.Any(value => value is long);
        var flag = held.Any(value => value is bool);
        return text || held.Count == 0 ? "VARCHAR"
            : fraction ? "FLOAT8"
            : whole ? held.OfType<long>().All(number => number is >= int.MinValue and <= int.MaxValue) ? "INTEGER" : "INT8"
            : flag ? "BOOLEAN"
            : "VARCHAR";
    }

    // A date as GDAL reads one: four digits, two and two, joined by - or /, such as 2024-05-01.
    private const string Day = @"(?<year>\d{4})[-/](?<month>\d{2})[-/](?<day>\d{2})";

    // A time of day: hours, minutes and seconds, with a fraction of a second if given.
    private const string Clock = @"(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})(\.\d+)?";

    private static readonly Regex DateOnly = new($"^{Day}$");
    private static readonly Regex TimeOnly = new($"^{Clock}$");

    // A date and a time joined by T or a space, with an optional zone: Z, or an offset such as +08:00.
    private static readonly Regex DateAndTime = new($"^{Day}[T ]{Clock}(Z|[+-]\\d{{2}}(:?\\d{{2}})?)?$");

    /// <summary>
    /// The type for a column of text when every value is a date, a time or a date-time, as GDAL
    /// typed it when it read the GeoJSON: <c>date</c>, <c>time</c>, or <c>timestamp with time zone</c>,
    /// which also takes dates mixed with date-times; none when any value is other text, or a date
    /// that cannot be, such as 2024-13-45. The values go into the script as written, and PostgreSQL
    /// reads them as the type says.
    /// </summary>
    public static Maybe<string> TimeTypeOf(IEnumerable<string> values)
    {
        var kinds = values.Select(KindOf).Distinct().ToList();
        return kinds switch
        {
            ["date"] => "date",
            ["time"] => "time",
            _ when kinds.Count > 0 && kinds.All(kind => kind is "date" or "timestamp") => "timestamp with time zone",
            _ => Maybe<string>.None,
        };
    }

    // One value's kind: "date", "time", "timestamp", or "text" for anything else.
    private static string KindOf(string value) =>
        DateOnly.Match(value) is { Success: true } date && Fits(date) ? "date"
        : TimeOnly.Match(value) is { Success: true } time && Fits(time) ? "time"
        : DateAndTime.Match(value) is { Success: true } moment && Fits(moment) ? "timestamp"
        : "text";

    // True when each part found lies within its range, as GDAL checks: month 1 to 12, day 1 to 31,
    // hour 0 to 23, minute 0 to 59, second 0 to 60, which allows a leap second.
    private static bool Fits(Match found)
    {
        static bool Within(Group part, int least, int most) =>
            !part.Success || int.Parse(part.Value, CultureInfo.InvariantCulture) is var number && number >= least && number <= most;
        return Within(found.Groups["month"], 1, 12) && Within(found.Groups["day"], 1, 31)
            && Within(found.Groups["hour"], 0, 23) && Within(found.Groups["minute"], 0, 59) && Within(found.Groups["second"], 0, 60);
    }

    /// <summary>
    /// A value as SQL for a column of the given type: NULL for none; text in single quotes, each quote
    /// inside doubled; numbers as digits, a fraction with every digit it holds; true and false as
    /// 't' and 'f', or as 1 and 0 in a column of numbers.
    /// </summary>
    public static string Literal(Maybe<object> value, string type)
    {
        if (!value.TryGetValue(out var held)) return "NULL";
        return (type, held) switch
        {
            ("BOOLEAN", bool flag) => flag ? "'t'" : "'f'",
            (not "VARCHAR", bool flag) => flag ? "1" : "0",
            (not "VARCHAR", long number) => number.ToString(CultureInfo.InvariantCulture),
            (not "VARCHAR", double number) => number.ToString("R", CultureInfo.InvariantCulture),
            (_, bool flag) => flag ? "'true'" : "'false'",
            (_, long number) => Text(number.ToString(CultureInfo.InvariantCulture)),
            (_, double number) => Text(number.ToString("R", CultureInfo.InvariantCulture)),
            _ => Text(Convert.ToString(held, CultureInfo.InvariantCulture) ?? ""),
        };
    }

    // Text in single quotes, each quote inside doubled. With standard_conforming_strings on, a
    // backslash is an ordinary character, so nothing else needs escaping.
    private static string Text(string text) => $"'{text.Replace("'", "''", StringComparison.Ordinal)}'";

    // A name in double quotes, each double quote inside doubled, as Queries.Quote does in the service.
    private static string Quote(string name) => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    // A table of lines or areas keeps one kind of shape in its column, so one part becomes a set of
    // one, as GDAL's PROMOTE_TO_MULTI made it.
    private static Geometry Multi(Geometry shape) => shape switch
    {
        Polygon area => shape.Factory.CreateMultiPolygon([area]),
        LineString line => shape.Factory.CreateMultiLineString([line]),
        Point point => shape.Factory.CreateMultiPoint([point]),
        _ => shape,
    };

    // The shape as PostGIS's extended well-known binary, in hexadecimal: the form GDAL wrote, which
    // PostGIS reads into a geometry column directly. The copy carries SRID 4326, since the extract
    // put every shape in longitude and latitude.
    private static string Ewkb(Geometry shape)
    {
        var copy = shape.Copy();
        copy.SRID = 4326;
        return Convert.ToHexString(Shapes.Write(copy));
    }
}
