using System.Text.Json;
using System.Text.Json.Serialization;

namespace DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;

/// <summary>
/// Can be used to serialize/deserialize <see cref="InfinitePageOptions"/> or just <see cref="DynamicCursor"/> itself.
/// </summary>
public sealed class DynamicCursorJsonConverter : JsonConverter<DynamicCursor?>
{
    /// <summary>
    /// Logic when deserializing.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="JsonException"></exception>
    public override DynamicCursor? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected a Base64-encoded cursor string.");

        return DynamicCursorTokenizer.Decode(reader.GetString());
    }

    /// <summary>
    /// Logic when serializing.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, DynamicCursor? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Encode());
    }
}
