using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace Tessera.Etl;

/// <summary>
/// Teaches System.Text.Json to read and write a <b><i>Maybe</i></b>, a value that is either there or
/// none, since the library knows no Maybe of its own. A JSON null reads as none; any other value
/// reads as the inner type and is wrapped. None writes null. A key the file leaves out never reaches
/// the converter, so the record's own default for it, none, stands.
/// </summary>
public sealed class MaybeJson : JsonConverterFactory
{
    /// <summary>True for any Maybe, whatever it holds.</summary>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Maybe<>);

    /// <summary>
    /// The converter for one kind of Maybe, such as a Maybe of a number. It hands the inner value to
    /// the converter the options already hold for that type, so a misspelt key or a wrong type inside
    /// it is refused as it would be without the Maybe. One thing differs: for an object inside a
    /// Maybe, such as stopWhen, the error's path stops at the object, '$.stopWhen', and no longer
    /// names the key within it, since the library starts a new path inside a converter.
    /// </summary>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var inner = typeToConvert.GetGenericArguments()[0];
        // Activator gives null only for a Nullable type, and MaybeConverter is a class.
        return (JsonConverter)Activator.CreateInstance(typeof(MaybeConverter<>).MakeGenericType(inner), options.GetConverter(inner))!;
    }

    private sealed class MaybeConverter<T>(JsonConverter inner) : JsonConverter<Maybe<T>>
    {
        private readonly JsonConverter<T> _inner = (JsonConverter<T>)inner;

        // Hands a JSON null to Read, rather than letting the library decide what it means.
        public override bool HandleNull => true;

        public override Maybe<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null
                ? Maybe<T>.None
                // The inner converter gives null only for a JSON null, handled above; Maybe.From would
                // make none of it all the same.
                : Maybe.From(_inner.Read(ref reader, typeof(T), options)!);

        public override void Write(Utf8JsonWriter writer, Maybe<T> value, JsonSerializerOptions options)
        {
            if (value.TryGetValue(out var found)) _inner.Write(writer, found, options);
            else writer.WriteNullValue();
        }
    }
}
