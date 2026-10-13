using Tessera.Catalogue;
using Tessera.Settings;
using Tessera.Tiles;

namespace Tessera.UnitTests;

/// <summary>US-07 and S-01 test 2: the tile query's text, and the tile addresses the service accepts.</summary>
public sealed class TileTests
{
    private static readonly Layer Cafes = LayerCatalogue
        .ToLayer(new CatalogueRow("food", "cafes", "geom", 4326, "POINT", """["group","layer","name"]"""))
        .Match(layer => layer, error => throw new InvalidOperationException(error));

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(15, 27154, 14280)]
    [InlineData(22, 4194303, 0)]
    public void Accepts_a_tile_that_exists(int z, int x, int y) =>
        Assert.True(TileAddress.Of(z, x, y).Match(_ => true, _ => false));

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(23, 0, 0)]
    [InlineData(1, 2, 0)]
    [InlineData(1, 0, -1)]
    public void Refuses_a_tile_outside_the_grid(int z, int x, int y) =>
        Assert.False(TileAddress.Of(z, x, y).Match(_ => true, _ => false));

    // S-01 test 2: the tile query's text matches an approved snapshot, kept in
    // approved/tile-query.sql. A change to the query fails this test until someone reads the new
    // text and approves it by copying it over the old file: a person checks every change to the SQL.
    [Fact]
    public void Matches_the_approved_tile_query()
    {
        var query = TileQuery.For(Cafes, new TileAddress(15, 27154, 14280), new TileSettings(10000, 64));
        var approved = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "approved", "tile-query.sql"));

        Assert.Equal(approved.ReplaceLineEndings().TrimEnd(), query.Text.ReplaceLineEndings().TrimEnd());
        Assert.Equal(["food.cafes", "4096", "15", "27154", "14280", "4096", "64", "15", "27154", "14280", "0.015625", "4326", "10000"],
            query.Parameters.Select(value => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Keeps_the_tile_numbers_out_of_the_text()
    {
        var text = TileQuery.For(Cafes, new TileAddress(15, 27154, 14280), new TileSettings(10000, 64)).Text;

        Assert.DoesNotContain("27154", text, StringComparison.Ordinal);
        Assert.DoesNotContain("14280", text, StringComparison.Ordinal);
        Assert.DoesNotContain("food.cafes", text, StringComparison.Ordinal);
    }
}
