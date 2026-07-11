using System.Text.Json;
using System.Text.Json.Serialization;

namespace DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;

/// <summary>
/// Provides helper methods to encode and decode <see cref="DynamicCursor"/> instances
/// to and from a string token suitable for client-side infinite scroll (crusor) pagination.
/// </summary>
public static class DynamicCursorTokenizer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    /// <summary>
    /// Encodes a <see cref="DynamicCursor"/> into a Base64 string token.
    /// This token can be used by clients to request the next page.
    /// </summary>
    /// <param name="cursor">The dynamic cursor containing the values of the last item on a page.</param>
    /// <returns>A Base64-encoded string representing the cursor.</returns>
    public static string Encode(this DynamicCursor cursor)
    {
        SerializableValue[] items = cursor.Values
            .Select(SerializableValue.Create)
            .ToArray();

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(items, Options);

        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decodes a Base64 string token back into a <see cref="DynamicCursor"/>.
    /// </summary>
    /// <param name="token">The Base64 string token representing a cursor.</param>
    /// <returns>The decoded <see cref="DynamicCursor"/>, or <c>null</c> if the token is <c>null</c> or empty.</returns>
    public static DynamicCursor? Decode(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        byte[] bytes = Convert.FromBase64String(token);

        SerializableValue[] items = JsonSerializer.Deserialize<SerializableValue[]>(bytes, Options)!;

        return new DynamicCursor(items.Select(i => i.ToObject()).ToArray());
    }

    private sealed class SerializableValue
    {
        public string? Type { get; init; }
        public JsonElement Value { get; init; }

        public static SerializableValue Create(object? value)
        {
            return new SerializableValue
            {
                Type = value?.GetType().AssemblyQualifiedName,
                Value = JsonSerializer.SerializeToElement(value, Options)
            };
        }

        public object? ToObject()
        {
            if (Type is null)
                return null;

            Type type = System.Type.GetType(Type)!;

            return JsonSerializer.Deserialize(Value.GetRawText(), type, Options);
        }
    }
}
