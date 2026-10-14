using Tessera.Sql;

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
        Assert.True(new DatabaseError(sqlState, "").IsUnavailable);

    // 42501 a missing grant, XX000 an internal fault: waiting cures neither.
    [Theory]
    [InlineData("42501")]
    [InlineData("XX000")]
    public void Leaves_any_other_failure_alone(string sqlState) =>
        Assert.False(new DatabaseError(sqlState, "").IsUnavailable);
}
