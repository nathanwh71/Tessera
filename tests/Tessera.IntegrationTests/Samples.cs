using System.Text.Json.Nodes;
using Tessera.Import;

namespace Tessera.IntegrationTests;

/// <summary>Sample GeoJSON files for the tests, small enough to read at a glance.</summary>
public static class Samples
{
    /// <summary>A sample file with two places for each of the nine layers (US-24 test 1).</summary>
    public static string Sample(Func<JsonObject, JsonObject>? change = null, int copies = 2)
    {
        var features = new JsonArray();
        var number = 0;
        foreach (var (layer, group) in Layers.GroupOfLayer)
        {
            for (var copy = 1; copy <= copies; copy++)
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

}
