using FsCheck;
using FsCheck.Xunit;
using Tessera.Sql;

namespace Tessera.UnitTests;

/// <summary>US-03 test 1 and S-01 test 1: names are quoted one way, and values never enter SQL text.</summary>
public sealed class SqlTests
{
    [Fact]
    public void Quotes_a_name_and_doubles_each_inner_quote() =>
        Assert.Equal("\"odd \"\"name\"\"\"", QuotedName.Quote("odd \"name\"").Match(name => name.Text, error => error));

    [Fact]
    public void Refuses_an_empty_name() =>
        Assert.True(QuotedName.Quote("").Match(_ => false, _ => true));

    // Every name, however odd, becomes text that opens with a quote, closes with one, and holds
    // no lone quote between: so no name can end its quotes early and smuggle in SQL.
    [Property]
    public bool Quotes_every_name_safely(NonEmptyString name) =>
        QuotedName.Quote(name.Get).Match(
            quoted =>
                quoted.Text.StartsWith('"') && quoted.Text.EndsWith('"')
                && !quoted.Text[1..^1].Replace("\"\"", "", StringComparison.Ordinal).Contains('"')
                && quoted.Raw == name.Get,
            _ => false);

    // S-01 test 1: the SQL text is the same whatever value travels with it, so no value can
    // change what a query does. The value lives among the parameters instead.
    [Property]
    public bool Keeps_every_value_out_of_the_text(NonNull<string> value)
    {
        var query = Query.Of($"SELECT * FROM places WHERE name = {value.Get}");
        return query.Text == "SELECT * FROM places WHERE name = @p0" && (string)query.Parameters[0] == value.Get;
    }

    [Fact]
    public void Turns_an_attack_into_a_placeholder()
    {
        var attack = "'; DROP TABLE roads; --";
        var query = Query.Of($"SELECT 1 WHERE {attack} = {attack}");

        Assert.Equal("SELECT 1 WHERE @p0 = @p1", query.Text);
        Assert.DoesNotContain("DROP", query.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Writes_a_quoted_name_into_the_text() =>
        Assert.Equal(
            "SELECT * FROM \"food\".\"cafes\"",
            TableName.Of("food", "cafes").Match(table => Query.Of($"SELECT * FROM {table}").Text, error => error));
}
