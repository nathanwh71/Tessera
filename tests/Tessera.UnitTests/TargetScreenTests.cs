using Tessera.Http;

namespace Tessera.UnitTests;

/// <summary>S-06: the screen refuses disguised targets and lets clean ones through unchanged.</summary>
public sealed class TargetScreenTests
{
    [Theory]
    [InlineData("/food.cafes/%2e%2e/1/2/3.pbf", "a dot segment")]
    [InlineData("/food.cafes/../1/2/3.pbf", "a dot segment")]
    [InlineData("/food.cafes/%2E/1/2/3.pbf", "a dot segment")]
    [InlineData("/food.cafes%2F15/1/2.pbf", "an encoded slash")]
    [InlineData("/food.cafes\\15/1/2.pbf", "a backslash")]
    [InlineData("/food.cafes%5c15/1/2.pbf", "a backslash")]
    [InlineData("/food.cafes/15/1/2.pbf%", "a '%' without two hexadecimal digits")]
    [InlineData("/food.cafes/15/1/2%zz.pbf", "a '%' without two hexadecimal digits")]
    [InlineData("/food.cafes/15/1/2.pbf%00", "an encoded control character")]
    [InlineData("/food.cafes/15/1/\t2.pbf", "a control character")]
    [InlineData("/food.c%61fes/15/1/2.pbf", "an encoded plain character")]
    [InlineData("/food%2Ecafes/15/1/2.pbf", "an encoded plain character")]
    [InlineData("/food.cafes/15/1/2%2Epbf", "an encoded plain character")]
    [InlineData("/food.cafes/%31/1/2.pbf", "an encoded plain character")]
    [InlineData("/food.cafes/15/1/2.pbf%7E", "an encoded plain character")]
    public void Refuses_a_malformed_target(string target, string problem) =>
        Assert.Equal(problem, TargetScreen.Problem(target).OrElse("clean"));

    [Theory]
    [InlineData("/food.cafes/15/27154/14280.pbf")]
    [InlineData("/food.cafes/0/0/0.mvt")]
    [InlineData("/food.cafes.html")]
    [InlineData("/")]
    [InlineData("/odd%20%22name%22/0/0/0.pbf")]
    [InlineData("/caf%C3%A9/0/0/0.pbf")]
    [InlineData("/food.cafes/0/0/0.pbf?x=%61")]
    public void Lets_a_clean_target_through(string target) =>
        Assert.Equal("clean", TargetScreen.Problem(target).OrElse("clean"));
}
