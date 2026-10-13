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
    public const string HttpPort = "TESSERA_HTTP_PORT";
    public const string ReaderDatabase = "DATABASE_URL";
    public const string PublisherDatabase = "TESSERA_PUBLISHER_DATABASE_URL";

    /// <summary>
    /// Variables that start with this and hold "__", such as <c>TESSERA_Tiles__Buffer</c>, override
    /// a key in tessera.json; "__" stands for a step down a level (ADR 0003).
    /// </summary>
    public const string OverridePrefix = "TESSERA_";
}
