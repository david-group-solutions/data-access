using DavidGroup.Core.DataAccess.Pagination;

namespace DavidGroup.Core.DataAccessTests.Pagination;

public static class PaginationExtensionsTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Returns a list of integers 1...count.</summary>
    private static List<int> Range(int count) => Enumerable.Range(1, count).ToList();

    private static PageOptions Options(int page, int size) => new() { Page = page, Size = size };

    // =========================================================================
    // IEnumerable<T> overload
    // =========================================================================

    public class EnumerableToPageData
    {
        // ----- basic pagination -----

        [Fact]
        public void FirstPage_ReturnsFirstChunk()
        {
            // Arrange
            IEnumerable<int> source = Range(10);

            // Act
            PageData<int> result = source.ToPageData(Options(1, 3));

            // Assert
            Assert.Equal([1, 2, 3], result.Entities);
        }

        [Fact]
        public void SecondPage_ReturnsSecondChunk()
        {
            // Arrange
            IEnumerable<int> source = Range(10);

            // Act
            PageData<int> result = source.ToPageData(Options(2, 3));

            // Assert
            Assert.Equal([4, 5, 6], result.Entities);
        }

        [Fact]
        public void LastPage_ReturnsRemainingItems()
        {
            // Arrange
            IEnumerable<int> source = Range(10);

            // Act
            PageData<int> result = source.ToPageData(Options(4, 3));

            // Assert
            Assert.Equal([10], result.Entities);
        }

        [Fact]
        public void PageBeyondData_ReturnsEmptyItems()
        {
            // Arrange
            IEnumerable<int> source = Range(5);

            // Act
            PageData<int> result = source.ToPageData(Options(10, 3));

            // Assert
            Assert.Empty(result.Entities);
        }

        // ----- total count -----

        [Fact]
        public void TotalCount_EqualsSourceLength()
        {
            // Arrange
            IEnumerable<int> source = Range(17);

            // Act
            PageData<int> result = source.ToPageData(Options(1, 5));

            // Assert
            Assert.Equal(17, result.TotalCount);
        }

        [Fact]
        public void TotalCount_IsCorrectEvenOnLastPage()
        {
            // Arrange
            IEnumerable<int> source = Range(7);

            // Act
            PageData<int> result = source.ToPageData(Options(3, 3));

            // Assert
            Assert.Equal(7, result.TotalCount);
            Assert.Equal([7], result.Entities);
        }

        // ----- edge: empty source -----

        [Fact]
        public void EmptySource_ReturnsEmptyItemsAndZeroCount()
        {
            // Arrange
            IEnumerable<int> source = [];

            // Act
            PageData<int> result = source.ToPageData(Options(1, 10));

            // Assert
            Assert.Empty(result.Entities);
            Assert.Equal(0, result.TotalCount);
        }

        // ----- page size equals / exceeds source -----

        [Fact]
        public void PageSizeLargerThanSource_ReturnsAllItems()
        {
            // Arrange
            IEnumerable<int> source = Range(4);

            // Act
            PageData<int> result = source.ToPageData(Options(1, 100));

            // Assert
            Assert.Equal(Range(4), result.Entities);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void PageSizeEqualsSourceCount_ReturnsAllItemsOnFirstPage()
        {
            // Arrange
            IEnumerable<int> source = Range(5);

            // Act
            PageData<int> result = source.ToPageData(Options(1, 5));

            // Assert
            Assert.Equal(Range(5), result.Entities);
        }

        // ----- type parameter -----

        [Fact]
        public void WorksWithStringType()
        {
            // Arrange
            IEnumerable<string> source = ["a", "b", "c", "d", "e"];

            // Act
            PageData<string> result = source.ToPageData(Options(2, 2));

            // Assert
            Assert.Equal(["c", "d"], result.Entities);
        }

        [Fact]
        public void WorksWithObjectType()
        {
            // Arrange
            IEnumerable<object> source = Enumerable.Range(1, 6)
                .Select(i => new { Id = i })
                .ToList();

            // Act
            PageData<object> result = source.ToPageData(Options(2, 2));

            // Assert
            Assert.Equal(2, result.Entities.Count());
            Assert.Equal(source.Skip(2).Take(2), result.Entities);
        }

        // ----- source is not double-enumerated -----

        [Fact]
        public void NonRepeatable_LazySource_IsEnumeratedOnce()
        {
            // Arrange
            int iterationCount = 0;

            // Act
            LazySource().ToPageData(Options(1, 3));

            // Assert
            Assert.Equal(1, iterationCount);

            return;

            IEnumerable<int> LazySource()
            {
                iterationCount++;

                foreach (int i in Range(5))
                    yield return i;
            }
        }
    }

    // =========================================================================
    // IQueryable<T> overload
    // =========================================================================

    public class QueryableToPageData
    {
        // ----- basic pagination -----

        [Fact]
        public void FirstPage_ReturnsFirstChunk()
        {
            // Arrange
            IQueryable<int> source = Range(10).AsQueryable();

            // Act
            PageData<int> result = source.ToPageData(Options(1, 3));

            // Assert
            Assert.Equal([1, 2, 3], result.Entities);
        }

        [Fact]
        public void SecondPage_ReturnsSecondChunk()
        {
            // Arrange
            IQueryable<int> source = Range(10).AsQueryable();

            // Act
            PageData<int> result = source.ToPageData(Options(2, 3));

            // Assert
            Assert.Equal([4, 5, 6], result.Entities);
        }

        [Fact]
        public void LastPage_ReturnsRemainingItems()
        {
            // Arrange
            IQueryable<int> source = Range(10).AsQueryable();

            // Act
            PageData<int> result = source.ToPageData(Options(4, 3));

            // Assert
            Assert.Equal([10], result.Entities);
        }

        [Fact]
        public void PageBeyondData_ReturnsEmptyItems()
        {
            // Arrange
            IQueryable<int> source = Range(5).AsQueryable();

            // Act
            PageData<int> result = source.ToPageData(Options(10, 3));

            // Assert
            Assert.Empty(result.Entities);
        }

        // ----- total count -----

        [Fact]
        public void TotalCount_EqualsSourceLength()
        {
            // Arrange
            IQueryable<int> source = Range(17).AsQueryable();

            // Act
            PageData<int> result = source.ToPageData(Options(1, 5));

            // Assert
            Assert.Equal(17, result.TotalCount);
        }

        [Fact]
        public void TotalCount_IsCorrectEvenOnLastPage()
        {
            // Arrange
            IQueryable<int> source = Range(7).AsQueryable();

            // Act
            PageData<int> result = source.ToPageData(Options(3, 3));

            // Assert
            Assert.Equal(7, result.TotalCount);
            Assert.Equal([7], result.Entities);
        }

        // ----- edge: empty source -----

        [Fact]
        public void EmptySource_ReturnsEmptyItemsAndZeroCount()
        {
            // Arrange
            IQueryable<int> source = Enumerable.Empty<int>().AsQueryable();

            // Act
            PageData<int> result = source.ToPageData(Options(1, 10));

            // Assert
            Assert.Empty(result.Entities);
            Assert.Equal(0, result.TotalCount);
        }

        // ----- page size equals / exceeds source -----

        [Fact]
        public void PageSizeLargerThanSource_ReturnsAllItems()
        {
            // Arrange
            IQueryable<int> source = Range(4).AsQueryable();

            // Act
            PageData<int> result = source.ToPageData(Options(1, 100));

            // Assert
            Assert.Equal(Range(4), result.Entities);
            Assert.Equal(4, result.TotalCount);
        }

        [Fact]
        public void PageSizeEqualsSourceCount_ReturnsAllItemsOnFirstPage()
        {
            // Arrange
            IQueryable<int> source = Range(5).AsQueryable();

            // Act
            PageData<int> result = source.ToPageData(Options(1, 5));

            // Assert
            Assert.Equal(Range(5), result.Entities);
        }

        // ----- type parameter -----

        [Fact]
        public void WorksWithStringType()
        {
            // Arrange
            string[] sourceArray = ["a", "b", "c", "d", "e"];
            IQueryable<string> source = sourceArray.AsQueryable();

            // Act
            PageData<string> result = source.ToPageData(Options(2, 2));

            // Assert
            Assert.Equal(["c", "d"], result.Entities);
        }

        [Fact]
        public void WorksWithObjectType()
        {
            // Arrange
            IQueryable<object> source = Enumerable.Range(1, 6)
                .Select(i => new { Id = i })
                .AsQueryable();

            // Act
            PageData<object> result = source.ToPageData(Options(2, 2));

            // Assert
            Assert.Equal(2, result.Entities.Count());
            Assert.Equal(source.Skip(2).Take(2), result.Entities);
        }
    }

    // =========================================================================
    // Cross-overload consistency
    // =========================================================================

    public class OverloadConsistency
    {
        [Theory]
        [InlineData(1, 5)]
        [InlineData(2, 5)]
        [InlineData(3, 5)]
        [InlineData(1, 1)]
        [InlineData(10, 3)]
        public void IEnumerable_And_IQueryable_ProduceSameItems(int page, int size)
        {
            List<int> data = Range(30);
            PageOptions opts = Options(page, size);

            PageData<int> fromEnumerable = data.AsEnumerable().ToPageData(opts);
            PageData<int> fromQueryable = data.AsQueryable().ToPageData(opts);

            Assert.Equal(fromEnumerable.Entities, fromQueryable.Entities);
            Assert.Equal(fromEnumerable.TotalCount, fromQueryable.TotalCount);
        }
    }
}
