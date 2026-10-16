using System.Reflection;
using ExhaustiveMatching;

namespace Tessera.Etl.Tests;

/// <summary>
/// The <b><i>unions</i></b>, the types marked [Closed]. The ExhaustiveMatching analyser checks that
/// each switch handles every case [Closed] names, but it misses a record that derives from the type
/// without being named there, so this test makes the list and the records agree.
/// </summary>
public sealed class ClosedTypeTests
{
    [Theory]
    [InlineData(typeof(Verdict))]
    [InlineData(typeof(Failure))]
    public void Lists_every_record_that_derives_from_a_union_as_a_case(Type union)
    {
        var named = union.GetCustomAttribute<ClosedAttribute>()!.Cases;
        var derived = union.Assembly.GetTypes().Where(type => type.BaseType == union);

        Assert.Equal(named.OrderBy(type => type.Name), derived.OrderBy(type => type.Name));
    }
}
