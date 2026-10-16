namespace Modules.Tessera.Domain;

/// <summary>
/// One <b><i>tile</i></b>'s address: zoom level z, column x and row y. Level 0 is one square of the
/// world; each level splits every square into four, so level z has 2^z columns and 2^z rows.
/// </summary>
/// <remarks>
/// A <b><i>smart constructor</i></b>: the constructor is private, so <see cref="Of"/> is the one way to
/// make an address, and every address that exists lies on the grid. The properties have no
/// <c>init</c>, so a <c>with</c> expression cannot copy one off the grid either.
/// </remarks>
public sealed record TileAddress
{
    public int Z { get; }

    public int X { get; }

    public int Y { get; }

    private TileAddress(int z, int x, int y) => (Z, X, Y) = (z, x, y);

    /// <summary>
    /// The address, or null when no such tile exists: z outside the levels served, or x and y outside
    /// that level's grid. <c>z &gt; 30</c> keeps <c>1 &lt;&lt; z</c> inside a 32-bit number.
    /// </summary>
    public static TileAddress? Of(int z, int x, int y, int minZoom = 0, int maxZoom = 22) =>
        z < minZoom || z > maxZoom || z > 30 || x < 0 || y < 0 || x >= 1 << z || y >= 1 << z
            ? null
            : new TileAddress(z, x, y);
}
