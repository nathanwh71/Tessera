using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tessera.Sql;

/// <summary>
/// A query ready for the database: SQL text, and the values that travel beside it (S-01).
/// </summary>
/// <remarks>
/// A bound parameter is a value the database receives apart from the SQL text, like an answer
/// written in a form's box rather than in its questions. The database never reads it as SQL, so no
/// value from a request can change what a query does.
/// The constructor is private, and <see cref="Of"/> and <see cref="QueryHandler"/> are internal to
/// this project, so only the named functions in Queries.cs can make a query (S-01 c2). Other
/// projects receive a Query as a value and can read its text, but never write one.
/// </remarks>
public sealed record Query
{
    private Query(string text, ImmutableArray<object> parameters) => (Text, Parameters) = (text, parameters);

    /// <summary>The SQL text, with <c>@p0</c>, <c>@p1</c> and so on where each value goes.</summary>
    public string Text { get; }

    /// <summary>The values, in order: <c>@p0</c> is the first.</summary>
    public ImmutableArray<object> Parameters { get; }

    /// <summary>
    /// Builds a query from an interpolated string. Its fixed text becomes SQL; a
    /// <see cref="QuotedName"/>, <see cref="TableName"/>, <see cref="NameList"/> or
    /// <see cref="SqlType"/> in braces becomes SQL text too; any other value in braces becomes a
    /// bound parameter, a string included.
    /// </summary>
    internal static Query Of(QueryHandler query) => query.Build();

    /// <summary>
    /// The compiler hands each part of a <c>$"…"</c> string to these methods in turn. Overloads
    /// decide what each part becomes; the generic one, for any other value, makes a parameter.
    /// </summary>
    [InterpolatedStringHandler]
    internal ref struct QueryHandler
    {
        private readonly StringBuilder _text;
        private readonly ImmutableArray<object>.Builder _parameters;

        public QueryHandler(int literalLength, int formattedCount)
        {
            _text = new StringBuilder(literalLength + (formattedCount * 4));
            _parameters = ImmutableArray.CreateBuilder<object>(formattedCount);
        }

        public readonly void AppendLiteral(string text) => _text.Append(text);

        public readonly void AppendFormatted(QuotedName name) => _text.Append(name.Text);

        public readonly void AppendFormatted(TableName name) => _text.Append(name.ToString());

        public readonly void AppendFormatted(NameList names) => _text.Append(names.ToString());

        public readonly void AppendFormatted(SqlType type) => _text.Append(type.Keyword);

        public readonly void AppendFormatted(ValueList values)
        {
            foreach (var value in values.Values)
            {
                _text.Append(", ");
                AppendFormatted(value);
            }
        }

        public readonly void AppendFormatted<T>(T value) where T : notnull
        {
            _text.Append(CultureInfo.InvariantCulture, $"@p{_parameters.Count}");
            _parameters.Add(value);
        }

        internal readonly Query Build() => new(_text.ToString(), _parameters.ToImmutable());
    }
}

/// <summary>
/// Quoted names joined by commas, for a column list: <c>"id", "name"</c>. With
/// <paramref name="LeadingComma"/>, each name follows a comma, <c>, "id", "name"</c>, so a list that
/// may be empty can follow another column.
/// </summary>
public sealed record NameList(ImmutableArray<QuotedName> Names, bool LeadingComma = false)
{
    public override string ToString() =>
        LeadingComma
            ? string.Concat(Names.Select(name => $", {name.Text}"))
            : string.Join(", ", Names.Select(name => name.Text));
}

/// <summary>
/// The value "nothing", for a column a row leaves empty, such as a place with no website. A bound
/// parameter must be a value, so this stands in for the database's NULL.
/// </summary>
public sealed record SqlNull
{
    private SqlNull() { }

    public static readonly SqlNull Value = new();
}

/// <summary>
/// Values that each become a bound parameter after a comma: <c>, @p3, @p4</c>. The import uses it
/// for a row whose number of columns the file decides.
/// </summary>
public sealed record ValueList(ImmutableArray<object> Values);

/// <summary>
/// The column types the import may create. A type is SQL text, so it comes from this closed set and
/// never from the GeoJSON file.
/// </summary>
public sealed record SqlType
{
    private SqlType(string keyword) => Keyword = keyword;

    public string Keyword { get; }

    public static readonly SqlType Text = new("text");
    public static readonly SqlType Number = new("double precision");
    public static readonly SqlType Boolean = new("boolean");
    public static readonly SqlType Point4326 = new("geometry(Point, 4326)");
}
