using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Builders;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Builders;

public static class OrderingSpecificationTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private class OrderingTestEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public OrderingTestEntityAddress Address { get; set; } = new();
    }

    private class OrderingTestEntityAddress
    {
        public string City { get; set; } = string.Empty;
    }

    private static List<OrderingTestEntity> CreateSampleEntities()
    {
        return
        [
            new OrderingTestEntity
            {
                Id = 1,
                Name = "Charlie",
                Age = 30,
                CreatedAtUtc = new DateTime(2024, 1, 3),
                Address = new OrderingTestEntityAddress
                {
                    City = "Berlin"
                }
            },

            new OrderingTestEntity
            {
                Id = 2,
                Name = "Alice",
                Age = 25,
                CreatedAtUtc = new DateTime(2024, 1, 1),
                Address = new OrderingTestEntityAddress
                {
                    City = "Amsterdam"
                }
            },

            new OrderingTestEntity
            {
                Id = 3,
                Name = "Bob",
                Age = 35,
                CreatedAtUtc = new DateTime(2024, 1, 2),
                Address = new OrderingTestEntityAddress
                {
                    City = "Chicago"
                }
            }
        ];
    }

    // -------------------------------------------------------------------------
    // OrderingSpecification<TEntity> constructor tests
    // -------------------------------------------------------------------------

    public sealed class ConstructionTests
    {
        [Fact]
        public void Constructor_GivenOrderByExpressionAndDescendingFlag_AssignsBothProperties()
        {
            // Arrange
            Expression<Func<OrderingTestEntity, object>> orderByExpression = entity => entity.Name;
            const bool isDescending = true;

            // Act
            OrderingSpecification<OrderingTestEntity> specification = new(orderByExpression, isDescending);

            // Assert
            Assert.Same(orderByExpression, specification.OrderBy);
            Assert.True(specification.IsDescending);
        }
    }

    // -------------------------------------------------------------------------
    // OrderingSpecification<TEntity>.Parse tests
    // -------------------------------------------------------------------------

    public sealed class ParseTests
    {
        [Fact]
        public void Parse_SingleAscendingPropertyName_ReturnsSuccessWithOneAscendingSpecification()
        {
            // Arrange
            const string orderBy = "Name";
            IReadOnlyList<Expression<Func<OrderingTestEntity, object>>>? allowedProperties = null;

            // Act
            OperationResult<IReadOnlyList<OrderingSpecification<OrderingTestEntity>>> result =
                OrderingSpecification<OrderingTestEntity>.Parse(orderBy, allowedProperties);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.False(result.Value[0].IsDescending);
        }

        [Fact]
        public void Parse_SinglePropertyWithAscSuffix_ReturnsAscendingSpecification()
        {
            // Arrange
            const string orderBy = "Name asc";
            IReadOnlyList<Expression<Func<OrderingTestEntity, object>>>? allowedProperties = null;

            // Act
            OperationResult<IReadOnlyList<OrderingSpecification<OrderingTestEntity>>> result =
                OrderingSpecification<OrderingTestEntity>.Parse(orderBy, allowedProperties);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.Value);
            Assert.False(result.Value[0].IsDescending);
        }

        [Fact]
        public void Parse_SinglePropertyWithDescSuffix_ReturnsDescendingSpecification()
        {
            // Arrange
            const string orderBy = "Name desc";
            IReadOnlyList<Expression<Func<OrderingTestEntity, object>>>? allowedProperties = null;

            // Act
            OperationResult<IReadOnlyList<OrderingSpecification<OrderingTestEntity>>> result =
                OrderingSpecification<OrderingTestEntity>.Parse(orderBy, allowedProperties);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.Value);
            Assert.True(result.Value[0].IsDescending);
        }

        [Fact]
        public void Parse_MultipleCommaSeparatedProperties_ReturnsOneSpecificationPerProperty()
        {
            // Arrange
            const string orderBy = "Name desc, CreatedAtUtc, Id asc";
            IReadOnlyList<Expression<Func<OrderingTestEntity, object>>>? allowedProperties = null;

            // Act
            OperationResult<IReadOnlyList<OrderingSpecification<OrderingTestEntity>>> result =
                OrderingSpecification<OrderingTestEntity>.Parse(orderBy, allowedProperties);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(3, result.Value.Count);
            Assert.True(result.Value[0].IsDescending);
            Assert.False(result.Value[1].IsDescending);
            Assert.False(result.Value[2].IsDescending);
        }

        [Fact]
        public void Parse_WhitespaceOnlySegmentBetweenCommas_IsSkippedWithoutError()
        {
            // Arrange
            const string orderBy = "Name, , CreatedAtUtc";
            IReadOnlyList<Expression<Func<OrderingTestEntity, object>>>? allowedProperties = null;

            // Act
            OperationResult<IReadOnlyList<OrderingSpecification<OrderingTestEntity>>> result =
                OrderingSpecification<OrderingTestEntity>.Parse(orderBy, allowedProperties);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(2, result.Value.Count);
        }

        [Fact]
        public void Parse_SurroundingWhitespaceAroundTerms_IsTrimmedBeforeEvaluation()
        {
            // Arrange
            const string orderBy = "   Name   ,   CreatedAtUtc desc   ";
            IReadOnlyList<Expression<Func<OrderingTestEntity, object>>>? allowedProperties = null;

            // Act
            OperationResult<IReadOnlyList<OrderingSpecification<OrderingTestEntity>>> result =
                OrderingSpecification<OrderingTestEntity>.Parse(orderBy, allowedProperties);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(2, result.Value.Count);
            Assert.False(result.Value[0].IsDescending);
            Assert.True(result.Value[1].IsDescending);
        }

        [Fact]
        public void Parse_PropertyPresentInAllowedList_ReturnsSuccessResult()
        {
            // Arrange
            const string orderBy = "Name";
            List<Expression<Func<OrderingTestEntity, object>>> allowedProperties =
            [
                entity => entity.Name,
                entity => entity.Age
            ];

            // Act
            OperationResult<IReadOnlyList<OrderingSpecification<OrderingTestEntity>>> result =
                OrderingSpecification<OrderingTestEntity>.Parse(orderBy, allowedProperties);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.Value);
        }

        [Fact]
        public void Parse_NestedPropertyPathPresentInAllowedList_ReturnsSuccessResult()
        {
            // Arrange
            const string orderBy = "Address.City";
            List<Expression<Func<OrderingTestEntity, object>>> allowedProperties =
                [entity => entity.Address.City];

            // Act
            OperationResult<IReadOnlyList<OrderingSpecification<OrderingTestEntity>>> result =
                OrderingSpecification<OrderingTestEntity>.Parse(orderBy, allowedProperties);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Single(result.Value);
        }

        [Fact]
        public void Parse_PropertyMissingFromAllowedList_ReturnsFailureResult()
        {
            // Arrange
            const string orderBy = "Age";
            List<Expression<Func<OrderingTestEntity, object>>> allowedProperties =
                [entity => entity.Name];

            // Act
            OperationResult<IReadOnlyList<OrderingSpecification<OrderingTestEntity>>> result =
                OrderingSpecification<OrderingTestEntity>.Parse(orderBy, allowedProperties);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal($"Ordering parameter '{orderBy}' is not allowed.", result.Messages[0].Message);
        }

        [Fact]
        public void Parse_PropertyNotFoundOnEntityWithoutAllowedList_ReturnsFailureResult()
        {
            // Arrange
            const string orderBy = "DoesNotExistOnEntity";
            IReadOnlyList<Expression<Func<OrderingTestEntity, object>>>? allowedProperties = null;

            // Act
            OperationResult<IReadOnlyList<OrderingSpecification<OrderingTestEntity>>> result =
                OrderingSpecification<OrderingTestEntity>.Parse(orderBy, allowedProperties);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal($"Field '{orderBy}' does not not exist.", result.Messages[0].Message);
        }
    }

    // -------------------------------------------------------------------------
    // OrderingSpecification<TEntity>.Apply tests
    // -------------------------------------------------------------------------

    public sealed class ApplyTests
    {
        [Fact]
        public void Apply_SingleAscendingSpecification_OrdersQueryAscendingByProperty()
        {
            // Arrange
            List<OrderingTestEntity> entities = CreateSampleEntities();
            IQueryable<OrderingTestEntity> query = entities.AsQueryable();
            List<OrderingSpecification<OrderingTestEntity>> specifications =
                [new(e => e.Name, false)];

            // Act
            IOrderedQueryable<OrderingTestEntity> orderedQuery =
                OrderingSpecification<OrderingTestEntity>.Apply(query, specifications);

            List<string> orderedNames = orderedQuery.Select(e => e.Name).ToList();

            // Assert
            Assert.Equal([
                "Alice",
                "Bob",
                "Charlie"
            ], orderedNames);
        }

        [Fact]
        public void Apply_SingleDescendingSpecification_OrdersQueryDescendingByProperty()
        {
            // Arrange
            List<OrderingTestEntity> entities = CreateSampleEntities();
            IQueryable<OrderingTestEntity> query = entities.AsQueryable();
            List<OrderingSpecification<OrderingTestEntity>> specifications =
                [new(e => e.Name, true)];

            // Act
            IOrderedQueryable<OrderingTestEntity> orderedQuery =
                OrderingSpecification<OrderingTestEntity>.Apply(query, specifications);

            List<string> orderedNames = orderedQuery.Select(e => e.Name).ToList();

            // Assert
            Assert.Equal([
                "Charlie",
                "Bob",
                "Alice"
            ], orderedNames);
        }

        [Fact]
        public void Apply_MultipleSpecifications_AppliesSecondSpecificationAsThenBy()
        {
            // Arrange
            List<OrderingTestEntity> entities =
            [
                new()
                {
                    Id = 1,
                    Name = "Zack",
                    Age = 30,
                    CreatedAtUtc = DateTime.UtcNow,
                    Address = new OrderingTestEntityAddress()
                },

                new()
                {
                    Id = 2,
                    Name = "Amy",
                    Age = 30,
                    CreatedAtUtc = DateTime.UtcNow,
                    Address = new OrderingTestEntityAddress()
                },

                new()
                {
                    Id = 3,
                    Name = "Bob",
                    Age = 20,
                    CreatedAtUtc = DateTime.UtcNow,
                    Address = new OrderingTestEntityAddress()
                }
            ];

            IQueryable<OrderingTestEntity> query = entities.AsQueryable();
            List<OrderingSpecification<OrderingTestEntity>> specifications =
            [
                new(e => e.Age, false),
                new(e => e.Name, false)
            ];

            // Act
            IOrderedQueryable<OrderingTestEntity> orderedQuery =
                OrderingSpecification<OrderingTestEntity>.Apply(query, specifications);

            List<string> orderedNames = orderedQuery.Select(e => e.Name).ToList();

            // Assert
            Assert.Equal([
                "Bob",
                "Amy",
                "Zack"
            ], orderedNames);
        }

        [Fact]
        public void Apply_MixedAscendingAndDescendingSpecifications_AppliesEachDirectionIndependently()
        {
            // Arrange
            List<OrderingTestEntity> entities =
            [
                new()
                {
                    Id = 1,
                    Name = "Zack",
                    Age = 20,
                    CreatedAtUtc = DateTime.UtcNow,
                    Address = new OrderingTestEntityAddress()
                },

                new()
                {
                    Id = 2,
                    Name = "Amy",
                    Age = 20,
                    CreatedAtUtc = DateTime.UtcNow,
                    Address = new OrderingTestEntityAddress()
                },

                new()
                {
                    Id = 3,
                    Name = "Bob",
                    Age = 30,
                    CreatedAtUtc = DateTime.UtcNow,
                    Address = new OrderingTestEntityAddress()
                }
            ];

            IQueryable<OrderingTestEntity> query = entities.AsQueryable();
            List<OrderingSpecification<OrderingTestEntity>> specifications =
            [
                new(e => e.Age, false),
                new(e => e.Name, true)
            ];

            // Act
            IOrderedQueryable<OrderingTestEntity> orderedQuery =
                OrderingSpecification<OrderingTestEntity>.Apply(query, specifications);

            List<string> orderedNames = orderedQuery.Select(e => e.Name).ToList();

            // Assert
            Assert.Equal([
                "Zack",
                "Amy",
                "Bob"
            ], orderedNames);
        }

        [Fact]
        public void Apply_EmptySpecificationsList_ThrowsInvalidOperationException()
        {
            // Arrange
            List<OrderingTestEntity> entities = CreateSampleEntities();
            IQueryable<OrderingTestEntity> query = entities.AsQueryable();

            // Act & Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(()
                => OrderingSpecification<OrderingTestEntity>.Apply(query, []));

            Assert.Equal("No ordering specifications were found.", ex.Message);
        }

        [Fact]
        public void Apply_ThreeSpecifications_ChainsOrderByThenByThenBy()
        {
            // Arrange
            List<OrderingTestEntity> entities =
            [
                new()
                {
                    Id = 1,
                    Name = "Same",
                    Age = 20,
                    CreatedAtUtc = new DateTime(2024, 3, 1),
                    Address = new OrderingTestEntityAddress()
                },

                new()
                {
                    Id = 2,
                    Name = "Same",
                    Age = 20,
                    CreatedAtUtc = new DateTime(2024, 1, 1),
                    Address = new OrderingTestEntityAddress()
                },

                new()
                {
                    Id = 3,
                    Name = "Same",
                    Age = 20,
                    CreatedAtUtc = new DateTime(2024, 2, 1),
                    Address = new OrderingTestEntityAddress()
                }
            ];
            IQueryable<OrderingTestEntity> query = entities.AsQueryable();
            List<OrderingSpecification<OrderingTestEntity>> specifications =
            [
                new(e => e.Age, false),
                new(e => e.Name, false),
                new(e => e.CreatedAtUtc, false)
            ];

            // Act
            IOrderedQueryable<OrderingTestEntity> orderedQuery =
                OrderingSpecification<OrderingTestEntity>.Apply(query, specifications);

            List<int> orderedIds = orderedQuery.Select(e => e.Id).ToList();

            // Assert
            Assert.Equal([
                2,
                3,
                1
            ], orderedIds);
        }
    }
}
