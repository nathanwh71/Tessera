using Tessera.Catalogue;

namespace Tessera.UnitTests;

/// <summary>US-03 test 2: a row of the database's list of tables becomes a layer, purely.</summary>
public sealed class CatalogueTests
{
    private static readonly CatalogueRow Cafes = new("food", "cafes", "geom", 4326, "POINT", """["layer","name"]""");

    [Fact]
    public void Turns_a_row_into_a_layer_whose_id_is_schema_dot_name()
    {
        var layer = LayerCatalogue.ToLayer(Cafes).Match(found => found, error => throw new Xunit.Sdk.XunitException(error));

        Assert.Equal("food.cafes", layer.Id);
        Assert.Equal("\"food\".\"cafes\"", layer.Table.ToString());
        Assert.Equal(["\"layer\"", "\"name\""], layer.Properties.Select(name => name.Text));
        Assert.Equal(4326, layer.Srid);
    }

    [Fact]
    public void Finds_a_layer_exactly_and_case_sensitively()
    {
        var catalogue = LayerCatalogue.FromRows([Cafes]);

        Assert.True(catalogue.Find("food.cafes").Match(_ => true, () => false));
        Assert.False(catalogue.Find("Food.Cafes").Match(_ => true, () => false));
        Assert.False(catalogue.Find("food").Match(_ => true, () => false));
    }

    // US-03 test 3: each column the catalogue query returns maps to one field of the row, by name.
    [Fact]
    public void Names_each_column_after_a_field_of_the_row()
    {
        var fields = typeof(CatalogueRow).GetConstructors().Single().GetParameters().Select(parameter => parameter.Name!.ToLowerInvariant());

        foreach (var field in fields)
            Assert.Contains($" AS {field}", LayerCatalogue.ListQuery.Text, StringComparison.Ordinal);
    }

    // US-03 c5: a function never becomes a layer; the query reads pg_class, where functions never appear.
    [Fact]
    public void Lists_tables_and_views_alone() =>
        Assert.Contains("c.relkind IN ('r', 'p', 'v', 'm')", LayerCatalogue.ListQuery.Text, StringComparison.Ordinal);
}
