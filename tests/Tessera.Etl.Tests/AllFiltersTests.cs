using System.Reflection;
using Tessera.Etl.Filters;

namespace Tessera.Etl.Tests;

/// <summary>
/// The compiler checks each <b><i>filter</i></b>'s own file, but not the one line that lists it in
/// Filters/All.cs. These tests do: a filter left out would compile, yet a
/// <b><i>pipeline file</i></b> naming it would be refused.
/// </summary>
public sealed class AllFiltersTests
{
    // Every class in Tessera.Etl.Filters that declares a FilterKind named Kind is a filter.
    private static readonly IReadOnlyList<FilterKind> Declared =
        [.. typeof(All).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(All).Namespace)
            .Select(type => type.GetField("Kind", BindingFlags.Public | BindingFlags.Static))
            .Where(field => field?.FieldType == typeof(FilterKind))
            .Select(field => (FilterKind)field!.GetValue(null)!)];

    [Fact]
    public void Lists_every_filter_declared_in_the_filters_folder()
    {
        // The eight filters of US-36. A fixed count also fails when a filter's file and its line
        // in All.cs vanish together, which the two checks below would let pass.
        Assert.Equal(8, Declared.Count);
        Assert.All(Declared, kind => Assert.Contains(kind, All.Kinds));
        Assert.Equal(Declared.Count, All.Kinds.Length);
    }

    [Fact]
    public void Gives_each_filter_a_name_of_its_own()
    {
        Assert.Equal(All.Kinds.Length, All.Kinds.Select(kind => kind.Name).Distinct().Count());
    }
}
