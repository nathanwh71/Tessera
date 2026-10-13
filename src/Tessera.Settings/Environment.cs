namespace Tessera.Settings;

/// <summary>
/// The names of the environment variables the service and the import read (ADR 0001, ADR 0003).
/// </summary>
/// <remarks>
/// An environment variable is a name and a value the operating system hands to a program when it
/// starts. Machines, ports and passwords live there rather than in code or in tessera.json, so the
/// same build runs on any machine, and no password ever sits in Git (US-00 c4).
/// </remarks>
public static class EnvironmentNames
{
    public const string Config = "TESSERA_CONFIG";
    /// <summary>The port the service answers on; it overrides <c>HttpPort</c>, as pg_tileserv's <c>TS_HTTPPORT</c> does.</summary>
    public const string HttpPort = "TESSERA_HTTPPORT";
    public const string ReaderDatabase = "DATABASE_URL";
    public const string PublisherDatabase = "TESSERA_PUBLISHER_DATABASE_URL";

    /// <summary>
    /// A variable that starts with this and names a key, in any case, overrides that key in
    /// tessera.json: <c>TESSERA_CACHETTL</c> overrides <c>CacheTTL</c>, as pg_tileserv's <c>TS_</c>
    /// does. "__" stands for a step down a level, as in <c>TESSERA_AREA__HOME__ZOOM</c> (ADR 0003).
    /// </summary>
    public const string OverridePrefix = "TESSERA_";
}
