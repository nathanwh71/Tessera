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
}
