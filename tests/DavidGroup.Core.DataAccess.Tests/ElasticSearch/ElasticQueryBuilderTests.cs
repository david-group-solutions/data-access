using DavidGroup.Core.DataAccess.ElasticSearch;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace DavidGroup.Core.DataAccess.Tests.ElasticSearch;

public static class ElasticQueryBuilderTests
{
    // -------------------------------------------------------------------------
    // EitherShouldHavePropertyWithValueOrNot
    // -------------------------------------------------------------------------

    public class EitherShouldHavePropertyWithValueOrNotTests
    {
        [Fact]
        public void Returns_BoolQuery()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyWithValueOrNot("status", "active");

            // Assert
            Assert.IsType<BoolQuery>(result.Bool);
        }

        [Fact]
        public void BoolQuery_Has_Two_Should_Clauses()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyWithValueOrNot("status", "active");

            // Assert
            Assert.Equal(2, result.Bool!.Should!.Count);
        }

        [Fact]
        public void MinimumShouldMatch_Is_One()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyWithValueOrNot("status", "active");

            // Assert
            Assert.Equal(1, result.Bool!.MinimumShouldMatch?.Value1);
        }

        [Fact]
        public void First_Should_Clause_Is_TermQuery_With_Correct_Field_And_Value()
        {
            // Arrange
            const string field = "status";
            const string value = "active";

            // Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyWithValueOrNot(field, value);

            // Assert
            List<Query> shouldClauses = result.Bool!.Should!.ToList();
            TermQuery termQuery = shouldClauses[0].Term!;

            Assert.NotNull(termQuery);
            Assert.Equal(field, termQuery.Field.ToString());
            Assert.Equal(value, termQuery.Value.ToString());
        }

        [Fact]
        public void Second_Should_Clause_Is_BoolQuery_With_MustNot_ExistsQuery()
        {
            // Arrange
            const string field = "status";

            // Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyWithValueOrNot(field, "active");

            // Assert
            List<Query> shouldClauses = result.Bool!.Should!.ToList();
            BoolQuery innerBool = shouldClauses[1].Bool!;

            Assert.NotNull(innerBool);
            List<Query> mustNotClauses = innerBool.MustNot!.ToList();
            Assert.Single(mustNotClauses);

            ExistsQuery existsQuery = mustNotClauses[0].Exists!;
            Assert.NotNull(existsQuery);
            Assert.Equal(field, existsQuery.Field.ToString());
        }

        [Theory]
        [InlineData("category", "books")]
        [InlineData("region", "EU")]
        [InlineData("type", "premium")]
        public void Works_With_Various_Field_And_Value_Combinations(string field, string value)
        {
            // Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyWithValueOrNot(field, value);

            // Assert
            BoolQuery boolQuery = result.Bool!;

            Assert.NotNull(boolQuery);
            Assert.NotNull(boolQuery.Should);
        }
    }

    // -------------------------------------------------------------------------
    // EitherShouldHavePropertyInRangeOrNot (Number)
    // -------------------------------------------------------------------------

    public class NumericRangeTests
    {
        [Fact]
        public void Returns_NumberRangeQuery()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot("price", 10.0, 20.0);

            // Assert
            Assert.IsType<NumberRangeQuery>(result.Range);
        }

        [Fact]
        public void Returns_Gte_Query_When_Only_From_Is_Provided()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot("price", 10.0, null);

            // Assert
            NumberRangeQuery rangeQuery = (NumberRangeQuery)result.Range!;
            Assert.Equal(10.0, rangeQuery.Gte);
            Assert.Null(rangeQuery.Lte);
        }

        [Fact]
        public void Returns_Lte_Query_When_Only_To_Is_Provided()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot("price", null, 100.0);

            // Assert
            NumberRangeQuery rangeQuery = (NumberRangeQuery)result.Range!;
            Assert.Equal(100.0, rangeQuery.Lte);
            Assert.Null(rangeQuery.Gte);
        }

        [Fact]
        public void Query_Targets_Correct_Field()
        {
            // Arrange
            const string field = "price";

            // Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot(field, 10.0, 100.0);

            // Assert
            NumberRangeQuery query = (NumberRangeQuery)result.Range!;
            Assert.Equal(field, query.Field.ToString());
        }

        [Fact]
        public void Gte_And_Lte_Values_Are_Correct_When_Both_Bounds_Provided()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot("price", 5.5, 99.9);

            // Assert
            NumberRangeQuery query = (NumberRangeQuery)result.Range!;

            Assert.Equal(5.5, query.Gte);
            Assert.Equal(99.9, query.Lte);
        }

        [Fact]
        public void ThrowsException_When_Both_Bounds_Are_Null()
        {
            // Arrange, Act, Assert
            ArgumentException ex = Assert.Throws<ArgumentException>(()
                => ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot("price", (Number?)null, (Number?)null));

            Assert.Equal(ElasticSearchErrorMessages.EitherArgumentMustBeSpecified, ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    // EitherShouldHavePropertyInRangeOrNot (DateTime)
    // -------------------------------------------------------------------------

    public class DateRangeTests
    {
        private static readonly DateTime FromDate = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ToDate = new(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Returns_NumberRangeQuery()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot("price", FromDate, ToDate);

            // Assert
            Assert.IsType<DateRangeQuery>(result.Range);
        }

        [Fact]
        public void Returns_Gte_Query_When_Only_From_Is_Provided()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot("price", FromDate, null);

            // Assert
            DateRangeQuery rangeQuery = (DateRangeQuery)result.Range!;
            Assert.NotNull(rangeQuery.Gte);
            Assert.Equal(FromDate, rangeQuery.Gte.Anchor.Value1);
            Assert.Null(rangeQuery.Lte);
        }

        [Fact]
        public void Returns_Lte_Query_When_Only_To_Is_Provided()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot("price", null, ToDate);

            // Assert
            DateRangeQuery rangeQuery = (DateRangeQuery)result.Range!;
            Assert.NotNull(rangeQuery.Lte);
            Assert.Equal(ToDate, rangeQuery.Lte.Anchor.Value1);
            Assert.Null(rangeQuery.Gte);
        }

        [Fact]
        public void Query_Targets_Correct_Field()
        {
            // Arrange
            const string field = "createdAt";

            // Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot(field, FromDate, ToDate);

            // Assert
            DateRangeQuery query = (DateRangeQuery)result.Range!;
            Assert.Equal(field, query.Field.ToString());
        }

        [Fact]
        public void Gte_And_Lte_Values_Are_Correct_When_Both_Bounds_Provided()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot("price", FromDate, ToDate);

            // Assert
            DateRangeQuery query = (DateRangeQuery)result.Range!;

            Assert.NotNull(query.Gte);
            Assert.NotNull(query.Lte);

            Assert.Equal(FromDate, query.Gte.Anchor.Value1);
            Assert.Equal(ToDate, query.Lte.Anchor.Value1);
        }

        [Fact]
        public void ThrowsException_When_Both_Bounds_Are_Null()
        {
            // Arrange, Act, Assert
            ArgumentException ex = Assert.Throws<ArgumentException>(()
                => ElasticQueryBuilder.EitherShouldHavePropertyInRangeOrNot("price", (DateTime?)null, (DateTime?)null));

            Assert.Equal(ElasticSearchErrorMessages.EitherArgumentMustBeSpecified, ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    // ShouldBeInBoundingBox
    // -------------------------------------------------------------------------

    public class ShouldBeInBoundingBoxTests
    {
        [Fact]
        public void Returns_GeoBoundingBoxQuery()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.ShouldBeInBoundingBox("location", 51.5, -0.1, 51.4, 0.0);

            // Assert
            Assert.NotNull(result.GeoBoundingBox);
        }

        [Fact]
        public void Query_Targets_Correct_Field()
        {
            // Arrange
            const string field = "location";

            // Act
            Query result = ElasticQueryBuilder.ShouldBeInBoundingBox(field, 51.5, -0.1, 51.4, 0.0);

            // Assert
            Assert.Equal(field, result.GeoBoundingBox!.Field.ToString());
        }

        [Fact]
        public void Coordinates_Are_Set_Correctly()
        {
            // Arrange, Act
            Query result = ElasticQueryBuilder.ShouldBeInBoundingBox("location", 48.9, 2.2, 48.8, 2.4);

            // Assert
            Assert.True(result.GeoBoundingBox!.BoundingBox.TryGetTopLeftBottomRight(out TopLeftBottomRightGeoBounds? bounds));

            Assert.True(bounds.TopLeft.TryGetLatitudeLongitude(out LatLonGeoLocation? topLeft));
            Assert.True(bounds.BottomRight.TryGetLatitudeLongitude(out LatLonGeoLocation? bottomRight));

            Assert.Equal(48.9, topLeft.Lat);
            Assert.Equal(2.2, topLeft.Lon);

            Assert.Equal(48.8, bottomRight.Lat);
            Assert.Equal(2.4, bottomRight.Lon);
        }
    }

    // -------------------------------------------------------------------------
    // NearestToFurthestSort
    // -------------------------------------------------------------------------

    public class NearestToFurthestSortTests
    {
        [Fact]
        public void Returns_GeoDistanceSort()
        {
            // Arrange, Act
            SortOptions result = ElasticQueryBuilder.NearestToFurthestSort("location", 51.5, -0.1);

            // Assert
            Assert.NotNull(result.GeoDistance);
        }

        [Fact]
        public void Sort_Targets_Correct_Field()
        {
            // Arrange, Act
            const string field = "location";

            // Act
            SortOptions result = ElasticQueryBuilder.NearestToFurthestSort(field, 51.5, -0.1);

            // Assert
            Assert.Equal(field, result.GeoDistance!.Field.ToString());
        }

        [Fact]
        public void Sort_Order_Is_Ascending()
        {
            // Arrange, Act
            SortOptions result = ElasticQueryBuilder.NearestToFurthestSort("location", 51.5, -0.1);

            // Assert
            Assert.Equal(SortOrder.Asc, result.GeoDistance!.Order);
        }

        [Fact]
        public void Distance_Unit_Is_Meters()
        {
            // Arrange, Act
            SortOptions result = ElasticQueryBuilder.NearestToFurthestSort("location", 51.5, -0.1);

            // Assert
            Assert.Equal(DistanceUnit.Meters, result.GeoDistance!.Unit);
        }

        [Fact]
        public void Sort_Mode_Is_Min()
        {
            // Arrange, Act
            SortOptions result = ElasticQueryBuilder.NearestToFurthestSort("location", 51.5, -0.1);

            // Assert
            Assert.Equal(SortMode.Min, result.GeoDistance!.Mode);
        }

        [Fact]
        public void Distance_Type_Is_Arc()
        {
            // Arrange, Act
            SortOptions result = ElasticQueryBuilder.NearestToFurthestSort("location", 51.5, -0.1);

            // Assert
            Assert.Equal(GeoDistanceType.Arc, result.GeoDistance!.DistanceType);
        }

        [Fact]
        public void IgnoreUnmapped_Is_False()
        {
            // Arrange, Act
            SortOptions result = ElasticQueryBuilder.NearestToFurthestSort("location", 51.5, -0.1);

            // Assert
            Assert.False(result.GeoDistance!.IgnoreUnmapped);
        }

        [Fact]
        public void Reference_Point_Coordinates_Are_Correct()
        {
            // Arrange, Act
            SortOptions result = ElasticQueryBuilder.NearestToFurthestSort("location", 48.8566, 2.3522);

            // Assert
            Assert.True(result.GeoDistance!.Location.First().TryGetLatitudeLongitude(out LatLonGeoLocation? point));

            Assert.Equal(48.8566, point.Lat);
            Assert.Equal(2.3522, point.Lon);
        }

        [Fact]
        public void Has_Exactly_One_Reference_Location()
        {
            // Arrange, Act
            SortOptions result = ElasticQueryBuilder.NearestToFurthestSort("location", 0.0, 0.0);

            // Assert
            Assert.Single(result.GeoDistance!.Location);
        }

        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(90.0, 180.0)]
        [InlineData(-90.0, -180.0)]
        public void Works_With_Boundary_Coordinates(double lat, double lon)
        {
            // Arrange, Act
            SortOptions result = ElasticQueryBuilder.NearestToFurthestSort("geo", lat, lon);

            // Assert
            Assert.NotNull(result.GeoDistance);
        }
    }

    // -------------------------------------------------------------------------
    // CreatePitReference
    // -------------------------------------------------------------------------

    public class CreatePitReferenceTests
    {
        [Fact]
        public void Returns_PointInTimeReference()
        {
            // Arrange, Act
            PointInTimeReference result = ElasticQueryBuilder.CreatePitReference("abc123", "1m");

            // Assert
            Assert.IsType<PointInTimeReference>(result);
        }

        [Fact]
        public void Sets_Id_Correctly()
        {
            // Arrange
            const string pitId = "my-pit-id-xyz";

            // Act
            PointInTimeReference result = ElasticQueryBuilder.CreatePitReference(pitId, "1m");

            // Assert
            Assert.Equal(pitId, result.Id);
        }

        [Fact]
        public void Sets_KeepAlive_Correctly()
        {
            // Arrange
            const string keepAlive = "5m";

            // Act
            PointInTimeReference result = ElasticQueryBuilder.CreatePitReference("some-id", keepAlive);

            // Assert
            Assert.NotNull(result.KeepAlive);
            Assert.Equal(keepAlive, result.KeepAlive.ToString());
        }

        [Theory]
        [InlineData("pit-001", "1m")]
        [InlineData("pit-002", "5m")]
        [InlineData("pit-003", "30s")]
        [InlineData("pit-004", "1h")]
        public void Works_With_Various_Ids_And_KeepAlive_Values(string pitId, string keepAlive)
        {
            // Act
            PointInTimeReference result = ElasticQueryBuilder.CreatePitReference(pitId, keepAlive);

            // Assert
            Assert.Equal(pitId, result.Id);
            Assert.NotNull(result.KeepAlive);
            Assert.Equal(keepAlive, result.KeepAlive.ToString());
        }

        [Fact]
        public void Does_Not_Mutate_Inputs()
        {
            // Arrange
            const string pitId = "stable-id";
            const string keepAlive = "2m";

            // Act
            _ = ElasticQueryBuilder.CreatePitReference(pitId, keepAlive);

            // Assert
            Assert.Equal("stable-id", pitId);
            Assert.Equal("2m", keepAlive);
        }
    }
}
