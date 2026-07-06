using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Sql.Builders;
using DavidGroup.Core.DataAccess.Sql.Builders.BasicQuery;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Builders.BasicQuery;

public static class BasicQueryBuilderTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private class BasicQueryTestEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private class BasicQueryProjectedResult
    {
        public int Id { get; set; }
    }

    private static List<BasicQueryTestEntity> CreateFourEntities()
    {
        return
        [
            new BasicQueryTestEntity
            {
                Id = 1,
                Name = "Alpha"
            },

            new BasicQueryTestEntity
            {
                Id = 2,
                Name = "Beta"
            },

            new BasicQueryTestEntity
            {
                Id = 3,
                Name = "Gamma"
            },

            new BasicQueryTestEntity
            {
                Id = 4,
                Name = "Delta"
            }
        ];
    }

    // -------------------------------------------------------------------------
    // BasicQueryBuilder<TEntity> constructor tests
    // -------------------------------------------------------------------------

    public sealed class ConstructionTests
    {
        [Fact]
        public void Constructor_GivenQuery_ExposesSameQueryThroughQueryProperty()
        {
            // Arrange
            List<BasicQueryTestEntity> entities =
            [
                new()
                {
                    Id = 1,
                    Name = "Alpha"
                }
            ];
            IQueryable<BasicQueryTestEntity> sourceQuery = entities.AsQueryable();

            // Act
            BasicQueryBuilder<BasicQueryTestEntity> builder = new(sourceQuery);

            // Assert
            Assert.Same(sourceQuery, builder.Query);
        }
    }

    // -------------------------------------------------------------------------
    // BasicQueryBuilder<TEntity>.WithOrdering(IReadOnlyList<OrderingSpecification<TEntity>>?) tests
    // -------------------------------------------------------------------------

    public sealed class WithOrderingSpecificationsTests
    {
        [Fact]
        public void WithOrdering_GivenOrderingSpecifications_AppliesThemToQuery()
        {
            // Arrange
            List<BasicQueryTestEntity> entities =
            [
                new()
                {
                    Id = 3,
                    Name = "Gamma"
                },

                new()
                {
                    Id = 1,
                    Name = "Alpha"
                },

                new()
                {
                    Id = 2,
                    Name = "Beta"
                }
            ];
            IQueryable<BasicQueryTestEntity> sourceQuery = entities.AsQueryable();

            BasicQueryBuilder<BasicQueryTestEntity> builder = new(sourceQuery);

            List<OrderingSpecification<BasicQueryTestEntity>> specifications = [new(e => e.Id, false)];

            // Act
            BasicQueryBuilder<BasicQueryTestEntity> result = builder.WithOrdering(specifications);

            List<int> resultingIds = result.Query.Select(e => e.Id).ToList();

            // Assert
            Assert.Same(builder, result);
            Assert.Equal([
                1,
                2,
                3
            ], resultingIds);
        }

        [Fact]
        public void WithOrdering_GivenNullOrderingSpecifications_LeavesQueryReferenceUnchanged()
        {
            // Arrange
            List<BasicQueryTestEntity> entities =
            [
                new()
                {
                    Id = 1,
                    Name = "Alpha"
                }
            ];

            IQueryable<BasicQueryTestEntity> sourceQuery = entities.AsQueryable();

            BasicQueryBuilder<BasicQueryTestEntity> builder = new(sourceQuery);

            IReadOnlyList<OrderingSpecification<BasicQueryTestEntity>>? specifications = null;

            // Act
            BasicQueryBuilder<BasicQueryTestEntity> result = builder.WithOrdering(specifications);

            // Assert
            Assert.Same(builder, result);
            Assert.Same(sourceQuery, result.Query);
        }

        [Fact]
        public void WithOrdering_GivenEmptyOrderingSpecificationsList_LeavesQueryReferenceUnchanged()
        {
            // Arrange
            List<BasicQueryTestEntity> entities =
            [
                new()
                {
                    Id = 1,
                    Name = "Alpha"
                }
            ];

            IQueryable<BasicQueryTestEntity> sourceQuery = entities.AsQueryable();

            BasicQueryBuilder<BasicQueryTestEntity> builder = new(sourceQuery);

            IReadOnlyList<OrderingSpecification<BasicQueryTestEntity>> specifications = [];

            // Act
            BasicQueryBuilder<BasicQueryTestEntity> result = builder.WithOrdering(specifications);

            // Assert
            Assert.Same(builder, result);
            Assert.Same(sourceQuery, result.Query);
        }
    }

    // -------------------------------------------------------------------------
    // BasicQueryBuilder<TEntity>.WithProjection tests
    // -------------------------------------------------------------------------

    public sealed class WithProjectionTests
    {
        [Fact]
        public void WithProjection_GivenSelector_ReturnsNewBuilderWrappingProjectedResults()
        {
            // Arrange
            List<BasicQueryTestEntity> entities =
            [
                new()
                {
                    Id = 1,
                    Name = "Alpha"
                },

                new()
                {
                    Id = 2,
                    Name = "Beta"
                }
            ];

            IQueryable<BasicQueryTestEntity> sourceQuery = entities.AsQueryable();

            BasicQueryBuilder<BasicQueryTestEntity> builder = new(sourceQuery);

            Expression<Func<BasicQueryTestEntity, BasicQueryProjectedResult>> selector =
                e => new BasicQueryProjectedResult { Id = e.Id };

            // Act
            BasicQueryBuilder<BasicQueryProjectedResult> result = builder.WithProjection(selector);

            List<int> resultingIds = result.Query.Select(p => p.Id).ToList();

            // Assert
            Assert.NotSame(builder, result);
            Assert.Equal([
                1,
                2
            ], resultingIds);
        }

        [Fact]
        public void WithProjection_SelectorIsNull_ReturnsNewBuilderWrappingDefaultProjectedResults()
        {
            // Arrange
            List<BasicQueryTestEntity> entities =
            [
                new()
                {
                    Id = 1,
                    Name = "Alpha"
                },

                new()
                {
                    Id = 2,
                    Name = "Beta"
                }
            ];

            IQueryable<BasicQueryTestEntity> sourceQuery = entities.AsQueryable();

            BasicQueryBuilder<BasicQueryTestEntity> builder = new(sourceQuery);

            // Act
            BasicQueryBuilder<BasicQueryTestEntity> result = builder.WithProjection<BasicQueryTestEntity>(null);

            List<BasicQueryTestEntity> resultingEntities = result.Query.ToList();

            // Assert
            Assert.NotSame(builder, result);

            Assert.Equal(1, resultingEntities[0].Id);
            Assert.Equal("Alpha", resultingEntities[0].Name);

            Assert.Equal(2, resultingEntities[1].Id);
            Assert.Equal("Beta", resultingEntities[1].Name);
        }
    }

    // -------------------------------------------------------------------------
    // BasicQueryBuilder<TEntity>.WithOffsetPagination tests
    // -------------------------------------------------------------------------

    public sealed class WithOffsetPaginationTests
    {
        [Fact]
        public void WithOffsetPagination_GivenFirstPage_SkipsNoneAndTakesPageSizeItems()
        {
            // Arrange
            List<BasicQueryTestEntity> entities = CreateFourEntities();
            IQueryable<BasicQueryTestEntity> sourceQuery = entities.AsQueryable();

            BasicQueryBuilder<BasicQueryTestEntity> builder = new(sourceQuery);
            PageOptions options = new()
            {
                Page = 1,
                Size = 2
            };

            // Act
            BasicQueryBuilder<BasicQueryTestEntity> result = builder.WithOffsetPagination(options);

            List<int> resultingIds = result.Query.Select(e => e.Id).ToList();

            // Assert
            Assert.Same(builder, result);
            Assert.Equal([
                1,
                2
            ], resultingIds);
        }

        [Fact]
        public void WithOffsetPagination_GivenSecondPage_SkipsFirstPageAndTakesNextPageSizeItems()
        {
            // Arrange
            List<BasicQueryTestEntity> entities = CreateFourEntities();
            IQueryable<BasicQueryTestEntity> sourceQuery = entities.AsQueryable();

            BasicQueryBuilder<BasicQueryTestEntity> builder = new(sourceQuery);
            PageOptions options = new()
            {
                Page = 2,
                Size = 2
            };

            // Act
            BasicQueryBuilder<BasicQueryTestEntity> result = builder.WithOffsetPagination(options);

            List<int> resultingIds = result.Query.Select(e => e.Id).ToList();

            // Assert
            Assert.Same(builder, result);
            Assert.Equal([
                3,
                4
            ], resultingIds);
        }

        [Fact]
        public void WithOffsetPagination_GivenPageSizeLargerThanRemainingItems_ReturnsOnlyRemainingItems()
        {
            // Arrange
            List<BasicQueryTestEntity> entities = CreateFourEntities();
            IQueryable<BasicQueryTestEntity> sourceQuery = entities.AsQueryable();

            BasicQueryBuilder<BasicQueryTestEntity> builder = new(sourceQuery);
            PageOptions options = new()
            {
                Page = 2,
                Size = 3
            };

            // Act
            BasicQueryBuilder<BasicQueryTestEntity> result = builder.WithOffsetPagination(options);

            List<int> resultingIds = result.Query.Select(e => e.Id).ToList();

            // Assert
            Assert.Same(builder, result);
            Assert.Equal([4], resultingIds);
        }
    }
}
