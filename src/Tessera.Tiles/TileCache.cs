using System.Security.Cryptography;
using Tessera.Kernel;
using Tessera.Settings;

namespace Tessera.Tiles;

/// <summary>
/// The label a browser keeps with a tile, so it can ask 'is mine still good?' (N-19).
/// </summary>
/// <remarks>
/// An ETag is a short text made from the tile's bytes, here a hash: a fixed-length fingerprint
/// that changes whenever one byte changes. The same bytes always give the same tag, on any machine
/// and after a restart, so the browser's copy and the service's agree without a word more. The tag
/// is weak, <c>W/"…"</c>, because the packed form N-20 sends is the same tile, and HTTP lets one
/// weak tag cover both forms. SHA-256 is at hand in .NET; half of it, 128 bits, is more than enough
/// to tell two tiles apart, and the tag guards freshness, not secrets.
/// </remarks>
public static class TileTag
{
    public static string Of(byte[] bytes) => $"W/\"{Convert.ToHexStringLower(SHA256.HashData(bytes).AsSpan(0, 16))}\"";

    /// <summary>
    /// True when the browser's <c>If-None-Match</c> names this tag. The header may list several
    /// tags, each weak or strong, or be <c>*</c> for 'any'; a weak comparison ignores the <c>W/</c>.
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

/// <summary>One tile's place in the grid of one layer: the key a kept copy is found by.</summary>
public readonly record struct TileKey(string LayerId, int Z, int X, int Y);

/// <summary>A tile the service built: its bytes, its tag, and when it was built.</summary>
public sealed record KeptTile(byte[] Bytes, string Tag, DateTimeOffset Built);

/// <summary>
/// The tiles the service has built, kept in memory so a second request costs no query (N-19 c3).
/// </summary>
/// <remarks>
/// This is the one object in the Tiles module that changes over time, so it sits behind a lock
/// (ADR 0004: state lives at an edge, not in the pure core). A copy lives for CacheSeconds, the
/// same time a browser may keep its own, so a new import shows everywhere within that time
/// (N-19 c6). The copies together take no more than CacheMegabytes; when the room is full, the
/// copy used longest ago goes first (N-19 c5). A list ordered by use, with the newest at the
/// front, makes both finding the oldest and moving one to the front a step of fixed cost.
/// </remarks>
public sealed class TileCache
{
    private readonly object _gate = new();
    private readonly Dictionary<TileKey, LinkedListNode<(TileKey Key, KeptTile Tile)>> _byKey = new();
    private readonly LinkedList<(TileKey Key, KeptTile Tile)> _byUse = new();
    private readonly TimeSpan _life;
    private readonly long _room;
    private readonly TimeProvider _clock;

    public TileCache(TileSettings settings, TimeProvider clock)
    {
        _life = TimeSpan.FromSeconds(settings.CacheSeconds);
        _room = settings.CacheMegabytes * 1024L * 1024L;
        _clock = clock;
    }

    /// <summary>The bytes the kept copies take together, for the tests of the cap.</summary>
    public long Bytes { get; private set; }

    public DateTimeOffset Now => _clock.GetUtcNow();

    /// <summary>A fresh copy of the tile, if one is kept; a copy past its life is dropped instead.</summary>
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
    /// Keeps a tile just built. With CacheSeconds at 0 nothing is kept (N-19 c4), and a tile larger
    /// than the whole room is not kept either, since it would push every other copy out.
    /// </summary>
    public void Keep(TileKey key, KeptTile tile)
    {
        if (_life <= TimeSpan.Zero || tile.Bytes.LongLength > _room)
            return;
        lock (_gate)
        {
            if (_byKey.TryGetValue(key, out var old))
                Drop(old);
            while (Bytes + tile.Bytes.LongLength > _room && _byUse.Last is { } oldest)
                Drop(oldest);
            var node = new LinkedListNode<(TileKey, KeptTile)>((key, tile));
            _byUse.AddFirst(node);
            _byKey[key] = node;
            Bytes += tile.Bytes.LongLength;
        }
    }

    private void Drop(LinkedListNode<(TileKey Key, KeptTile Tile)> node)
    {
        _byUse.Remove(node);
        _byKey.Remove(node.Value.Key);
        Bytes -= node.Value.Tile.Bytes.LongLength;
    }
}
