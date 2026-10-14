using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tessera.Sql;

/// <summary>
/// A query ready for the database: the SQL text, and the values that travel beside it (S-01). This
/// file belongs to the Sql module; the functions in Queries.cs build every query with it, and
/// <see cref="Database"/> sends them.
/// </summary>
/// <remarks>
/// No value from a web request may change what a query does, so each value goes as a bound
/// parameter: a value the database receives apart from the SQL text, like an answer written in a
/// form's box rather than in its questions. The database never reads a parameter as SQL.
/// <para>
/// Only the named functions in Queries.cs can make a query (S-01 c2). The constructor is private,
/// and <see cref="Of"/> and <see cref="QueryHandler"/> are internal, which in C# means only code in
/// this project may use them. Other projects receive a Query as a value and can read its text, but
/// never write one. Query is a record, a type that holds fixed values, like a frozen Python dataclass.
/// </para>
/// </remarks>
public sealed record Query
{
    private Query(string text, ImmutableArray<object> parameters) => (Text, Parameters) = (text, parameters);

    /// <summary>The SQL text, with <c>@p0</c>, <c>@p1</c> and so on marking where each value goes.</summary>
    public string Text { get; }

    /// <summary>The values, in order: <c>@p0</c> is the first.</summary>
    public ImmutableArray<object> Parameters { get; }

    /// <summary>
    /// Builds a query from an interpolated string, a string such as <c>$"... {x} ..."</c> whose braces
    /// hold values. Its fixed text becomes SQL. A <see cref="QuotedName"/>, <see cref="TableName"/>,
    /// <see cref="NameList"/>, <see cref="OrderBy"/> or <see cref="SqlType"/> in braces becomes SQL
    /// text too, since each has passed a check before it gets here. A <see cref="ValueList"/> becomes
    /// several bound parameters, and any other value, a string included, becomes one.
    /// </summary>
    internal static Query Of(QueryHandler query) => query.Build();

    /// <summary>
    /// The compiler hands each piece of a <c>$"…"</c> string to these methods in turn, left to right.
    /// Overloads, methods that share a name but take different types, decide what each piece becomes;
    /// the generic one, written for a value of any type <c>T</c>, makes a bound parameter.
    /// </summary>
    [InterpolatedStringHandler]
    internal ref struct QueryHandler
    {
        private readonly StringBuilder _text;
        private readonly ImmutableArray<object>.Builder _parameters;

        public QueryHandler(int literalLength, int formattedCount)
        {
            // Reserves room at the start: the fixed text, plus about four characters, such as @p12, for each
            // value.
            _text = new StringBuilder(literalLength + (formattedCount * 4));
            _parameters = ImmutableArray.CreateBuilder<object>(formattedCount);
        }

        public readonly void AppendLiteral(string text) => _text.Append(text);

        public readonly void AppendFormatted(QuotedName name) => _text.Append(name.Text);

        public readonly void AppendFormatted(TableName name) => _text.Append(name.ToString());

        public readonly void AppendFormatted(NameList names) => _text.Append(names.ToString());

        public readonly void AppendFormatted(OrderBy order) => _text.Append(order.ToString());

        public readonly void AppendFormatted(SqlType type) => _text.Append(type.Keyword);

        public readonly void AppendFormatted(ValueList values)
        {
            foreach (var value in values.Values)
            {
                _text.Append(", ");
                AppendFormatted(value);
            }
        }

        // 'notnull' keeps null out: a value a row lacks travels as SqlNull instead.
        public readonly void AppendFormatted<T>(T value) where T : notnull
        {
            _text.Append(CultureInfo.InvariantCulture, $"@p{_parameters.Count}");
            _parameters.Add(value);
        }

        internal readonly Query Build() => new(_text.ToString(), _parameters.ToImmutable());
    }
}

/// <summary>
/// Quoted names joined by commas, for a list of columns: <c>"id", "name"</c>. With
/// <paramref name="LeadingComma"/>, a comma comes before each name, <c>, "id", "name"</c>, so a list
/// that may be empty can follow another column.
/// </summary>
public sealed record NameList(ImmutableArray<QuotedName> Names, bool LeadingComma = false)
{
    public override string ToString() =>
        LeadingComma
            ? string.Concat(Names.Select(name => $", {name.Text}"))
            : string.Join(", ", Names.Select(name => name.Text));
}

/// <summary>
/// An ORDER BY clause, which sorts the rows, on the table the query calls <c>t</c>, on a line of its
/// own: <c>ORDER BY t."id"</c>. With no names it writes nothing, since a view, a saved query that
/// reads like a table, has no key to sort by.
/// </summary>
public sealed record OrderBy(ImmutableArray<QuotedName> Names)
{
    public override string ToString() =>
        Names.IsDefaultOrEmpty ? "" : "\n  ORDER BY " + string.Join(", ", Names.Select(name => $"t.{name.Text}"));
}

/// <summary>
/// The value 'nothing', for a column a row leaves empty, such as a place with no website. A bound
/// parameter must be a value, so this stands in for the database's NULL, its mark for an empty cell.
/// The private constructor leaves <see cref="Value"/> as the only one.
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
/// The column types the import may create. A type goes into the SQL text itself, not beside it, so it
/// comes from this closed set and never from the GeoJSON file. The private constructor keeps the set
/// closed.
/// </summary>
public sealed record SqlType
{
    private SqlType(string keyword) => Keyword = keyword;

    /// <summary>The type as SQL writes it.</summary>
    public string Keyword { get; }

    public static readonly SqlType Text = new("text");
    public static readonly SqlType Number = new("double precision");
    public static readonly SqlType Boolean = new("boolean");

    /// <summary>
    /// A point in longitude and latitude, in degrees. 4326 is its SRID, the number that names a
    /// coordinate system; this one is WGS 84, the system GPS uses.
    /// </summary>
    public static readonly SqlType Point4326 = new("geometry(Point, 4326)");
}
