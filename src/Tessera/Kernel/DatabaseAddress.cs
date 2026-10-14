namespace Tessera.Kernel;

/// <summary>
/// Where a database lives and who signs in to it, read from a URL such as
/// <c>postgresql://tessera_reader:secret@db:5432/tessera</c> (ADR 0001). It is a record, a bundle of
/// named values that cannot change once made.
/// </summary>
/// <remarks>
/// The Settings module reads it and the Sql module uses it, but neither module may refer to the
/// other, so it lives here in Kernel, the small module every other one shares.
/// The password stays in memory alone. <see cref="ToString"/>, the method that turns a value into
/// text, prints stars in its place, so a log line, a line the program writes to record what
/// happened, never shows it.
/// </remarks>
public sealed record DatabaseAddress(string Host, int Port, string Database, string User, string Password)
{
    public override string ToString() => $"postgresql://{User}:***@{Host}:{Port}/{Database}";
}
