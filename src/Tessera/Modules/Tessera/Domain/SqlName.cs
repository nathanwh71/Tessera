namespace Modules.Tessera.Domain;

/// <summary>
/// A table, schema or column name, quoted so the database reads it as one name and never as part of a
/// command (S-01 c3): <c>x"; DROP TABLE cafes</c> stays one odd name, and <b><i>SQL injection</i></b>,
/// SQL hidden in a name, finds no way in.
/// </summary>
/// <remarks>
/// In F# a <b><i>single-case union</i></b>, a type that wraps one value, such as
/// <c>SqlName of string</c>, would guard the quoting. Here a private constructor guards it: only
/// <see cref="Quote"/> and <see cref="Qualified"/> can make one, so a name reaches a query's text only
/// quoted, and the compiler keeps the rule.
/// </remarks>
public sealed record SqlName
{
    /// <summary>The name as it goes into a query's text, quotes included.</summary>
    public string Sql { get; }

    private SqlName(string sql) => Sql = sql;

    /// <summary>Wraps the name in double quotes and doubles any quote inside.</summary>
    public static SqlName Quote(string name) => new($"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"");

    /// <summary>A table with its <b><i>schema</i></b>, the named folder it lives in: <c>"food"."cafes"</c>.</summary>
    public static SqlName Qualified(string schema, string table) => new($"{Quote(schema).Sql}.{Quote(table).Sql}");

    public override string ToString() => Sql;
}
