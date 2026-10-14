using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using Tessera.Kernel;
using Tessera.Settings;

// This file lets the service answer a second request for a tile without asking the database again.
// It belongs to the Tiles module and serves story N-19 (keep tiles) and N-20 (pack them smaller).
// TileServer in Tiles.cs reads and fills the cache here; TesseraService makes the one cache the
// service uses, and drops a layer's tiles when the layer leaves the list.

namespace Tessera.Tiles;

/// <summary>
/// The label a browser keeps with a tile, so it can later ask 'is mine still good?' (N-19).
/// </summary>
/// <remarks>
/// HTTP, the set of rules a browser and a web server talk by, calls this label an ETag. Here the tag
/// is made from a hash of the tile's bytes: a fixed-length fingerprint that changes whenever one
/// byte changes. The same bytes always give the same tag, on any machine and after a restart, so
/// the browser's copy and the service's agree with no further message. The tag is weak, written
/// <c>W/"…"</c>, because HTTP lets one weak tag cover two forms of the same content, and N-20 sends
/// the tile both plain and packed. SHA-256 is the hash .NET, the toolkit this program runs on,
/// offers ready-made. The tag keeps half of it, 128 bits: ample to tell two tiles apart, and the tag
/// guards freshness, not secrets.
/// </remarks>
public static class TileTag
{
    /// <summary>The weak tag for these bytes; equal bytes always give an equal tag.</summary>
    public static string Of(byte[] bytes) => $"W/\"{Convert.ToHexStringLower(SHA256.HashData(bytes).AsSpan(0, 16))}\"";

    /// <summary>
    /// True when the browser's <c>If-None-Match</c> header, a line of extra information sent with the
    /// request, names this tag. The header may list several tags, each weak or strong, or be
    /// <c>*</c> for 'any'. The comparison drops the <c>W/</c> mark from both sides, as HTTP's
    /// weak comparison does.
    /// </summary>
    public static bool Matches(string ifNoneMatch, string tag) =>
        ifNoneMatch.Trim() == "*"
        || ifNoneMatch.Split(',').Select(Strong).Contains(Strong(tag), StringComparer.Ordinal);

    private static string Strong(string tag)
    {
        var trimmed = tag.Trim();
        return trimmed.StartsWith("W/", StringComparison.Ordinal) ? trimmed[2..] : trimmed;
    }
}

/// <summary>One tile's place in the grid of one layer: the key that finds a kept copy.</summary>
public readonly record struct TileKey(string LayerId, int Z, int X, int Y);

/// <summary>
/// A tile the service built: its bytes, the same bytes packed with gzip, its tag, and the time it
/// was built. The packing happens once, here, so a kept tile never needs packing again (N-20).
/// </summary>
public sealed record KeptTile(byte[] Bytes, byte[] Packed, string Tag, DateTimeOffset Built)
{
    /// <summary>Packs and tags the bytes, stamping them with <paramref name="built"/>.</summary>
    public static KeptTile Of(byte[] bytes, DateTimeOffset built) => new(bytes, TilePack.Gzip(bytes), TileTag.Of(bytes), built);

    /// <summary>The memory the copy takes, in bytes: both forms count.</summary>
    public long Size => Bytes.LongLength + Packed.LongLength;
}

/// <summary>
/// gzip, a way of packing data smaller that every browser can unpack (N-20).
/// </summary>
public static class TilePack
{
    /// <summary>
    /// Packs the bytes with gzip. The fastest setting serves here because a tile packs once and
    /// then waits in the cache, so time saved now counts for more than a few bytes saved.
    /// </summary>
    public static byte[] Gzip(byte[] bytes)
    {
        using var packed = new MemoryStream();
        using (var zip = new GZipStream(packed, CompressionLevel.Fastest, leaveOpen: true))
            zip.Write(bytes);
        return packed.ToArray();
    }

    /// <summary>
    /// True when the browser's <c>Accept-Encoding</c> header, its list of the packings it can undo,
    /// names gzip, or <c>*</c> for any, with a weight above 0. A browser writes each entry as
    /// <c>gzip</c> or <c>gzip;q=0.5</c>, separated by commas; a weight of 0 means 'never'.
    /// </summary>
    public static bool AcceptsGzip(string acceptEncoding) =>
        acceptEncoding.Split(',')
            .Select(part => part.Split(';', 2))
            .Any(part => part[0].Trim() is "gzip" or "*" && Weight(part) > 0);

    // An entry with no weight counts as 1; a weight that will not read as a number counts as 0.
    private static double Weight(string[] part) =>
        part.Length == 1 ? 1
            : double.TryParse(part[1].Trim().Replace("q=", "", StringComparison.OrdinalIgnoreCase), NumberStyles.Float, CultureInfo.InvariantCulture, out var weight) ? weight
            : 0;
}

/// <summary>
/// The tiles the service has built, kept in memory so a second request for one costs no database
/// query (N-19 c3). A cache is such a store of answers kept ready for the next asker.
/// </summary>
/// <remarks>
/// <para>
/// Many requests run at the same moment on separate threads, lines of work the computer runs side
/// by side. This is the one object in the Tiles module that changes over time, so it sits behind a
/// lock: a gate only one thread may pass at a time, so no two change the cache at once. ADR 0004 asks
/// for this: changing state lives at an edge, outside the pure core.
/// </para>
/// <para>
/// A copy lives for CacheSeconds, the same time a browser may keep its own, so a new import shows
/// everywhere within that time (N-19 c6). All copies together take at most CacheMegabytes. When the
/// room is full, the copy used longest ago goes first (N-19 c5); this rule is called LRU, for
/// 'least recently used'. The copies sit in a chain ordered by use, newest at the front, and a table
/// finds any copy by its key. With the two, finding the oldest copy and moving a copy to the front
/// each take the same short time however many copies there are.
/// </para>
/// </remarks>
public sealed class TileCache
{
    private readonly object _gate = new();
    private readonly Dictionary<TileKey, LinkedListNode<(TileKey Key, KeptTile Tile)>> _byKey = new();
    private readonly LinkedList<(TileKey Key, KeptTile Tile)> _byUse = new();
    private readonly TimeSpan _life;
    private readonly long _room;
    private readonly TimeProvider _clock;

    /// <param name="settings">CacheSeconds sets how long a copy lives; CacheMegabytes caps the room.</param>
    /// <param name="clock">Tells the time; a test passes a clock it can move forward by hand.</param>
    public TileCache(TileSettings settings, TimeProvider clock)
    {
        _life = TimeSpan.FromSeconds(settings.CacheSeconds);
        _room = settings.CacheMegabytes * 1024L * 1024L;
        _clock = clock;
    }

    /// <summary>The bytes the kept copies take together, read by the tests of the cap.</summary>
    public long Bytes { get; private set; }

    /// <summary>The time by this cache's clock, used to stamp a tile as it is built.</summary>
    public DateTimeOffset Now => _clock.GetUtcNow();

    /// <summary>
    /// A fresh copy of the tile, if one is kept, which then counts as the most recently used. A copy
    /// past its life is dropped, and the caller gets none.
    /// </summary>
    public Option<KeptTile> Find(TileKey key)
    {
        lock (_gate)
        {
            if (!_byKey.TryGetValue(key, out var node))
                return Option.None<KeptTile>();
            if (Now - node.Value.Tile.Built >= _life)
            {
                Drop(node);
                return Option.None<KeptTile>();
            }

            _byUse.Remove(node);
            _byUse.AddFirst(node);
            return Option.Some(node.Value.Tile);
        }
    }

    /// <summary>
    /// Keeps a newly built tile, pushing out the least recently used copies until it fits. With
    /// CacheSeconds at 0 nothing is kept (N-19 c4). A tile larger than the whole room is never kept,
    /// since it would push every other copy out.
    /// </summary>
    public void Keep(TileKey key, KeptTile tile)
    {
        if (_life <= TimeSpan.Zero || tile.Size > _room)
            return;
        lock (_gate)
        {
            if (_byKey.TryGetValue(key, out var old))
                Drop(old);
            while (Bytes + tile.Size > _room && _byUse.Last is { } oldest)
                Drop(oldest);
            var node = new LinkedListNode<(TileKey, KeptTile)>((key, tile));
            _byUse.AddFirst(node);
            _byKey[key] = node;
            Bytes += tile.Size;
        }
    }

    /// <summary>
    /// Drops every kept copy of the layers named, such as layers whose grant has gone (N-23 c3).
    /// Without this, a table the database owner has withdrawn would go on serving its kept copies
    /// for up to CacheTTL seconds.
    /// </summary>
    public void DropLayers(IReadOnlyCollection<string> layerIds)
    {
        if (layerIds.Count == 0)
            return;
        lock (_gate)
        {
            foreach (var key in _byKey.Keys.Where(key => layerIds.Contains(key.LayerId)).ToList())
                Drop(_byKey[key]);
        }
    }

    // The caller must hold the lock: this changes the chain, the table and the byte count together.
    private void Drop(LinkedListNode<(TileKey Key, KeptTile Tile)> node)
    {
        _byUse.Remove(node);
        _byKey.Remove(node.Value.Key);
        Bytes -= node.Value.Tile.Size;
    }
}
