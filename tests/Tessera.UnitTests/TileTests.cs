using Modules.Tessera.Domain;
using Modules.Tessera.Features;
using Modules.Tessera.Infrastructure;
using Modules.Tessera.Infrastructure.Sql;

namespace Tessera.UnitTests;

/// <summary>
/// US-07 and S-01 test 2: the <b><i>tile</i></b> query's text, and the tile addresses the service
/// accepts.
/// </summary>
public sealed class TileTests
{
    /// <summary>
    /// A <b><i>layer</i></b> shaped like the shipped <c>food.cafes</c>: its first four properties,
    /// in the seed's order, and its primary key, <c>fid</c>, the key the seed gives every table.
    /// So the query under test carries a column list and the ORDER BY a shipped table gets, which
    /// fixes the order of features (US-24). There, as in the seed, <c>id</c> is a property, not
    /// the key.
    /// </summary>
    private static readonly Layer Cafes = CatalogueReader.ToLayer(new CatalogueRow("food", "cafes", "geom", 4326, "POINT", """["id","layer","group","name"]""", """["fid"]"""));

    /// <summary>
    /// A view of the same places. A view has no primary key, so the catalogue reads an empty key
    /// list for it (Queries.Layers).
    /// </summary>
    private static readonly Layer CafesView = CatalogueReader.ToLayer(new CatalogueRow("food", "cafes_view", "geom", 4326, "POINT", """["id","name"]""", "[]"));

    /// <summary>
    /// A tile of Macau at level 15: the one that holds the area's home point, 113.55 E, 22.19 N,
    /// as tessera.json sets it. Normal data comes from the area Tessera serves, so a reader who
    /// works the numbers back finds Macau, not a point near 118.3 E.
    /// </summary>
    private static readonly TileAddress Home = TileAddress.Of(15, 26719, 14311)!;

    /// <summary>
    /// A buffer fixed here, apart from the shipped 256, so a change to that default leaves the
    /// expected <b><i>parameter</i></b> values below unchanged.
    /// </summary>
    private static readonly TileSettings Limits = new() { Buffer = 64 };

    /// <summary>
    /// The approved text. Line endings are made alike and the trailing newline dropped, so a checkout
    /// on Windows, which writes '\r\n', compares equal.
    /// </summary>
    private static string Approved => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "approved", "tile-query.sql")).ReplaceLineEndings().TrimEnd();

    // Web-map levels run from 0 to 22 (UI-09), and level z has 2^z columns and rows, numbered from 0;
    // so 4194303, 2^22 - 1, is the last column at level 22. Anything past these edges names no tile.
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(15, 26719, 14311)]
    [InlineData(22, 4194303, 0)]
    public void Accepts_a_tile_that_exists(int z, int x, int y) =>
        Assert.NotNull(TileAddress.Of(z, x, y));

    // Whatever three numbers arrive, an address that exists lies on its level's grid: the private
    // constructor leaves Of the one way to make one.
    [FsCheck.Xunit.Property]
    public bool Gives_only_addresses_on_the_grid(int z, int x, int y) =>
        TileAddress.Of(z, x, y) is not { } tile
        || (tile.Z is >= 0 and <= 22 && tile.X >= 0 && tile.Y >= 0 && tile.X < 1 << tile.Z && tile.Y < 1 << tile.Z);

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(23, 0, 0)]
    [InlineData(1, 2, 0)]
    [InlineData(1, 0, -1)]
    public void Refuses_a_tile_outside_the_grid(int z, int x, int y) =>
        Assert.Null(TileAddress.Of(z, x, y));

    // S-01 test 2: the tile query's text matches an approved snapshot, approved/tile-query.sql. A change
    // to the query fails this test until someone reads the new text and copies it over the old file.
    [Fact]
    public void Matches_the_approved_tile_query() =>
        Assert.Equal(Approved, Queries.Tile(Cafes, Home, Limits).Text.ReplaceLineEndings().TrimEnd());

    // US-24 test 2: a view has no key, so its tile query carries no ORDER BY. An ORDER BY on a
    // column the view lacks would fail every tile of it; any other order would be a guess.
    [Fact]
    public void Gives_a_view_no_order()
    {
        var text = Queries.Tile(CafesView, Home, Limits).Text;

        Assert.DoesNotContain("ORDER BY", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT @limit", text, StringComparison.Ordinal);
    }

    // S-01 c1: whatever tile a request names, the text stays the approved text; the numbers travel as
    // parameters alone.
    [FsCheck.Xunit.Property]
    public bool Keeps_every_tile_address_out_of_the_text(byte level, uint column, uint row)
    {
        // Folds any random number onto a real tile: a level from 0 to 22, then a column and row
        // within that level's 2^z.
        var z = level % 23;
        var size = 1L << z;
        var tile = TileAddress.Of(z, (int)(column % size), (int)(row % size))!;
        return Queries.Tile(Cafes, tile, Limits).Text.ReplaceLineEndings().TrimEnd() == Approved;
    }

    // S-01 c1: the layer's name and the address travel as parameters, never in the text. The margin
    // is the buffer as a share of the tile, 64 / 4096; the limit is the 10,000 features a tile may
    // carry (US-07).
    [Fact]
    public void Sends_the_layer_and_the_tile_as_parameters()
    {
        var query = Queries.Tile(Cafes, Home, Limits);

        Assert.DoesNotContain("food.cafes", query.Text, StringComparison.Ordinal);
        Assert.Equal(new { layer = "food.cafes", resolution = 4096, z = 15, x = 26719, y = 14311, buffer = 64, margin = 0.015625, srid = 4326, limit = 10000 }.ToString(), query.Parameters?.ToString());
    }

    // A column a seed script dropped since the list was read gives 42703. The tile answers 503, so the
    // browser tries again, and the list heals at its next refresh (N-23).
    [Fact]
    public async Task Answers_503_for_a_dropped_column()
    {
        var server = new TileServer(
            () => CatalogueReader.FromRows([new CatalogueRow("food", "cafes", "geom", 4326, "POINT", "[]")]),
            (_, _) => Task.FromResult(Result.Failure<byte[]?, DatabaseError>(DatabaseError.Of("42703", "column does not exist"))),
            Limits);

        var answer = await server.Answer("food.cafes", 15, 26719, 14311, TestContext.Current.CancellationToken);

        Assert.Equal(503, Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IStatusCodeHttpResult>(answer).StatusCode);
    }

    // N-18 test 1: the header follows the setting.
    [Theory]
    [InlineData(60, "public, max-age=60")]
    [InlineData(0, "no-cache")]
    public void Tells_the_browser_and_nginx_how_long_to_keep_a_tile(int seconds, string header) =>
        Assert.Equal(header, TileServer.CacheControl(seconds));

    // N-19 c1: equal bytes give an equal tag, and the tag is weak.
    [Fact]
    public void Tags_a_tile_by_its_bytes()
    {
        Assert.Equal(TileServer.Tag([1, 2, 3]), TileServer.Tag([1, 2, 3]));
        Assert.NotEqual(TileServer.Tag([1, 2, 3]), TileServer.Tag([1, 2, 4]));
        Assert.StartsWith("W/\"", TileServer.Tag([]).ToString(), StringComparison.Ordinal);
    }
}
