namespace Tessera.Kernel;

/// <summary>
/// Where a database lives and who signs in to it, read from a URL such as
/// <c>postgresql://tessera_reader:secret@db:5432/tessera</c> (ADR 0001).
/// </summary>
/// <remarks>
/// Settings reads it and Sql uses it, but neither module may reference the other, so it lives here.
/// The password sits in memory only; <see cref="ToString"/> hides it, so a log line never shows it.
/// </remarks>
public sealed record DatabaseAddress(string Host, int Port, string Database, string User, string Password)
{
    public override string ToString() => $"postgresql://{User}:***@{Host}:{Port}/{Database}";
}
