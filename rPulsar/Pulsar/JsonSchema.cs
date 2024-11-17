using System.Buffers;
using System.Text;
using System.Text.Json;

using DotPulsar;
using DotPulsar.Abstractions;

namespace rPulsar.Pulsar;

/// <summary>
/// A schema class handling encoding and decoding class instances to JSON
/// format, which is supported by Pulsar 
/// </summary>
/// <typeparam name="T">The type of of class the schema encodes and decodes
/// </typeparam>
public class JsonSchema<T> : ISchema<T>
{
    private const string EncodingKey = "__encoding";

    private readonly Encoding _encoding;

    public JsonSchema(Encoding encoding)
    {
        _encoding = encoding;
        var properties = new Dictionary<string, string>
        {
            { EncodingKey, encoding.EncodingName }
        };

        SchemaInfo = new SchemaInfo(
            "String",
            Array.Empty<byte>(),
            SchemaType.String,
            properties
        );
    }

    public static JsonSchema<T> Default => new(Encoding.UTF8);

    public SchemaInfo SchemaInfo { get; }


    /// <summary>
    /// Decodes a sequence of bytes into an instance of the class
    /// </summary>
    /// <param name="bytes">The sequence of bytes to decode</param>
    /// <param name="schemaVersion">A byte array representing a schema version</param>
    /// <returns>An instance of the type</returns>
    /// <exception cref="JsonException">Thrown if decoding fails due to an
    /// invalid object being returned by the json serializer.</exception>
    public T Decode(
        ReadOnlySequence<byte> bytes,
        byte[]? schemaVersion = null
    )
    {
        var json = _encoding.GetString(bytes);
        return JsonSerializer.Deserialize<T>(
                json
            ) ??
            throw new JsonException("Could not deserialize JSON.");
    }

    /// <summary>
    /// Encode an instance of the type to a byte sequence
    /// </summary>
    /// <param name="message">The type instance to encode</param>
    /// <returns>A byte sequence representing the encoded type instance</returns>
    public ReadOnlySequence<byte> Encode(T message)
    {
        var json = JsonSerializer.Serialize(
            message,
            new JsonSerializerOptions()
        );
        return new ReadOnlySequence<byte>(_encoding.GetBytes(json));
    }
}