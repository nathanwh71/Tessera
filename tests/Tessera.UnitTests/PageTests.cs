using System.Text.Json;
using Tessera.Map;
using Tessera.Settings;

namespace Tessera.UnitTests;

/// <summary>US-23 c1, c5 and c7, and ADR 0006's contract: the page carries the area, in the one block the map reads.</summary>
public sealed class PageTests
{
    private static readonly AreaSettings Macau = new();
    private const string Html = "<!doctype html><html><head><title>Map</title></head><body><div id=\"root\"></div></body></html>";

    [Fact]
    public void Writes_the_area_block_before_the_head_closes()
    {
        var page = Page.Render(Html, Macau);
        var start = page.IndexOf("<script id=\"tessera-area\" type=\"application/json\">", StringComparison.Ordinal);
        var end = page.IndexOf("</script>", StringComparison.Ordinal);

        Assert.True(start > 0 && end > start && page.IndexOf("</head>", StringComparison.Ordinal) > end);
        using var block = JsonDocument.Parse(page[(start + "<script id=\"tessera-area\" type=\"application/json\">".Length)..end]);
        Assert.Equal("Macau", block.RootElement.GetProperty("name").GetString());
        Assert.Equal([113.52, 22.10, 113.61, 22.22], block.RootElement.GetProperty("bounds").EnumerateArray().Select(number => number.GetDouble()));
        Assert.Equal(15, block.RootElement.GetProperty("home").GetProperty("zoom").GetDouble());
        Assert.Equal(113.55, block.RootElement.GetProperty("home").GetProperty("center").GetProperty("lng").GetDouble());
    }

    // ADR 0006 fixes the block's shape word for word; this is the text the Java build writes.
    [Fact]
    public void Matches_the_java_builds_block() =>
        Assert.Contains(
            """{"name":"Macau","bounds":[113.52,22.1,113.61,22.22],"home":{"center":{"lng":113.55,"lat":22.19},"zoom":15}}""",
            Page.Render(Html, Macau), StringComparison.Ordinal);

    [Fact]
    public void Answers_off_when_the_preview_is_switched_off() =>
        Assert.Equal("off", Page.Of(Option.Some(Html), Macau, new MapSettings { ShowPreview = false }).Match(_ => "shown", () => "off", () => "missing"));

    [Fact]
    public void Answers_missing_when_the_release_carries_no_map() =>
        Assert.Equal("missing", Page.Of(Option.None<string>(), Macau, new MapSettings()).Match(_ => "shown", () => "off", () => "missing"));

    [Theory]
    [InlineData("food.cafes.html", "food.cafes")]
    [InlineData("transport.buses.html", "transport.buses")]
    public void Opens_a_page_named_for_one_of_the_nine_layers(string file, string layer) =>
        Assert.Equal(layer, Page.LayerOf(file).OrElse(""));

    [Theory]
    [InlineData("index.html")]
    [InlineData("food.zoos.html")]
    [InlineData("food.cafes")]
    [InlineData("food.cafes.htm")]
    public void Refuses_any_other_name(string file) =>
        Assert.Equal("", Page.LayerOf(file).OrElse(""));
}
