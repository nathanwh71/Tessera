namespace Tessera.Settings;

/// <summary>
/// The names of the environment variables the service and the import read (ADR 0001, ADR 0003).
/// They belong to the Settings module: <see cref="SettingsLoader"/> and <see cref="SettingsReader"/>
/// look each one up when the program starts.
/// </summary>
/// <remarks>
/// An environment variable is a name and a value the operating system hands to a program when it
/// starts. Machines, ports and passwords live there rather than in code or in tessera.json, so the
/// same build runs on any machine, and no password ever sits in Git, the store of the code's
/// history (US-00 c4).
/// Several names follow pg_tileserv, an older tile server whose setting names Tessera copies, so an
/// operator who knows one knows the other.
/// </remarks>
public static class EnvironmentNames
{
    /// <summary>The path of the settings file, when it is not tessera.json beside the program.</summary>
    public const string Config = "TESSERA_CONFIG";
    /// <summary>
    /// The port the service answers on; it overrides <c>HttpPort</c>, as pg_tileserv's <c>TS_HTTPPORT</c>
    /// does.
    /// </summary>
    public const string HttpPort = "TESSERA_HTTPPORT";
    /// <summary>
    /// The database URL the service signs in with to read tiles; pg_tileserv uses the same name.
    /// </summary>
    public const string ReaderDatabase = "DATABASE_URL";
    /// <summary>The database URL the import signs in with to write places.</summary>
    public const string PublisherDatabase = "TESSERA_PUBLISHER_DATABASE_URL";

    /// <summary>
    /// A variable that starts with this and names a key, in any mix of capitals and small letters,
    /// overrides that key in tessera.json: <c>TESSERA_CACHETTL</c> overrides <c>CacheTTL</c>, as
    /// pg_tileserv's <c>TS_</c> does. '__' stands for a step down a level, as in
    /// <c>TESSERA_AREA__HOMELEVEL</c> (ADR 0003).
    /// </summary>
    public const string OverridePrefix = "TESSERA_";
}
