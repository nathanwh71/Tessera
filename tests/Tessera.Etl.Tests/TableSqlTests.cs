using CSharpFunctionalExtensions;

namespace Tessera.Etl.Tests;

/// <summary>
/// The SQL that makes and fills one table (TableSql.cs), checked against the text GDAL's PGDump
/// driver wrote for the same rows before 16 October 2026, so a new script loads as the old ones did.
/// </summary>
public sealed class TableSqlTests
{
    private static readonly TablePlan Cafes = new("food.cafes");

    private static Row Place(int number, Maybe<Geometry> shape, params (string Column, object? Value)[] values) =>
        new(number, shape, values.ToImmutableDictionary(value => value.Column, value => value.Value));

    private static readonly GeometryFactory Factory = new();

    private static Geometry Point(double x, double y) => Factory.CreatePoint(new Coordinate(x, y));

    // The statements GDAL wrote for one café, line for line, less its BEGIN and COMMIT, with the
    // statistics the script adds. The shape's hex is GDAL's own, for the point 113.55, 22.19.
    [Fact]
    public void Writes_a_table_as_GDAL_wrote_it()
    {
        var rows = ImmutableArray.Create(Place(1, Point(113.55, 22.19), ("id", "cafes-001"), ("name", "O'Neil's"), ("open", true)));

        var sql = TableSql.Statements(Cafes, rows, ["id", "name", "open"]).Value;

        Assert.Equal(
        [
            "SET standard_conforming_strings = ON;",
            "DROP TABLE IF EXISTS \"food\".\"cafes\" CASCADE;",
            "CREATE TABLE \"food\".\"cafes\"();",
            "ALTER TABLE \"food\".\"cafes\" ADD COLUMN \"fid\" SERIAL CONSTRAINT \"cafes_pk\" PRIMARY KEY;",
            "SELECT AddGeometryColumn('food','cafes','geom',4326,'POINT',2);",
            "ALTER TABLE \"food\".\"cafes\" ADD COLUMN \"id\" VARCHAR;",
            "ALTER TABLE \"food\".\"cafes\" ADD COLUMN \"name\" VARCHAR;",
            "ALTER TABLE \"food\".\"cafes\" ADD COLUMN \"open\" BOOLEAN;",
            "INSERT INTO \"food\".\"cafes\" (\"geom\", \"id\", \"name\", \"open\") VALUES ('0101000020E61000003333333333635C40713D0AD7A3303640', 'cafes-001', 'O''Neil''s', 't');",
            "CREATE INDEX \"cafes_geom_geom_idx\" ON \"food\".\"cafes\" USING GIST (\"geom\");",
            "ANALYZE \"food\".\"cafes\";",
        ], sql);
    }

    // A row with no shape leaves the column out of its INSERT, so it holds NULL; a value it lacks
    // is NULL too.
    [Fact]
    public void Leaves_out_a_shape_a_row_lacks()
    {
        var rows = ImmutableArray.Create(Place(1, Maybe<Geometry>.None, ("name", null)));

        Assert.Contains("INSERT INTO \"food\".\"cafes\" (\"name\") VALUES (NULL);", TableSql.Statements(Cafes, rows, ["name"]).Value);
    }

    // A table of lines or areas stores one part as a set of one, so the column keeps one type, as
    // GDAL's PROMOTE_TO_MULTI did; shapes of two kinds take the column type that holds any.
    [Fact]
    public void Gives_the_column_the_one_type_its_shapes_share()
    {
        var line = Factory.CreateLineString([new Coordinate(113.55, 22.19), new Coordinate(113.56, 22.2)]);
        var lines = ImmutableArray.Create(Place(1, line), Place(2, Factory.CreateMultiLineString([line])));
        var mixed = ImmutableArray.Create(Place(1, Point(113.55, 22.19)), Place(2, line));

        var promoted = TableSql.Statements(Cafes with { Lines = true }, lines, []).Value;

        Assert.Contains("SELECT AddGeometryColumn('food','cafes','geom',4326,'MULTILINESTRING',2);", promoted);
        Assert.All(promoted.Where(statement => statement.StartsWith("INSERT", StringComparison.Ordinal)),
            insert => Assert.Contains("'0105000020E6100000", insert, StringComparison.Ordinal));
        Assert.Contains("SELECT AddGeometryColumn('food','cafes','geom',4326,'GEOMETRY',2);", TableSql.Statements(Cafes, mixed, []).Value);
    }

    // GDAL's names: A to Z lower case, each ' - # made _, cut to 63 bytes; other letters stay.
    [Theory]
    [InlineData("Name Zh", "name zh")]
    [InlineData("has-dash#", "has_dash_")]
    [InlineData("Ünï", "Ünï")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ABCDEFGH", "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz0123456789a")]
    public void Names_columns_as_GDAL_did(string column, string name) => Assert.Equal(name, TableSql.Launder(column));

    // The types GDAL chose, but for text mixed with other kinds, which stays text rather than JSON,
    // a type the tile query leaves out.
    [Theory]
    [InlineData("VARCHAR")]
    [InlineData("VARCHAR", "a", null)]
    [InlineData("INTEGER", 3L, 4L)]
    [InlineData("INT8", 3L, 5_000_000_000L)]
    [InlineData("FLOAT8", 3L, 2.5)]
    [InlineData("BOOLEAN", true, false)]
    [InlineData("INTEGER", true, 2L)]
    [InlineData("VARCHAR", "a", 3.5)]
    [InlineData("VARCHAR", "a", false)]
    public void Types_a_column_by_the_values_it_holds(string type, params object?[] values) =>
        Assert.Equal(type, TableSql.TypeOf(values.Select(value => value.AsMaybe())));

    // Text that is all dates, times or date-times takes that type, as GDAL typed it; a date that
    // cannot be, or any other text beside the dates, keeps the column as text.
    [Theory]
    [InlineData("date", "2024-05-01", "2023/01/02")]
    [InlineData("time", "10:30:00", "11:00:00.5")]
    [InlineData("timestamp with time zone", "2024-05-01T10:30:00Z", "2024-05-01 11:00:00")]
    [InlineData("timestamp with time zone", "2024-05-01T10:30:00.123+08:00", "2024-05-01T10:30:00-05")]
    [InlineData("timestamp with time zone", "2024-05-01", "2024-05-01T10:30:00")]
    [InlineData("VARCHAR", "2024-05-01", "soon")]
    [InlineData("VARCHAR", "2024-13-45")]
    [InlineData("VARCHAR", "2024-5-1")]
    [InlineData("VARCHAR", "2024-05-01", "10:30:00")]
    public void Types_dates_and_times_as_GDAL_did(string type, params string[] values) =>
        Assert.Equal(type, TableSql.TypeOf(values.Select(value => Maybe.From<object>(value))));

    // A date goes into the script as written, in quotes, and PostgreSQL reads it as the column's type.
    [Fact]
    public void Writes_a_date_as_its_text() =>
        Assert.Equal("'2024-05-01T10:30:00Z'", TableSql.Literal(Maybe.From<object>("2024-05-01T10:30:00Z"), "timestamp with time zone"));

    // Every digit a number holds survives, where GDAL kept fifteen and wrote 0.3.
    [Theory]
    [InlineData(0.30000000000000004, "FLOAT8", "0.30000000000000004")]
    [InlineData(3L, "FLOAT8", "3")]
    [InlineData(true, "INTEGER", "1")]
    [InlineData(3.5, "VARCHAR", "'3.5'")]
    [InlineData(false, "VARCHAR", "'false'")]
    [InlineData("a\\b", "VARCHAR", "'a\\b'")]
    public void Writes_each_value_for_its_column(object value, string type, string literal) =>
        Assert.Equal(literal, TableSql.Literal(Maybe.From(value), type));

    // Two columns that become one name would make a script the database refuses, so the run stops.
    [Fact]
    public void Refuses_two_columns_that_become_one_name()
    {
        var rows = ImmutableArray.Create(Place(1, Point(113.55, 22.19), ("Name", "A"), ("name", "B")));

        Assert.Equal("food.cafes: two columns become \"name\"; rename one in the source.", TableSql.Statements(Cafes, rows, ["Name", "name"]).Error);
    }

    [Fact]
    public void Refuses_a_table_with_no_rows() =>
        Assert.Equal("food.cafes: no rows to write.", TableSql.Statements(Cafes, [], []).Error);
}
