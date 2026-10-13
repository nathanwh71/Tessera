using FsCheck.Xunit;

namespace Tessera.UnitTests;

/// <summary>
/// The laws Option and Result must keep (ADR 0004). A property test states a rule that must hold for
/// every input, and FsCheck tries a hundred random inputs to find one that breaks it.
/// </summary>
public sealed class KernelTests
{
    // Identity law: mapping "do nothing" changes nothing.
    [Property]
    public bool Option_map_identity(int? value) =>
        value.ToOption().Map(found => found) == value.ToOption();

    // Composition law: mapping f then g equals mapping "g after f" once.
    [Property]
    public bool Option_map_composes(int? value) =>
        value.ToOption().Map(found => found + 1).Map(found => found * 2)
        == value.ToOption().Map(found => (found + 1) * 2);

    // Bind with Some changes nothing: a step that only wraps its value adds nothing to a chain.
    [Property]
    public bool Option_bind_some_is_identity(int? value) =>
        value.ToOption().Bind(Option.Some) == value.ToOption();

    [Property]
    public bool Result_map_identity(int value, bool refused)
    {
        var result = refused ? Result.Refuse<int, string>("no") : Result.Ok<int, string>(value);
        return result.Map(found => found) == result;
    }

    [Property]
    public bool Result_keeps_the_first_refusal(int value)
    {
        var chain =
            from a in Result.Ok<int, string>(value)
            from b in Result.Refuse<int, string>("first")
            from c in Result.Refuse<int, string>("second")
            select a + b + c;
        return chain == Result.Refuse<int, string>("first");
    }

    [Fact]
    public void Traverse_stops_at_the_first_refusal() =>
        Assert.Equal(
            "2 is even",
            new[] { 1, 2, 3, 4 }
                .Traverse(number => number % 2 == 0 ? Result.Refuse<int, string>($"{number} is even") : Result.Ok<int, string>(number))
                .Match(_ => "no refusal", error => error));

    [Fact]
    public void Traverse_keeps_every_value_in_order() =>
        Assert.Equal(
            [1, 3, 5],
            new[] { 1, 3, 5 }.Traverse(Result.Ok<int, string>).Match(values => values.ToArray(), _ => []));
}
