using Common.Api;

namespace Tessera.UnitTests;

/// <summary>
/// S-06 c1 and c3, SR-09 and SR-11: the rule that spots a request target in disguise, read as the
/// browser sent it, before the web server tidies it.
/// </summary>
public sealed class TargetTests
{
    // '%2e' is a dot written as its number. A folder of one or two dots, written so in part or in
    // whole, climbs the tree once the server decodes it: '/x/%2e%2e/food.cafes' becomes
    // '/food.cafes'. Case does not matter to the decoder, so it does not matter here.
    [Theory]
    [InlineData("/x/%2e%2e/food.cafes/0/0/0.pbf")]
    [InlineData("/x/%2E%2E/food.cafes/0/0/0.pbf")]
    [InlineData("/x/%2e./food.cafes/0/0/0.pbf")]
    [InlineData("/x/.%2E/food.cafes/0/0/0.pbf")]
    [InlineData("/food.cafes/%2e/0/0/0.pbf")]
    [InlineData("/x/%2e%2e")]
    [InlineData("/x/%2e%2e?layer=food.cafes")]
    public void Spots_a_dot_folder_in_disguise(string target) =>
        Assert.True(Targets.IsDisguised(target));

    // S-06 c1: a raw control character, such as byte 1 or DEL, has no place in an address.
    [Theory]
    [InlineData("/food.cafes/0/0/0.pbf\u0001")]
    [InlineData("/food\u001fcafes/0/0/0.pbf")]
    [InlineData("/food.cafes/0/0/\u007f0.pbf")]
    public void Spots_a_raw_control_character(string target) =>
        Assert.True(Targets.IsDisguised(target));

    // S-06 c4: a clean address passes, and so does an encoded dot inside a longer name, which
    // climbs nowhere. A query string may hold a dot folder, since it never names a file.
    [Theory]
    [InlineData("/food.cafes/0/0/0.pbf")]
    [InlineData("/food%2ecafes/0/0/0.pbf")]
    [InlineData("/x/%2e%2ex/food.cafes")]
    [InlineData("/x/..%2e%2e../food.cafes")]
    [InlineData("/")]
    [InlineData("/health?next=/%2e%2e/")]
    [InlineData("*")]
    public void Lets_a_clean_target_pass(string target) =>
        Assert.False(Targets.IsDisguised(target));
}
