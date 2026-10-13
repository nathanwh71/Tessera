using FsCheck.Xunit;
using Tessera.Catalogue;
using Tessera.Settings;
using Tessera.Sql;
using Tessera.Tiles;

namespace Tessera.UnitTests;

/// <summary>N-19: the tag follows the bytes, a kept copy costs no query, and the room holds.</summary>
public sealed class TileCacheTests
{
    private static readonly LayerCatalogue Catalogue = LayerCatalogue.FromRows([new CatalogueRow("food", "cafes", "geom", 4326, "POINT", """["name"]""")]);

    // A clock the test moves by hand, so a copy's life passes without waiting.
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 14, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    // A reader that counts its queries and answers with bytes made from the query's parameters.
    private sealed class Counter
    {
        public int Queries { get; private set; }

        public Task<Result<Option<byte[]>, DatabaseError>> Read(Query query, CancellationToken cancel)
        {
            Queries++;
            return Task.FromResult(Result.Ok<Option<byte[]>, DatabaseError>(Option.Some(query.Parameters.Select(value => (byte)value.GetHashCode()).ToArray())));
        }
    }

    private static TileKey Key(int z = 15, int x = 27154, int y = 14280) => new("food.cafes", z, x, y);

    // A tile of random bytes, which gzip cannot shrink, so its size is what the test says.
    private static KeptTile Tile(int size, DateTimeOffset built)
    {
        var bytes = new byte[size];
        new Random(size).NextBytes(bytes);
        return new KeptTile(bytes, [], TileTag.Of(bytes), built);
    }

    // N-19 test 1: the tag follows the bytes alone.
    [Property]
    public bool Gives_the_same_bytes_the_same_tag(byte[] bytes) => TileTag.Of(bytes) == TileTag.Of([.. bytes]);

    [Property]
    public bool Gives_different_bytes_different_tags(byte[] first, byte[] second) =>
        first.SequenceEqual(second) || TileTag.Of(first) != TileTag.Of(second);

    [Fact]
    public void Writes_a_weak_tag() =>
        Assert.Matches("^W/\"[0-9a-f]{32}\"$", TileTag.Of([1, 2, 3]));

    [Theory]
    [InlineData("W/\"abc\"", true)]
    [InlineData("\"abc\"", true)]
    [InlineData("W/\"xyz\", W/\"abc\"", true)]
    [InlineData("*", true)]
    [InlineData("W/\"xyz\"", false)]
    [InlineData("", false)]
    public void Matches_the_browsers_tag_weakly(string ifNoneMatch, bool matches) =>
        Assert.Equal(matches, TileTag.Matches(ifNoneMatch, "W/\"abc\""));

    [Fact]
    public void Finds_a_kept_copy_until_its_life_ends()
    {
        var clock = new Clock();
        var cache = new TileCache(new TileSettings(10000, 64, 60, 64), clock);
        cache.Keep(Key(), Tile(10, clock.Now));

        clock.Now += TimeSpan.FromSeconds(59);
        Assert.True(cache.Find(Key()).Match(_ => true, () => false));
        clock.Now += TimeSpan.FromSeconds(1);
        Assert.False(cache.Find(Key()).Match(_ => true, () => false));
        Assert.Equal(0, cache.Bytes);
    }

    // N-19 c4: with CacheSeconds at 0, nothing is kept.
    [Fact]
    public void Keeps_nothing_when_cache_seconds_is_zero()
    {
        var clock = new Clock();
        var cache = new TileCache(new TileSettings(10000, 64, 0, 64), clock);
        cache.Keep(Key(), Tile(10, clock.Now));

        Assert.False(cache.Find(Key()).Match(_ => true, () => false));
    }

    // N-19 c5 and test 4: the room holds, and the copy used longest ago goes first.
    [Fact]
    public void Drops_the_copy_used_longest_ago_when_the_room_is_full()
    {
        var clock = new Clock();
        var cache = new TileCache(new TileSettings(10000, 64, 60, 1), clock);
        var third = 1024 * 1024 / 3;
        cache.Keep(Key(x: 1), Tile(third, clock.Now));
        cache.Keep(Key(x: 2), Tile(third, clock.Now));
        cache.Keep(Key(x: 3), Tile(third, clock.Now));
        cache.Find(Key(x: 1));

        cache.Keep(Key(x: 4), Tile(third, clock.Now));

        Assert.True(cache.Bytes <= 1024 * 1024);
        Assert.True(cache.Find(Key(x: 1)).Match(_ => true, () => false));
        Assert.False(cache.Find(Key(x: 2)).Match(_ => true, () => false));
        Assert.True(cache.Find(Key(x: 4)).Match(_ => true, () => false));
    }

    [Fact]
    public void Never_keeps_a_tile_larger_than_the_room()
    {
        var clock = new Clock();
        var cache = new TileCache(new TileSettings(10000, 64, 60, 1), clock);
        cache.Keep(Key(), Tile(1024 * 1024 + 1, clock.Now));

        Assert.Equal(0, cache.Bytes);
    }

    // N-19 c3 and test 3: two requests for one tile run one query; after the copy expires, two.
    [Fact]
    public async Task Runs_one_query_for_two_requests_within_the_minute()
    {
        var clock = new Clock();
        var counter = new Counter();
        var settings = new TileSettings(10000, 64, 60, 64);
        var server = new TileServer(() => Option.Some(Catalogue), counter.Read, settings, new TileCache(settings, clock));

        await server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, CancellationToken.None);
        await server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, CancellationToken.None);
        Assert.Equal(1, counter.Queries);

        clock.Now += TimeSpan.FromSeconds(60);
        await server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, CancellationToken.None);
        Assert.Equal(2, counter.Queries);
    }

    // N-19 c2: a browser that sends the tile's tag back gets 304, and still no query.
    [Fact]
    public async Task Answers_304_to_a_browser_that_holds_the_tile()
    {
        var counter = new Counter();
        var settings = new TileSettings(10000, 64, 60, 64);
        var server = new TileServer(() => Option.Some(Catalogue), counter.Read, settings, new TileCache(settings, new Clock()));

        var first = Assert.IsType<TileResult>(await server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, CancellationToken.None));
        var second = Assert.IsType<NotModifiedResult>(await server.Answer("food.cafes", 15, 27154, 14280, new TileRequest(Option.Some(first.Tag), AcceptsGzip: false), CancellationToken.None));

        Assert.Equal(first.Tag, second.Tag);
        Assert.Equal("public, max-age=60", second.CacheControl);
        Assert.Equal(1, counter.Queries);
    }

    // N-19 c4: with CacheSeconds at 0, each request runs the query, and a matching tag still answers 304.
    [Fact]
    public async Task Still_answers_304_when_nothing_is_kept()
    {
        var counter = new Counter();
        var settings = new TileSettings(10000, 64, 0, 64);
        var server = new TileServer(() => Option.Some(Catalogue), counter.Read, settings, new TileCache(settings, new Clock()));

        var first = Assert.IsType<TileResult>(await server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, CancellationToken.None));
        Assert.IsType<NotModifiedResult>(await server.Answer("food.cafes", 15, 27154, 14280, new TileRequest(Option.Some(first.Tag), AcceptsGzip: false), CancellationToken.None));

        Assert.Equal(2, counter.Queries);
        Assert.Equal("no-cache", first.CacheControl);
    }

    [Fact]
    public async Task Refuses_an_unknown_layer_before_any_query()
    {
        var counter = new Counter();
        var settings = new TileSettings(10000, 64, 60, 64);
        var server = new TileServer(() => Option.Some(Catalogue), counter.Read, settings, new TileCache(settings, new Clock()));

        var answer = await server.Answer("food.zoos", 0, 0, 0, TileRequest.Plain, CancellationToken.None);

        Assert.IsNotType<TileResult>(answer);
        Assert.Equal(0, counter.Queries);
    }

    // N-20 c1 and c2: the packed form unpacks to the plain bytes, and a browser that accepts gzip gets it.
    [Fact]
    public void Packs_and_unpacks_to_the_same_bytes()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("tessera ", 200)));
        var tile = KeptTile.Of(bytes, DateTimeOffset.UnixEpoch);

        using var unpack = new System.IO.Compression.GZipStream(new MemoryStream(tile.Packed), System.IO.Compression.CompressionMode.Decompress);
        using var plain = new MemoryStream();
        unpack.CopyTo(plain);

        Assert.Equal(bytes, plain.ToArray());
        Assert.True(tile.Packed.Length < bytes.Length / 2);
    }

    [Theory]
    [InlineData("gzip, deflate, br", true)]
    [InlineData("gzip;q=0.8, br", true)]
    [InlineData("*", true)]
    [InlineData("br", false)]
    [InlineData("gzip;q=0", false)]
    [InlineData("", false)]
    public void Reads_whether_the_browser_accepts_gzip(string header, bool accepts) =>
        Assert.Equal(accepts, TilePack.AcceptsGzip(header));

    [Fact]
    public async Task Sends_the_packed_form_to_a_browser_that_accepts_it()
    {
        var counter = new Counter();
        var settings = new TileSettings(10000, 64, 60, 64);
        var server = new TileServer(() => Option.Some(Catalogue), counter.Read, settings, new TileCache(settings, new Clock()));

        var packed = Assert.IsType<TileResult>(await server.Answer("food.cafes", 15, 27154, 14280, new TileRequest(Option.None<string>(), AcceptsGzip: true), CancellationToken.None));
        var plain = Assert.IsType<TileResult>(await server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, CancellationToken.None));

        Assert.True(packed.Packed);
        Assert.False(plain.Packed);
        Assert.Equal(packed.Tag, plain.Tag);
        Assert.Equal(1, counter.Queries);
    }

    // N-22: which failures ask the browser to wait, and which are faults.
    [Theory]
    [InlineData("08006", true)]
    [InlineData("08001", true)]
    [InlineData("57014", true)]
    [InlineData("57P01", true)]
    [InlineData("57P03", true)]
    [InlineData("53300", true)]
    [InlineData("42501", false)]
    [InlineData("23505", false)]
    [InlineData("XX000", false)]
    public void Tells_a_wait_from_a_fault(string sqlState, bool unavailable) =>
        Assert.Equal(unavailable, new DatabaseError(sqlState, "").IsUnavailable);

    // N-22 c3: a failure is never kept, so the next request asks the database again.
    [Fact]
    public async Task Keeps_no_failure()
    {
        var queries = 0;
        var settings = new TileSettings(10000, 64, 60, 64);
        var server = new TileServer(() => Option.Some(Catalogue), (_, _) =>
        {
            queries++;
            return Task.FromResult(Result.Refuse<Option<byte[]>, DatabaseError>(new DatabaseError("08006", "down")));
        }, settings, new TileCache(settings, new Clock()));

        var first = await server.Answer("food.cafes", 0, 0, 0, TileRequest.Plain, CancellationToken.None);
        await server.Answer("food.cafes", 0, 0, 0, TileRequest.Plain, CancellationToken.None);

        Assert.IsNotType<TileResult>(first);
        Assert.Equal(2, queries);
    }

    // N-23 c4: before the first list of layers loads, a tile answers 503 and runs no query.
    [Fact]
    public async Task Asks_the_browser_to_wait_before_the_list_loads()
    {
        var counter = new Counter();
        var settings = new TileSettings(10000, 64, 60, 64);
        var server = new TileServer(() => Option.None<LayerCatalogue>(), counter.Read, settings, new TileCache(settings, new Clock()));

        var answer = await server.Answer("food.cafes", 0, 0, 0, TileRequest.Plain, CancellationToken.None);

        Assert.IsNotType<TileResult>(answer);
        Assert.Equal(0, counter.Queries);
    }

    // N-23 c3: a layer that leaves the list takes its kept tiles with it, and no other layer's.
    [Fact]
    public void Drops_the_kept_tiles_of_a_layer_that_left()
    {
        var clock = new Clock();
        var cache = new TileCache(new TileSettings(10000, 64, 60, 64), clock);
        cache.Keep(Key(), Tile(10, clock.Now));
        cache.Keep(new TileKey("food.markets", 15, 27154, 14280), Tile(10, clock.Now));

        cache.DropLayers(["food.cafes"]);

        Assert.False(cache.Find(Key()).Match(_ => true, () => false));
        Assert.True(cache.Find(new TileKey("food.markets", 15, 27154, 14280)).Match(_ => true, () => false));
    }

    // A reader that counts its queries and holds each one until the test lets it finish.
    private sealed class Gate
    {
        private readonly TaskCompletionSource _open = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _queries;

        public int Queries => Volatile.Read(ref _queries);

        public void Open() => _open.SetResult();

        public async Task<Result<Option<byte[]>, DatabaseError>> Read(Query query, CancellationToken cancel)
        {
            Interlocked.Increment(ref _queries);
            await _open.Task.WaitAsync(cancel);
            return Result.Ok<Option<byte[]>, DatabaseError>(Option.Some(new byte[] { 1, 2, 3 }));
        }
    }

    private static async Task Until(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200 && !condition(); attempt++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
    }

    // N-24 c1 and test 1: a hundred requests for one unbuilt tile run one query and all get the tile.
    [Fact]
    public async Task Runs_one_query_for_a_crowd_asking_at_once()
    {
        var gate = new Gate();
        var settings = new TileSettings(10000, 64, 60, 64);
        var server = new TileServer(() => Option.Some(Catalogue), gate.Read, settings, new TileCache(settings, new Clock()));

        var crowd = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, CancellationToken.None)))
            .ToArray();
        await Until(() => gate.Queries > 0);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        gate.Open();
        var answers = await Task.WhenAll(crowd);

        Assert.Equal(1, gate.Queries);
        Assert.All(answers, answer => Assert.IsType<TileResult>(answer));
    }

    // N-24 c2: one browser that leaves stops waiting, and the rest still get the tile from the one query.
    [Fact]
    public async Task Lets_one_browser_leave_without_cancelling_the_query_for_the_rest()
    {
        var gate = new Gate();
        var settings = new TileSettings(10000, 64, 60, 64);
        var server = new TileServer(() => Option.Some(Catalogue), gate.Read, settings, new TileCache(settings, new Clock()));
        using var leaving = new CancellationTokenSource();

        var first = server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, leaving.Token);
        await Until(() => gate.Queries > 0);
        var second = server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, CancellationToken.None);
        await leaving.CancelAsync();
        gate.Open();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.IsType<TileResult>(await second);
        Assert.Equal(1, gate.Queries);
    }

    // N-24 c3: with nothing kept (CacheTTL 0), a request after the query ends starts a new one.
    [Fact]
    public async Task Starts_a_new_query_once_the_last_has_ended()
    {
        var counter = new Counter();
        var settings = new TileSettings(10000, 64, 0, 64);
        var server = new TileServer(() => Option.Some(Catalogue), counter.Read, settings, new TileCache(settings, new Clock()));

        await server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, CancellationToken.None);
        await server.Answer("food.cafes", 15, 27154, 14280, TileRequest.Plain, CancellationToken.None);

        Assert.Equal(2, counter.Queries);
    }
}
