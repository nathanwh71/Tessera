namespace Tessera.Sql;

/// <summary>What one ping of the database found (N-28), as in the Java build.</summary>
public enum Ping
{
    /// <summary>The database answered <c>SELECT 1</c> within a second.</summary>
    Answered,

    /// <summary>The line was open, but no answer came within a second: the database may only be busy.</summary>
    Slow,

    /// <summary>No line could be opened: proof the database is down.</summary>
    Refused,
}
