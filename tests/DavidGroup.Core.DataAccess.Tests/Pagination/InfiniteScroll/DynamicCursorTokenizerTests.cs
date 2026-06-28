using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;

namespace DavidGroup.Core.DataAccess.Tests.Pagination.InfiniteScroll;

public static class DynamicCursorTokenizerTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// Encodes then decodes, returning the round-tripped values array.
    private static object?[] RoundTrip(params object?[] values)
    {
        DynamicCursor cursor = new(values);

        string token = cursor.Encode();

        return DynamicCursorTokenizer.Decode(token)!.Values;
    }

    // =========================================================================
    // Decode – null / empty guard
    // =========================================================================

    public class DecodeNullOrEmpty
    {
        [Fact]
        public void Null_ReturnsNull() =>
            Assert.Null(DynamicCursorTokenizer.Decode(null));

        [Fact]
        public void EmptyString_ReturnsNull() =>
            Assert.Null(DynamicCursorTokenizer.Decode(""));
    }

    // =========================================================================
    // Encode → Decode roundtrip (verifies both directions together)
    // =========================================================================

    public class EncodeDecodeRoundtrip
    {
        // ----- structural -----

        [Fact]
        public void EmptyValues_ProduceEmptyArray()
        {
            // Arrange, Act
            object?[] values = RoundTrip();

            // Assert
            Assert.Empty(values);
        }

        [Fact]
        public void MultipleValues_PreserveOrder()
        {
            // Arrange, Act
            object?[] values = RoundTrip(1, "hello", true);

            // Assert
            Assert.Equal(3, values.Length);
            Assert.Equal((byte)1, values[0]); // converted to byte because of number decoding logic
            Assert.Equal("hello", values[1]);
            Assert.Equal(true, values[2]);
        }

        [Fact]
        public void NullValue_RoundTrips()
        {
            // Arrange, Act
            object?[] values = RoundTrip((object?)null);

            // Assert
            Assert.Null(values[0]);
        }

        // ----- booleans -----

        [Fact]
        public void True_RoundTrips()
        {
            // Arrange, Act
            object?[] values = RoundTrip(true);

            // Assert
            Assert.Equal(true, values[0]);
        }

        [Fact]
        public void False_RoundTrips()
        {
            // Arrange, Act
            object?[] values = RoundTrip(false);

            // Assert
            Assert.Equal(false, values[0]);
        }

        // ----- integers – smallest winning type -----

        [Theory]
        [InlineData((byte)0)]
        [InlineData((byte)255)]
        public void ByteBoundaries_RoundTrip_AsByte(byte input)
        {
            // Arrange, Act
            object?[] result = RoundTrip(input);

            // Assert
            Assert.IsType<byte>(result[0]);
            Assert.Equal(input, result[0]);
        }

        [Theory]
        [InlineData((sbyte)-128)]
        [InlineData((sbyte)-1)]
        public void SByteBoundaries_RoundTrip_AsSByte(sbyte input)
        {
            // Arrange, Act
            object?[] result = RoundTrip(input);

            // Assert
            Assert.IsType<sbyte>(result[0]);
            Assert.Equal(input, result[0]);
        }

        [Theory]
        [InlineData(short.MinValue)]
        [InlineData((short)-129)]
        public void Int16Boundaries_RoundTrip_AsInt16(short input)
        {
            // Arrange, Act
            object?[] result = RoundTrip(input);

            // Assert
            Assert.IsType<short>(result[0]);
            Assert.Equal(input, result[0]);
        }

        [Fact]
        public void UInt16Max_RoundTrips_AsUInt16()
        {
            // Arrange, Act
            object?[] result = RoundTrip(ushort.MaxValue);

            // Assert
            Assert.IsType<ushort>(result[0]);
            Assert.Equal(ushort.MaxValue, result[0]);
        }

        [Fact]
        public void Int32Max_RoundTrips_AsInt32()
        {
            // Arrange, Act
            object?[] result = RoundTrip(int.MaxValue);

            // Assert
            Assert.IsType<int>(result[0]);
            Assert.Equal(int.MaxValue, result[0]);
        }

        [Fact]
        public void UInt32Max_RoundTrips_AsUInt32()
        {
            // Arrange, Act
            object?[] result = RoundTrip(uint.MaxValue);

            // Assert
            Assert.IsType<uint>(result[0]);
            Assert.Equal(uint.MaxValue, result[0]);
        }

        [Fact]
        public void Int64Max_RoundTrips_AsInt64()
        {
            // Arrange, Act
            object?[] result = RoundTrip(long.MaxValue);

            // Assert
            Assert.IsType<long>(result[0]);
            Assert.Equal(long.MaxValue, result[0]);
        }

        [Fact]
        public void UInt64Max_RoundTrips_AsUInt64()
        {
            // Arrange, Act
            object?[] result = RoundTrip(ulong.MaxValue);

            // Assert
            Assert.IsType<ulong>(result[0]);
            Assert.Equal(ulong.MaxValue, result[0]);
        }

        // ----- decimals / doubles -----

        [Fact]
        public void Decimal_RoundTrips()
        {
            // Arrange
            const decimal val = 3.14159265358979323846m;

            // Act
            object?[] values = RoundTrip(val);

            // Assert
            Assert.IsType<decimal>(values[0]);
            Assert.Equal(val, values[0]);
        }

        [Fact]
        public void Double_WhenNotRepresentableAsDecimal_RoundTrips()
        {
            // Arrange
            const double val = double.MaxValue; // double.MaxValue overflows decimal, so the double branch should win

            // Act
            object?[] values = RoundTrip(val);

            // Assert
            Assert.IsType<double>(values[0]);
            Assert.Equal(val, values[0]);
        }

        // ----- strings -----

        [Fact]
        public void PlainString_RoundTrips()
        {
            // Arrange
            const string val = "hello world";

            // Act
            object?[] values = RoundTrip(val);

            // Assert
            Assert.IsType<string>(values[0]);
            Assert.Equal(val, values[0]);
        }

        [Fact]
        public void EmptyString_RoundTrips_AsString()
        {
            // Arrange, Act
            object?[] values = RoundTrip("");

            // Assert
            Assert.IsType<string>(values[0]);
            Assert.Equal("", values[0]);
        }

        [Fact]
        public void SingleCharacterString_RoundTrips_AsChar()
        {
            // Arrange, Act
            object?[] values = RoundTrip("A");

            // Assert
            Assert.IsType<char>(values[0]);
            Assert.Equal('A', values[0]);
        }

        // ----- Guid -----

        [Fact]
        public void Guid_RoundTrips()
        {
            // Arrange
            Guid val = Guid.NewGuid();

            // Act
            object?[] values = RoundTrip(val.ToString());

            // Assert
            Assert.IsType<Guid>(values[0]);
            Assert.Equal(val, values[0]);
        }

        [Fact]
        public void Guid_IsRecognised_BeforeOtherStringParsing()
        {
            // Arrange
            const string guidString = "550e8400-e29b-41d4-a716-446655440000"; // A GUID string must not be accidentally parsed as DateTime or anything else

            // Act
            object?[] values = RoundTrip(guidString);

            // Assert
            Assert.IsType<Guid>(values[0]);
        }

        // ----- DateTime -----

        [Fact]
        public void DateTime_Utc_RoundTrips_WithKindPreserved()
        {
            // Arrange
            DateTime val = new(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);
            string dateStr = val.ToString("O");

            // Act
            object?[] values = RoundTrip(dateStr);

            // Assert
            Assert.IsType<DateTime>(values[0]);

            DateTime decoded = (DateTime)values[0]!;
            Assert.Equal(val, decoded);
            Assert.Equal(DateTimeKind.Utc, decoded.Kind);
        }

        [Fact]
        public void DateTime_Local_RoundTrips()
        {
            // Arrange
            DateTime val = new(2024, 1, 1, 8, 0, 0, DateTimeKind.Local);
            string dateStr = val.ToString("O");

            // Act
            object?[] values = RoundTrip(dateStr);

            // Assert
            Assert.IsType<DateTime>(values[0]);

            DateTime decoded = (DateTime)values[0]!;
            Assert.Equal(val, decoded);
            Assert.Equal(DateTimeKind.Local, decoded.Kind);
        }

        // ----- DateOnly -----

        [Fact]
        public void DateOnly_RoundTrips()
        {
            // Arrange
            DateOnly val = new(2025, 12, 31);
            string dateStr = val.ToString("O"); // yyyy-MM-dd

            // Act
            object?[] values = RoundTrip(dateStr);

            // Assert
            Assert.IsType<DateOnly>(values[0]);
            Assert.Equal(val, values[0]);
        }

        // ----- TimeOnly -----

        [Fact]
        public void TimeOnly_RoundTrips()
        {
            // Arrange
            TimeOnly val = new(14, 30, 0);
            string dateStr = val.ToString("O");

            object?[] values = RoundTrip(dateStr);

            // Assert
            Assert.IsType<TimeOnly>(values[0]);
            Assert.Equal(val, values[0]);
        }

        // ----- TimeSpan -----

        // NOTE: There is an ambiguity between TimeSpan and TimeOnly.
        //       Currently, it's not possible to make the round trip unless we preserve the actual type in encoded string.
        //
        // [Fact]
        // public void TimeSpan_RoundTrips()
        // {
        //     // Arrange
        //     TimeSpan val = TimeSpan.FromHours(2.5);
        //     string valStr = val.ToString("c"); // constant format: 02:30:00
        //
        //     // Act
        //     object?[] values = RoundTrip(valStr);
        //
        //     // Assert
        //     Assert.IsType<TimeSpan>(values[0]);
        //     Assert.Equal(val, values[0]);
        // }

        // ----- mixed multi-value cursor -----

        [Fact]
        public void MultipleHeterogeneousValues_AllRoundTrip()
        {
            Guid id = Guid.NewGuid();
            DateTime dt = new(2024, 3, 10, 0, 0, 0, DateTimeKind.Utc);

            object?[] values = RoundTrip(
                id.ToString(),
                dt.ToString("O"),
                (byte)7,
                "name",
                true,
                null,
                3.14m
            );

            Assert.Equal(7, values.Length);
            Assert.Equal(id, values[0]);
            Assert.Equal(dt, values[1]);
            Assert.Equal((byte)7, values[2]);
            Assert.Equal("name", values[3]);
            Assert.Equal(true, values[4]);
            Assert.Null(values[5]);
            Assert.Equal(3.14m, values[6]);
        }
    }

    // =========================================================================
    // Encode – token is valid Base64
    // =========================================================================

    public class EncodeTokenFormat
    {
        [Fact]
        public void Token_IsValidBase64()
        {
            // Arrange
            DynamicCursor cursor = new([1, "test"]);
            string token = cursor.Encode();

            // Act
            byte[] bytes = Convert.FromBase64String(token);

            // Assert
            Assert.NotEmpty(bytes);
        }

        [Fact]
        public void SameCursor_ProducesSameToken()
        {
            // Arrange
            DynamicCursor cursor1 = new([42, "abc"]);
            DynamicCursor cursor2 = new([42, "abc"]);

            // Act
            string token1 = cursor1.Encode();
            string token2 = cursor2.Encode();

            // Assert
            Assert.Equal(token1, token2);
        }

        [Fact]
        public void DifferentValues_ProduceDifferentTokens()
        {
            // Arrange
            DynamicCursor cursor1 = new([1]);
            DynamicCursor cursor2 = new([2]);

            // Act
            string token1 = cursor1.Encode();
            string token2 = cursor2.Encode();

            // Assert
            Assert.NotEqual(token1, token2);
        }
    }

    // =========================================================================
    // Decode – unsupported JSON kinds throw NotSupportedException
    // =========================================================================

    public class DecodeUnsupportedJsonKind
    {
        [Fact]
        public void JsonArray_ThrowsNotSupportedException()
        {
            // Arrange
            const string json = "[[1,2,3]]"; // Encode a JSON array nested inside the cursor values array
            string token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

            // Act, Assert
            Assert.Throws<NotSupportedException>(() => DynamicCursorTokenizer.Decode(token));
        }

        [Fact]
        public void JsonObject_ThrowsNotSupportedException()
        {
            // Arrange
            const string json = """[{"key":"value"}]""";
            string token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

            // Act, Assert
            Assert.Throws<NotSupportedException>(() => DynamicCursorTokenizer.Decode(token));
        }
    }
}
