namespace Tessera.UnitTests;

/// <summary>
/// The service's own <b><i>Result</i></b> (Common/Result.cs): either a value or an error, never both
/// (ADR 0004).
/// </summary>
public sealed class ResultTests
{
    private static readonly Result<int, string> Twelve = Result.Success<int, string>(12);
    private static readonly Result<int, string> Refused = Result.Failure<int, string>("refused");

    [Fact]
    public void A_success_holds_its_value() =>
        Assert.Equal((true, false, 12), (Twelve.IsSuccess, Twelve.IsFailure, Twelve.Value));

    [Fact]
    public void A_failure_holds_its_error() =>
        Assert.Equal((false, true, "refused"), (Refused.IsSuccess, Refused.IsFailure, Refused.Error));

    // Reading the wrong side is a fault in the code, so it throws rather than handing back a default.
    [Fact]
    public void Reading_the_wrong_side_throws()
    {
        Assert.Throws<InvalidOperationException>(() => Refused.Value);
        Assert.Throws<InvalidOperationException>(() => Twelve.Error);
    }

    // Map changes a value and passes an error on untouched, so a chain stops at its first failure.
    [Fact]
    public void Map_changes_a_value_and_passes_an_error_on()
    {
        Assert.Equal(24, Twelve.Map(n => n * 2).Value);
        Assert.Equal("refused", Refused.Map(n => n * 2).Error);
    }

    [Fact]
    public void Match_turns_either_side_into_one_value() =>
        Assert.Equal(("12", "no: refused"),
            (Twelve.Match(n => $"{n}", e => $"no: {e}"), Refused.Match(n => $"{n}", e => $"no: {e}")));

    [Fact]
    public void GetValueOrDefault_falls_back_on_a_failure() =>
        Assert.Equal((12, 0), (Twelve.GetValueOrDefault(0), Refused.GetValueOrDefault(0)));

    // Two Results that hold the same thing are equal, as two records with the same fields are.
    [Fact]
    public void Equal_contents_make_equal_results() =>
        Assert.Equal(Result.Success<int, string>(12), Twelve);
}
