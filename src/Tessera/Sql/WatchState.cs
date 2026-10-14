namespace Tessera.Sql;

/// <summary>
/// What the service believes about the database: up or down, and how many slow pings in a row it
/// has seen (N-28, ADR 0006 rule 6). It belongs to the Sql module; <see cref="DatabaseWatch"/> keeps
/// the current state and asks this record for the next one after each ping. The rules match the Java
/// build's, line for line.
/// </summary>
/// <remarks>
/// <para>
/// A refused connection is proof, so the database is down at once. One slow ping is not, since a busy
/// database may miss one, so it takes two in a row. One answer is enough to say up again.
/// </para>
/// <para>
/// It is a record, a bundle of named values that cannot change once made, and it is pure (ADR 0004):
/// <see cref="After"/> returns a new state and changes nothing, so tests check the rules with values
/// alone and need no database.
/// </para>
/// </remarks>
public sealed record WatchState(bool Up, int Misses)
{
    /// <summary>Slow pings in a row that mean the database is down rather than busy.</summary>
    public const int MissesForDown = 2;

    /// <summary>The service starts trusting the database; the first ping, made at once, corrects it.</summary>
    public static readonly WatchState Start = new(true, 0);

    /// <summary>
    /// The state after one more ping. An answer clears the count of misses; a refusal says down and
    /// leaves the count alone; a slow ping adds one to the count, and says down when the count reaches
    /// <see cref="MissesForDown"/> or the database was down already.
    /// </summary>
    public WatchState After(Ping ping) => ping switch
    {
        Ping.Answered => new WatchState(true, 0),
        // 'with' makes a copy with the named values changed; the old state stays as it was.
        Ping.Refused => this with { Up = false },
        Ping.Slow => new WatchState(Up && Misses + 1 < MissesForDown, Misses + 1),
        _ => throw new ArgumentOutOfRangeException(nameof(ping)),
    };

    /// <summary>The state after a query lost its connection: proof, like a refused ping.</summary>
    public WatchState AfterLostConnection() => this with { Up = false };
}
