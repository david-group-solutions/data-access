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
        [Fact]
        public void MultipleHeterogeneousValues_AllRoundTrip()
        {
            // Arrange
            Guid guid = Guid.NewGuid();

            DateTime dateTimeUtc = new(2026, 7, 10, 23, 2, 10, DateTimeKind.Utc);
            DateTime dateTimeLocal = new(2026, 7, 10, 23, 2, 10, DateTimeKind.Local);
            DateTime dateTimeUnspecified = new(2026, 7, 10, 23, 2, 10, DateTimeKind.Unspecified);

            DateOnly dateOnly = new(2025, 12, 31);

            TimeOnly timeOnly = new(14, 30, 0);

            TimeSpan timeSpan = new(1, 15, 30);

            object?[] expected =
            [
                byte.MinValue,
                byte.MaxValue,

                sbyte.MinValue,
                sbyte.MaxValue,

                short.MinValue,
                short.MaxValue,

                ushort.MinValue,
                ushort.MaxValue,

                int.MinValue,
                int.MaxValue,

                uint.MinValue,
                uint.MaxValue,

                long.MinValue,
                long.MaxValue,

                ulong.MinValue,
                ulong.MaxValue,

                float.MinValue,
                float.MaxValue,
                float.NegativeZero,
                float.Epsilon,
                float.NaN,
                float.PositiveInfinity,
                float.NegativeInfinity,

                double.MinValue,
                double.MaxValue,
                double.NegativeZero,
                double.Epsilon,
                double.NaN,
                double.PositiveInfinity,
                double.NegativeInfinity,

                decimal.MinValue,
                decimal.Zero,
                decimal.MaxValue,

                false,
                true,

                null,

                "Hello World",
                "Привет",
                "こんにちは",
                "😀",
                "\0",
                "Line1\nLine2",
                "  leading and trailing  ",
                "",

                'A',
                'Ж',
                '\0',
                'ø',

                DayOfWeek.Friday,

                Guid.Empty,
                guid,

                dateTimeUtc,
                dateTimeLocal,
                dateTimeUnspecified,
                DateTime.MinValue,
                DateTime.MaxValue,

                dateOnly,
                DateOnly.MinValue,
                DateOnly.MaxValue,

                timeOnly,
                TimeOnly.MinValue,
                TimeOnly.MaxValue,

                timeSpan,
                TimeSpan.MinValue,
                TimeSpan.MaxValue,
                TimeSpan.Zero
            ];

            // Act
            object?[] actual = RoundTrip(expected);

            // Assert
            Assert.Equal(expected.Length, actual.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                object? exp = expected[i];
                object? act = actual[i];

                switch (exp)
                {
                    case float f:
                        float af = Assert.IsType<float>(act);
                        Assert.Equal(BitConverter.SingleToInt32Bits(f), BitConverter.SingleToInt32Bits(af));
                        break;
                    case double d:
                        double ad = Assert.IsType<double>(act);
                        Assert.Equal(BitConverter.DoubleToInt64Bits(d), BitConverter.DoubleToInt64Bits(ad));
                        break;
                    case DateTime dt:
                        DateTime adt = Assert.IsType<DateTime>(act);
                        Assert.Equal(dt, adt);
                        Assert.Equal(dt.Kind, adt.Kind);
                        break;
                    default:
                        Assert.Equal(exp, act);
                        break;
                }
            }
        }

        [Fact]
        public void EmptyValues_ProduceEmptyArray()
        {
            // Arrange, Act
            object?[] values = RoundTrip();

            // Assert
            Assert.Empty(values);
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
}
