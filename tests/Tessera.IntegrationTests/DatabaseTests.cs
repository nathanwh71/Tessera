using System.Security.Cryptography;
using Tessera.Catalogue;
using Tessera.Settings;
using Tessera.Sql;

namespace Tessera.IntegrationTests;

/// <summary>US-01: one packaged database, its shipped roles, and the seed the release carries.</summary>
public sealed class DatabaseTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    // US-01 test 1: the roles come from the shipped script, so the service signs in as the real one.
    [Fact]
    public async Task Signs_in_as_the_shipped_reader_role()
    {
        await using var reader = Database.Open(database.Reader);
        var catalogue = await LayerCatalogue.Load(reader, TestContext.Current.CancellationToken);

        Assert.True(catalogue.Match(found => found.Layers.IsEmpty, _ => false));
    }

    // The reader is read-only even where a grant would let it write: a second lock beside the grants.
    [Fact]
    public async Task Keeps_the_reader_read_only() =>
        Assert.Contains(
            "default_transaction_read_only=on",
            await database.OwnerValue<string[]>(
                "SELECT setconfig FROM pg_db_role_setting s JOIN pg_roles r ON r.oid = s.setrole WHERE r.rolname = 'tessera_reader'"));

    // US-01 test 2: the release carries the seed byte for byte as nathanwh71/JTessera holds it.
    [Fact]
    public void Ships_the_seed_from_jtessera()
    {
        var seed = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "data", "macau-pois.geojson"));

        // SHA-256 turns a file into a 64-digit fingerprint; one changed byte changes the fingerprint.
        // This one is the fingerprint of data/macau-pois.geojson in nathanwh71/JTessera on 13 October 2026.
        Assert.Equal("d9319335cc40dd25ecc8782c4cf9041cf2be53bdebd0aaa8637991b5e050e2f3", Convert.ToHexStringLower(SHA256.HashData(seed)));
        var places = Tessera.Import.Seed.Read(System.Text.Encoding.UTF8.GetString(seed), new AreaBox(113.52, 22.10, 113.61, 22.22))
            .Match(found => found, error => throw new Xunit.Sdk.XunitException(error.Message));
        Assert.Equal(900, places.Length);
        Assert.All(places.GroupBy(place => place.Layer), layer => Assert.Equal(100, layer.Count()));
    }
}
