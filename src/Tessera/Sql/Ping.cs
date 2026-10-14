namespace Tessera.Sql;

/// <summary>
/// What one ping of the database found (N-28), as in the Java build, Tessera's twin written in Java.
/// A ping is a tiny question sent only to learn whether the other side answers. This belongs to the
/// Sql module: <see cref="Database"/> sends each ping, and <see cref="DatabaseWatch"/> and
/// <see cref="WatchState"/> judge the answer.
/// </summary>
/// <remarks>
/// It is an enum, a type whose value must be one of a fixed list of names.
/// </remarks>
public enum Ping
{
    /// <summary>
    /// The database answered <c>SELECT 1</c>, a query that asks for the number 1, within a second.
    /// </summary>
    Answered,

    /// <summary>
    /// The connection opened, but no answer came within a second. The database may only be busy, so
    /// one slow ping alone is no proof it is down.
    /// </summary>
    Slow,

    /// <summary>No connection could be opened, or none within a second: proof the database is down.</summary>
    Refused,
}
