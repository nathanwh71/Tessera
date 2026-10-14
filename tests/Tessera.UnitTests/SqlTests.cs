using FsCheck;
using FsCheck.Xunit;
using Tessera.Sql;

namespace Tessera.UnitTests;

/// <summary>S-02: a name is quoted one way, so no name can end its quotes early and smuggle in SQL.</summary>
public sealed class SqlTests
{
    [Fact]
    public void Quotes_a_name_and_doubles_each_inner_quote() =>
        Assert.Equal("\"odd \"\"name\"\"\"", Queries.Quote("odd \"name\""));

    // Every name, however odd, opens with a quote, closes with one, and holds no lone quote between.
    [Property]
    public bool Quotes_every_name_safely(NonNull<string> name)
    {
        var quoted = Queries.Quote(name.Get);
        return quoted.StartsWith('"') && quoted.EndsWith('"')
            && !quoted[1..^1].Replace("\"\"", "", StringComparison.Ordinal).Contains('"');
    }

    // An import's values travel as parameters too: an attack in a place's id never reaches the text.
    [Fact]
    public void Keeps_an_inserted_value_out_of_the_text()
    {
        var attack = "'; DROP TABLE roads; --";
        var query = Queries.Insert("\"food\".\"cafes\"", ["\"name\""], attack, 113.5, 22.2, [attack]);

        Assert.DoesNotContain("DROP", query.Text, StringComparison.Ordinal);
        Assert.Equal("INSERT INTO \"food\".\"cafes\" (\"id\", \"geom\", \"name\") VALUES (@id, ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326), @p0)", query.Text);
    }
}
