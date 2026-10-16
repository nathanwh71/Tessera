using System.Reflection;
using ExhaustiveMatching;

namespace Tessera.Etl.Tests;

/// <summary>
/// The cases of <see cref="Verdict"/>. The ExhaustiveMatching analyser checks that each switch
/// handles every case [Closed] names, but it misses a record that derives from Verdict without
/// being named there, so this test makes the list and the records agree.
/// </summary>
public sealed class VerdictTests
{
    [Fact]
    public void Lists_every_record_that_derives_from_Verdict_as_a_case()
    {
        var named = typeof(Verdict).GetCustomAttribute<ClosedAttribute>()!.Cases;
        var derived = typeof(Verdict).Assembly.GetTypes().Where(type => type.BaseType == typeof(Verdict));

        Assert.Equal(named.OrderBy(type => type.Name), derived.OrderBy(type => type.Name));
    }
}
