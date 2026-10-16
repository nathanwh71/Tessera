using System.Collections.Immutable;

// The tables and views the service may publish as map tiles (US-03). The database keeps the guest
// list; the Infrastructure reads it (Catalogue.cs) and builds these values from it.

namespace Modules.Tessera.Domain;

/// <summary>
/// One table or view the service may serve as map <b><i>tiles</i></b>: a <b><i>layer</i></b> (US-03).
/// Every name here, but the id, is a <see cref="SqlName"/>, quoted, so it can go into a query's text as it is.
/// </summary>
/// <param name="Id">
/// The layer's id: its <b><i>schema</i></b>, the named folder that holds the table, and its name, such
/// as <c>food.cafes</c>.
/// </param>
/// <param name="Table">The quoted table, such as <c>"food"."cafes"</c>.</param>
/// <param name="Geometry">The quoted column that holds each place's shape.</param>
/// <param name="Srid">
/// The layer's <b><i>SRID</i></b>, the number of its coordinate system: 4326 for degrees, 3857 for
/// web-map metres.
/// </param>
/// <param name="GeometryType">The kind of shape, such as POINT.</param>
/// <param name="Properties">The quoted columns each tile carries beside the shape (US-07).</param>
/// <param name="Order">The quoted primary key, which fixes the order of features; empty for a view.</param>
public sealed record Layer(
    string Id, SqlName Table, SqlName Geometry, int Srid, string GeometryType,
    ImmutableArray<SqlName> Properties, ImmutableArray<SqlName> Order);

/// <summary>
/// The <b><i>catalogue</i></b>: the list of layers at one moment. Once built it never changes, so a
/// request can read it safely while a refresh builds the next one. The ordinal comparer the
/// Infrastructure builds it with matches ids letter for letter, case included, as <see cref="Find"/>
/// promises.
/// </summary>
public sealed record LayerCatalogue(ImmutableDictionary<string, Layer> Layers)
{
    /// <summary>
    /// The layer with this id, or null when there is none; the compiler makes the caller check
    /// which. The match is exact and case-sensitive, as the database treats quoted
    /// names: <c>food.cafes</c> is a layer, <c>Food.Cafes</c> is not.
    /// </summary>
    public Layer? Find(string id) => Layers.GetValueOrDefault(id);
}
