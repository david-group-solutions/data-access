using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;

/// <summary>
/// Provides helper methods to encode and decode <see cref="DynamicCursor"/> instances
/// to and from a string token suitable for client-side infinite scroll (crusor) pagination.
/// </summary>
public static class DynamicCursorTokenizer
{
    /// <summary>
    /// Encodes a <see cref="DynamicCursor"/> into a Base64 string token.
    /// This token can be used by clients to request the next page.
    /// </summary>
    /// <param name="cursor">The dynamic cursor containing the values of the last item on a page.</param>
    /// <returns>A Base64-encoded string representing the cursor.</returns>
    public static string Encode(this DynamicCursor cursor)
    {
        string json = JsonSerializer.Serialize(cursor.Values);
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
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

        string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
        object?[] jsonElements = JsonSerializer.Deserialize<object?[]>(json) ?? [];

        object?[] values = new object?[jsonElements.Length];
        for (int i = 0; i < jsonElements.Length; i++)
            values[i] = ConvertJsonElement(jsonElements[i]);

        return new DynamicCursor(values);
    }

    /// <summary>
    /// Converts a <see cref="JsonElement"/> or object to a .NET type.
    /// Handles numbers, strings, booleans, and nulls.
    /// </summary>
    /// <param name="obj">The object or <see cref="JsonElement"/> to convert.</param>
    /// <returns>The converted .NET value.</returns>
    /// <exception cref="NotSupportedException">Thrown if the <see cref="JsonElement"/> type is unsupported.</exception>
    private static object? ConvertJsonElement(object? obj)
    {
        if (obj is not JsonElement je) return obj;

        return je.ValueKind switch
        {
            JsonValueKind.String => ConvertJsonString(je),
            JsonValueKind.Number => ConvertJsonNumber(je),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => throw new NotSupportedException($"Unsupported JSON element {je.ValueKind}")
        };
    }

    private static object? ConvertJsonString(JsonElement je)
    {
        string? s = je.GetString();

        if (s is null)
            return null;

        if (Guid.TryParse(s, out Guid guid))
            return guid;

        if (DateOnly.TryParse(s, out DateOnly dateOnly))
            return dateOnly;

        if (TimeOnly.TryParse(s, out TimeOnly timeOnly))
            return timeOnly;

        // NOTE: There is an ambiguity between TimeSpan and TimeOnly.
        //       Currently, it's not possible to make the round trip unless we preserve the actual type in encoded string.
        //
        // if (TimeSpan.TryParse(s, out TimeSpan ts))
        //     return ts;

        if (DateTime.TryParse(s, null, DateTimeStyles.RoundtripKind, out DateTime dt))
            return dt;

        if (s.Length == 1)
            return s[0];

        return s;
    }

    private static object ConvertJsonNumber(JsonElement je)
    {
        if (je.TryGetByte(out byte b)) return b;
        if (je.TryGetSByte(out sbyte sb)) return sb;
        if (je.TryGetInt16(out short s)) return s;
        if (je.TryGetUInt16(out ushort us)) return us;
        if (je.TryGetInt32(out int i)) return i;
        if (je.TryGetUInt32(out uint ui)) return ui;
        if (je.TryGetInt64(out long l)) return l;
        if (je.TryGetUInt64(out ulong ul)) return ul;

        if (je.TryGetDecimal(out decimal dec))
            return dec;

        if (je.TryGetDouble(out double dbl))
            return dbl;

        if (BigInteger.TryParse(je.GetRawText(), out BigInteger big))
            return big;

        return je.GetRawText();
    }
}
