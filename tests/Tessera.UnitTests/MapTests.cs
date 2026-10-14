using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Tessera.Map;
using Tessera.Settings;

namespace Tessera.UnitTests;

/// <summary>US-23 c1, c5 and c7, and ADR 0006's contract: the page carries the area, in the one block the map reads.</summary>
public sealed class MapTests
{
    private static readonly AreaSettings Macau = new();
    private const string Html = "<!doctype html><html><head><title>Map</title></head><body><div id=\"root\"></div></body></html>";

    [Fact]
    public void Writes_the_area_block_before_the_head_closes()
    {
        var page = MapRoutes.Render(Html, Macau);
        const string opening = "<script id=\"tessera-area\" type=\"application/json\">";
        var start = page.IndexOf(opening, StringComparison.Ordinal);
        var end = page.IndexOf("</script>", StringComparison.Ordinal);

        Assert.True(start > 0 && end > start && page.IndexOf("</head>", StringComparison.Ordinal) > end);
        using var block = JsonDocument.Parse(page[(start + opening.Length)..end]);
        Assert.Equal("Macau", block.RootElement.GetProperty("name").GetString());
        Assert.Equal([113.52, 22.10, 113.61, 22.22], block.RootElement.GetProperty("bounds").EnumerateArray().Select(number => number.GetDouble()));
    }

    // ADR 0006 fixes the block's shape word for word; this is the text the Java build writes.
    [Fact]
    public void Matches_the_java_builds_block() =>
        Assert.Contains(
            """{"name":"Macau","bounds":[113.52,22.1,113.61,22.22],"home":{"center":{"lng":113.55,"lat":22.19},"zoom":15}}""",
            MapRoutes.Render(Html, Macau), StringComparison.Ordinal);

    // US-23 c5: with the preview off, or no map in the release, the page answers 404 and says why.
    [Theory]
    [InlineData(false, Html, "The map preview is switched off.")]
    [InlineData(true, null, "This release carries no map.")]
    public void Answers_404_when_there_is_no_page_to_show(bool preview, string? page, string detail)
    {
        var answer = Assert.IsType<ProblemHttpResult>(MapRoutes.Answer(page, new MapSettings { ShowPreview = preview }));

        Assert.Equal(404, answer.StatusCode);
        Assert.Equal(detail, answer.ProblemDetails.Detail);
    }

    [Theory]
    [InlineData("food.cafes.html", "food.cafes")]
    [InlineData("transport.buses.html", "transport.buses")]
    public void Opens_a_page_named_for_one_of_the_nine_layers(string file, string layer) =>
        Assert.Equal(layer, MapRoutes.LayerOf(file));

    [Theory]
    [InlineData("index.html")]
    [InlineData("food.zoos.html")]
    [InlineData("food.cafes")]
    [InlineData("food.cafes.htm")]
    public void Refuses_any_other_name(string file) =>
        Assert.Null(MapRoutes.LayerOf(file));
}
