using Tessera.Kernel;

namespace Tessera.Sql;

/// <summary>
/// A table, schema or column name, wrapped in double quotes so the database reads it as a name and
/// never as part of a command (US-03, S-02). A schema is a folder of tables inside one database. This
/// belongs to the Sql module; Catalogue quotes each published table's names with it, and Import
/// quotes the tables it creates.
/// </summary>
/// <remarks>
/// <para>
/// SQL injection is an attack that hides SQL inside text a user sends, in the hope that the database
/// runs it; you meet it at school under network security. A name such as <c>x; DROP TABLE cafes</c>
/// would end one command and start another. Inside double quotes the database reads it as one odd
/// name instead. Doubling each quote inside the name stops a name such as <c>odd"name</c> from
/// closing its own quotes early.
/// </para>
/// <para>
/// It is a record, a bundle of named values that cannot change once made. Its constructor, the code
/// that makes a new one, is private, so <see cref="Quote"/> is the one way to make a name, and every
/// name that reaches SQL has passed through it.
/// </para>
/// </remarks>
public sealed record QuotedName
{
    private QuotedName(string text, string raw) => (Text, Raw) = (text, raw);

    /// <summary>The name as SQL text, quotes included: <c>"odd ""name"""</c>.</summary>
    public string Text { get; }

    /// <summary>The name as the database stores it, without quotes: <c>odd "name"</c>.</summary>
    public string Raw { get; }

    /// <summary>
    /// Wraps a name in double quotes and doubles every quote inside it; refuses an empty name.
    /// </summary>
    public static Result<QuotedName, string> Quote(string name) =>
        name.Length == 0
            ? Result.Refuse<QuotedName, string>("a name may not be empty")
            : Result.Ok<QuotedName, string>(new QuotedName($"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"", name));

    // Printing a name, in a query or a log, gives the safe, quoted form.
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
