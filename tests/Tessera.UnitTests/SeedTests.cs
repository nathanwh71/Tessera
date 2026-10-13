using System.Text.Json.Nodes;
using Tessera.Import;
using Tessera.Settings;
using Tessera.Sql;

namespace Tessera.UnitTests;

/// <summary>US-24 c2 to c4, and tests 2 to 4: the import reads degrees alone, and refuses anything else.</summary>
public sealed class SeedTests
{
    private static readonly AreaBox Macau = new(113.52, 22.10, 113.61, 22.22);

    /// <summary>A sample file with two places for each of the nine layers (US-24 test 1).</summary>
    public static string Sample(Func<JsonObject, JsonObject>? change = null)
    {
        var features = new JsonArray();
        var number = 0;
        foreach (var (layer, group) in Layers.GroupOfLayer)
        {
            for (var copy = 1; copy <= 2; copy++)
            {
                number++;
                features.Add(new JsonObject
                {
                    ["type"] = "Feature",
                    ["id"] = $"{layer}-{copy:000}",
                    ["geometry"] = new JsonObject
                    {
                        ["type"] = "Point",
                        ["coordinates"] = new JsonArray(113.54 + (number * 0.001), 22.19),
                    },
                    ["properties"] = new JsonObject
                    {
                        ["id"] = $"{layer}-{copy:000}",
                        ["layer"] = layer,
                        ["group"] = group,
                        ["name"] = $"{layer} {copy}",
                        ["sample"] = true,
                        ["website"] = null,
                    },
                });
            }
        }

        var file = new JsonObject { ["type"] = "FeatureCollection", ["features"] = features };
        return (change?.Invoke(file) ?? file).ToJsonString();
    }

    private static string Problem(string json) => Seed.Read(json, Macau).Match(_ => "accepted", error => error.Message);

    private static JsonObject FirstFeature(JsonObject file) => (JsonObject)file["features"]![0]!;

    [Fact]
    public void Accepts_the_sample() =>
        Assert.Equal(18, Seed.Read(Sample(), Macau).Match(places => places.Length, _ => 0));

    // US-24 test 2: degrees stay degrees.
    [Fact]
    public void Keeps_degrees_unchanged()
    {
        var first = Seed.Read(Sample(), Macau).Match(places => places[0], error => throw new Xunit.Sdk.XunitException(error.Message));
        Assert.Equal((113.541, 22.19), (Math.Round(first.Longitude, 6), first.Latitude));
    }

    // US-24 test 3: another projection is refused, and the message asks for WGS 84.
    [Fact]
    public void Refuses_a_file_that_names_web_mercator() =>
        Assert.Contains("WGS 84", Problem(Sample(file =>
        {
            file["crs"] = new JsonObject { ["type"] = "name", ["properties"] = new JsonObject { ["name"] = "urn:ogc:def:crs:EPSG::3857" } };
            return file;
        })), StringComparison.Ordinal);

    // US-24 test 4: swapped numbers, or metres in place of degrees, fall outside the box.
    [Theory]
    [InlineData(22.19, 113.54)]
    [InlineData(12639215.0, 2534352.0)]
    public void Refuses_swapped_or_projected_numbers(double first, double second)
    {
        var message = Problem(Sample(file =>
        {
            FirstFeature(file)["geometry"]!["coordinates"] = new JsonArray(first, second);
            return file;
        }));

        Assert.Contains("outside the area's box", message, StringComparison.Ordinal);
        Assert.Contains("'buses-001' (number 1 in the file)", message, StringComparison.Ordinal);
    }

    [Fact]
    public void Refuses_a_feature_that_is_not_a_point() =>
        Assert.Contains("is not a point", Problem(Sample(file =>
        {
            FirstFeature(file)["geometry"]!["type"] = "LineString";
            return file;
        })), StringComparison.Ordinal);

    [Fact]
    public void Refuses_a_layer_outside_the_nine() =>
        Assert.Contains("names no layer of the nine", Problem(Sample(file =>
        {
            FirstFeature(file)["properties"]!["layer"] = "zoos";
            return file;
        })), StringComparison.Ordinal);

    [Fact]
    public void Refuses_a_group_that_differs_from_the_layers_group() =>
        Assert.Contains("gives a group other than 'transport'", Problem(Sample(file =>
        {
            FirstFeature(file)["properties"]!["group"] = "food";
            return file;
        })), StringComparison.Ordinal);

    [Fact]
    public void Refuses_text_that_is_not_json() =>
        Assert.Contains("not valid JSON", Problem("{ not json"), StringComparison.Ordinal);

    [Fact]
    public void Plans_nine_tables_in_four_schemas_with_points_in_4326()
    {
        var queries = Seed.Read(Sample(), Macau).Bind(Importer.Plan)
            .Match(found => found.Select(query => query.Text).ToArray(), error => throw new Xunit.Sdk.XunitException(error.Message));

        Assert.Equal(4, queries.Count(text => text.StartsWith("CREATE SCHEMA", StringComparison.Ordinal)));
        Assert.Equal(9, queries.Count(text => text.Contains("geometry(Point, 4326)", StringComparison.Ordinal)));
        Assert.Equal(9, queries.Count(text => text.Contains("USING GIST", StringComparison.Ordinal)));
        Assert.Equal(9, queries.Count(text => text.StartsWith("TRUNCATE", StringComparison.Ordinal)));
        Assert.Equal(18, queries.Count(text => text.StartsWith("INSERT", StringComparison.Ordinal)));
        Assert.Equal(9, queries.Count(text => text.StartsWith("ANALYZE", StringComparison.Ordinal)));
        Assert.DoesNotContain(queries, text => text.Contains("GRANT", StringComparison.Ordinal));
    }

    // The sample's 'sample' property is always true or false, so its column is boolean.
    [Fact]
    public void Gives_each_column_the_type_its_values_share() =>
        Assert.Contains(
            Seed.Read(Sample(), Macau).Bind(Importer.Plan).Match(found => found.Select(query => query.Text), _ => []),
            text => text.EndsWith($"ADD COLUMN IF NOT EXISTS \"sample\" {SqlType.Boolean.Keyword}", StringComparison.Ordinal));
}
