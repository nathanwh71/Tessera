using CSharpFunctionalExtensions;

namespace Modules.Tessera.Domain;

/// <summary>
/// One <b><i>tile</i></b>'s address: zoom level z, column x and row y. Level 0 is one square of the
/// world; each level splits every square into four, so level z has 2^z columns and 2^z rows.
/// </summary>
public sealed record TileAddress(int Z, int X, int Y)
{
    /// <summary>
    /// The address, or none when no such tile exists: z outside the levels served, or x and y outside
    /// that level's grid. <c>z &gt; 30</c> keeps <c>1 &lt;&lt; z</c> inside a 32-bit number.
    /// </summary>
    public static Maybe<TileAddress> Of(int z, int x, int y, int minZoom = 0, int maxZoom = 22) =>
        z < minZoom || z > maxZoom || z > 30 || x < 0 || y < 0 || x >= 1 << z || y >= 1 << z
            ? Maybe<TileAddress>.None
            : new TileAddress(z, x, y);
}
