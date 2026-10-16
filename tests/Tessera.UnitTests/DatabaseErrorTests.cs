using Modules.Tessera.Infrastructure.Sql;

namespace Tessera.UnitTests;

/// <summary>N-22 test 1: which database failures ask the browser to wait, and which do not.</summary>
public sealed class DatabaseErrorTests
{
    // 08006 a lost connection, 57014 a query past its time, 57P01 a server stopping, 53300 no room
    // for another connection: each may pass, so the tile answers 503 with Retry-After.
    [Theory]
    [InlineData("08006")]
    [InlineData("57014")]
    [InlineData("57P01")]
    [InlineData("53300")]
    public void Asks_the_browser_to_wait_for_a_failure_that_may_pass(string sqlState) =>
        Assert.IsType<DatabaseError.Unavailable>(DatabaseError.Of(sqlState, ""));

    // 42501 a missing grant and 42P01 a missing table mean the layer has gone; 42703 a dropped
    // column; XX000 an internal fault, which waiting cannot cure.
    [Theory]
    [InlineData("42501", typeof(DatabaseError.MissingTable))]
    [InlineData("42P01", typeof(DatabaseError.MissingTable))]
    [InlineData("42703", typeof(DatabaseError.MissingColumn))]
    [InlineData("XX000", typeof(DatabaseError.Other))]
    public void Sorts_any_other_failure_into_its_kind(string sqlState, Type kind) =>
        Assert.IsType(kind, DatabaseError.Of(sqlState, ""));

    // The ExhaustiveMatching analyser checks each switch against the cases [Closed] names, but misses
    // a record that derives from DatabaseError without being named, so this test makes them agree.
    [Fact]
    public void Lists_every_kind_as_a_case()
    {
        var named = typeof(DatabaseError).GetCustomAttributes(typeof(ExhaustiveMatching.ClosedAttribute), false)
            .Cast<ExhaustiveMatching.ClosedAttribute>().Single().Cases;
        var derived = typeof(DatabaseError).Assembly.GetTypes().Where(type => type.BaseType == typeof(DatabaseError));

        Assert.Equal(named.OrderBy(type => type.Name), derived.OrderBy(type => type.Name));
    }
}
