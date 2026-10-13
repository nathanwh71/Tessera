using Tessera.Kernel;

namespace Tessera.Sql;

/// <summary>
/// A table, schema or column name, wrapped in double quotes so the database reads it as a name and
/// never as part of a command (US-03, S-02).
/// </summary>
/// <remarks>
/// SQL injection is an attack that hides SQL inside text a user sends, in the hope that the database
/// runs it; you meet it at school under network security. A name such as <c>x; DROP TABLE cafes</c>
/// would end one command and start another. Inside double quotes the database reads it as one odd
/// name instead. Doubling each quote inside the name stops a name such as <c>odd"name</c> from
/// closing its own quotes early.
/// The constructor is private, so <see cref="Quote"/> is the one way to make a name, and every name
/// that reaches SQL has passed through it.
/// </remarks>
public sealed record QuotedName
{
    private QuotedName(string text, string raw) => (Text, Raw) = (text, raw);

    /// <summary>The name as SQL text, quotes included: <c>"odd ""name"""</c>.</summary>
    public string Text { get; }

    /// <summary>The name as the database stores it, without quotes: <c>odd "name"</c>.</summary>
    public string Raw { get; }

    /// <summary>Wraps a name in double quotes and doubles every quote inside it; refuses an empty name.</summary>
    public static Result<QuotedName, string> Quote(string name) =>
        name.Length == 0
            ? Result.Refuse<QuotedName, string>("a name may not be empty")
            : Result.Ok<QuotedName, string>(new QuotedName($"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"", name));

    public override string ToString() => Text;
}

/// <summary>A table's full name, its schema and its own name: <c>"food"."cafes"</c>.</summary>
public sealed record TableName(QuotedName Schema, QuotedName Table)
{
    /// <summary>The id the stories use for a layer: <c>food.cafes</c>, without quotes.</summary>
    public string Id => $"{Schema.Raw}.{Table.Raw}";

    public override string ToString() => $"{Schema.Text}.{Table.Text}";

    /// <summary>Quotes both parts; refuses if either is empty.</summary>
    public static Result<TableName, string> Of(string schema, string table) =>
        from quotedSchema in QuotedName.Quote(schema)
        from quotedTable in QuotedName.Quote(table)
        select new TableName(quotedSchema, quotedTable);
}
