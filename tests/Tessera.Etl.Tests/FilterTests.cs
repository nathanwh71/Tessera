namespace Tessera.Etl.Tests;

/// <summary>Each filter, alone: what it keeps, fixes and drops, and when it stops the run.</summary>
public sealed class FilterTests
{
    private static readonly GeometryFactory Shapes = new(new PrecisionModel(), 4326);

    // The area: Macau's box, as tessera.json holds it.
    private static readonly Area Macau = new(113.52, 22.10, 113.61, 22.22);

    internal static Row Place(int number, double longitude, double latitude, params (string Column, object? Value)[] values) =>
        new(number, Shapes.CreatePoint(new Coordinate(longitude, latitude)),
            values.ToImmutableDictionary(value => value.Column, value => value.Value, StringComparer.Ordinal));

    private static Outcome Run(string filter, ImmutableArray<Row> rows, OnFail onFail = OnFail.Drop, string[]? fields = null, string? field = null, double? metres = null) =>
        Filters.For(new FilterStep(filter, onFail, fields is null ? null : [.. fields], field, metres), Macau)(rows);

    [Fact]
    public void Drops_a_row_with_no_shape_or_an_empty_one()
    {
        var outcome = Run("dropEmptyShapes", [Place(1, 113.55, 22.19), new Row(2, null, []), new Row(3, Shapes.CreatePoint(), [])]);

        Assert.Equal([1], outcome.Rows.Select(row => row.Number));
        Assert.Equal(["Row 2: dropped by dropEmptyShapes: it has no shape", "Row 3: dropped by dropEmptyShapes: it has no shape"], outcome.Log.Select(line => line.ToString()));
    }

    [Fact]
    public void Stops_at_the_first_bad_row_when_told_to_and_keeps_every_row()
    {
        var rows = ImmutableArray.Create(Place(1, 113.55, 22.19), Place(2, 0.5, 51.5), Place(3, 0.6, 51.6));

        var outcome = Run("insideArea", rows, OnFail.Stop);

        Assert.Equal("insideArea stopped the run at row 2: its shape lies outside the area.", outcome.Stop);
        Assert.Equal(Act.Stopped, Assert.Single(outcome.Log).Act);
        Assert.Equal(3, outcome.Rows.Length);
    }

    [Fact]
    public void Swaps_a_point_whose_longitude_and_latitude_changed_places()
    {
        var outcome = Run("swapCoordinates", [Place(1, 22.19, 113.55), Place(2, 113.55, 22.19), Place(3, 0.5, 51.5)]);

        Assert.Equal(new Coordinate(113.55, 22.19), outcome.Rows[0].Shape!.Coordinate);
        // A place already inside, and one outside whichever way round, stay as they were.
        Assert.Equal(new Coordinate(113.55, 22.19), outcome.Rows[1].Shape!.Coordinate);
        Assert.Equal(new Coordinate(0.5, 51.5), outcome.Rows[2].Shape!.Coordinate);
        Assert.Equal("Row 1: fixed by swapCoordinates: swapped longitude and latitude", Assert.Single(outcome.Log).ToString());
    }

    [Fact]
    public void Keeps_a_line_that_runs_out_of_the_area()
    {
        var road = new Row(1, Shapes.CreateLineString([new Coordinate(113.60, 22.15), new Coordinate(113.70, 22.15)]), []);

        Assert.Single(Run("insideArea", [road]).Rows);
    }

    [Fact]
    public void Repairs_an_area_whose_edge_crosses_itself()
    {
        var bowTie = Shapes.CreatePolygon([new(113.54, 22.18), new(113.55, 22.19), new(113.55, 22.18), new(113.54, 22.19), new(113.54, 22.18)]);
        Assert.False(bowTie.IsValid);

        var outcome = Run("repairShapes", [new Row(1, bowTie, [])]);

        Assert.True(outcome.Rows[0].Shape!.IsValid);
        Assert.StartsWith("Row 1: fixed by repairShapes: repaired a shape that was invalid (Self-intersection", Assert.Single(outcome.Log).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Tidies_text_into_one_form_with_single_spaces()
    {
        // 'e' followed by a combining accent is the same letter as 'é', written two ways.
        var outcome = Run("tidyText", [Place(1, 113.55, 22.19, ("name", "  Café \t Uno  "), ("note", "   "), ("floors", 3L))]);

        Assert.Equal("Café Uno", outcome.Rows[0].Value("name"));
        Assert.Null(outcome.Rows[0].Value("note"));
        Assert.Equal(3L, outcome.Rows[0].Value("floors"));
        Assert.Equal("Row 1: fixed by tidyText: tidied name, note", Assert.Single(outcome.Log).ToString());
    }

    [Fact]
    public void Tidies_only_the_fields_named()
    {
        var outcome = Run("tidyText", [Place(1, 113.55, 22.19, ("name", " a "), ("code", " b "))], fields: ["name"]);

        Assert.Equal(("a", " b "), ((string?)outcome.Rows[0].Value("name"), (string?)outcome.Rows[0].Value("code")));
    }

    [Fact]
    public void Drops_a_row_missing_a_required_field_or_holding_only_spaces()
    {
        var outcome = Run("requireFields", [Place(1, 113.55, 22.19, ("name", "A")), Place(2, 113.55, 22.19, ("name", "  ")), Place(3, 113.55, 22.19)], fields: ["name"]);

        Assert.Equal([1], outcome.Rows.Select(row => row.Number));
        Assert.All(outcome.Log, line => Assert.Equal("name is missing", line.Detail));
    }

    [Fact]
    public void Keeps_the_first_of_two_rows_with_one_id()
    {
        var outcome = Run("dropDuplicateIds", [Place(1, 113.55, 22.19, ("id", 7L)), Place(2, 113.55, 22.19, ("id", 8L)), Place(3, 113.56, 22.20, ("id", 7L)), Place(4, 113.56, 22.20)], field: "id");

        Assert.Equal([1, 2, 4], outcome.Rows.Select(row => row.Number));
        Assert.Equal("Row 3: dropped by dropDuplicateIds: it repeats id '7' of row 1", Assert.Single(outcome.Log).ToString());
    }

    [Fact]
    public void Drops_a_place_entered_twice_nearby_and_keeps_one_of_the_same_name_far_off()
    {
        // 0.0001 degrees of latitude is about 11 metres; 0.01 is about 1.1 kilometres.
        var outcome = Run("dropNearDuplicates",
            [Place(1, 113.55, 22.19, ("name", "Café Uno")), Place(2, 113.55, 22.1901, ("name", "café  uno ")), Place(3, 113.55, 22.20, ("name", "Café Uno"))],
            field: "name", metres: 25);

        Assert.Equal([1, 3], outcome.Rows.Select(row => row.Number));
        Assert.Equal("Row 2: dropped by dropNearDuplicates: its name matches row 1, 11 m away", Assert.Single(outcome.Log).ToString());
    }

    [Fact]
    public void Measures_a_degree_of_latitude_as_about_111_kilometres()
    {
        var metres = Filters.Metres(Shapes.CreatePoint(new Coordinate(113.55, 22)), Shapes.CreatePoint(new Coordinate(113.55, 23)));

        Assert.InRange(metres, 111_150, 111_250);
    }
}
