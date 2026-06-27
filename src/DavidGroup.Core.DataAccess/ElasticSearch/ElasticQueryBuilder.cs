using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace DavidGroup.Core.DataAccess.ElasticSearch;

/// <summary>
/// Provides helper methods for building common Elasticsearch queries.
/// </summary>
public static class ElasticQueryBuilder
{
    /// <summary>
    /// Builds a query that matches documents where a field has a specific value
    /// or where the field does not exist at all.
    /// </summary>
    /// <param name="field">The name of the field to evaluate.</param>
    /// <param name="value">The value that the field should match.</param>
    /// <returns>
    /// A <see cref="Query"/> representing a <see cref="BoolQuery"/> with a <c>should</c> condition
    /// that allows either a matching term or absence of the field.
    /// </returns>
    /// <example>
    /// This query is equivalent to:
    /// <code>
    /// field == value OR field does not exist
    /// </code>
    /// </example>
    public static Query EitherShouldHavePropertyWithValueOrNot(string field, string value)
    {
        return new BoolQuery
        {
            Should =
            [
                new TermQuery { Field = field, Value = value },
                new BoolQuery
                {
                    MustNot =
                    [
                        new ExistsQuery { Field = new Field(field) }
                    ]
                }
            ],
            MinimumShouldMatch = 1
        };
    }

    /// <summary>
    /// Builds a query for documents where the specified field
    /// is within the given range.
    /// </summary>
    /// <param name="field">The field name to apply the range query to.</param>
    /// <param name="from">The lower bound of the range (inclusive). Pass <c>null</c> to omit.</param>
    /// <param name="to">The upper bound of the range (inclusive). Pass <c>null</c> to omit.</param>
    /// <returns>
    /// A <see cref="Query"/> representing the applicable range conditions.
    /// </returns>
    public static Query EitherShouldHavePropertyInRangeOrNot(string field, Number? from, Number? to)
    {
        if (from is null && to is null)
            throw new ArgumentException(ElasticSearchErrorMessages.EitherArgumentMustBeSpecified);

        return new NumberRangeQuery(new Field(field)) { Gte = from, Lte = to };
    }

    /// <summary>
    /// Builds a range query for documents where the specified field
    /// is within the given date range.
    /// </summary>
    /// <param name="field">The field name to apply the date range query to.</param>
    /// <param name="from">The start date (inclusive). Pass <c>null</c> to omit.</param>
    /// <param name="to">The end date (inclusive). Pass <c>null</c> to omit.</param>
    /// <returns>
    /// A <see cref="Query"/> representing the applicable range conditions.
    /// </returns>
    public static Query EitherShouldHavePropertyInRangeOrNot(string field, DateTime? from, DateTime? to)
    {
        if (from is null && to is null)
            throw new ArgumentException(ElasticSearchErrorMessages.EitherArgumentMustBeSpecified);

        return new DateRangeQuery(new Field(field)) { Gte = from, Lte = to };
    }

    /// <summary>
    /// Builds a geospatial bounding box query that filters documents whose
    /// location field falls within the specified rectangular area.
    /// </summary>
    /// <param name="field">The name of the geo-point field to query.</param>
    /// <param name="topLeftLat">The latitude of the top-left corner of the bounding box.</param>
    /// <param name="topLeftLon">The longitude of the top-left corner of the bounding box.</param>
    /// <param name="bottomRightLat">The latitude of the bottom-right corner of the bounding box.</param>
    /// <param name="bottomRightLon">The longitude of the bottom-right corner of the bounding box.</param>
    /// <returns>
    /// A <see cref="Query"/> representing a <see cref="GeoBoundingBoxQuery"/> for the specified coordinates.
    /// </returns>
    public static Query ShouldBeInBoundingBox(
        string field,
        double topLeftLat, double topLeftLon,
        double bottomRightLat, double bottomRightLon)
    {
        return new GeoBoundingBoxQuery
        {
            Field = new Field(field),
            BoundingBox = GeoBounds.TopLeftBottomRight(
                new TopLeftBottomRightGeoBounds
                {
                    TopLeft = GeoLocation.LatitudeLongitude(
                        new LatLonGeoLocation { Lat = topLeftLat, Lon = topLeftLon }),
                    BottomRight = GeoLocation.LatitudeLongitude(
                        new LatLonGeoLocation { Lat = bottomRightLat, Lon = bottomRightLon })
                })
        };
    }

    /// <summary>
    /// Builds a sorting option that orders search results by geographic distance
    /// from the specified latitude and longitude, from nearest to farthest.
    /// </summary>
    /// <param name="field">The name of the geo-point field used for distance calculation.</param>
    /// <param name="lat">The latitude of the reference point.</param>
    /// <param name="lon">The longitude of the reference point.</param>
    /// <returns>
    /// A <see cref="SortOptions"/> instance configured for ascending distance sorting.
    /// </returns>
    /// <remarks>
    /// This uses the <see cref="GeoDistanceType.Arc"/> model and sorts in meters.
    /// </remarks>
    public static SortOptions NearestToFurthestSort(string field, double lat, double lon)
    {
        return new SortOptions
        {
            GeoDistance = new GeoDistanceSort
            {
                Field = new Field(field),
                Location =
                [
                    GeoLocation.LatitudeLongitude(new LatLonGeoLocation { Lat = lat, Lon = lon })
                ],
                Order = SortOrder.Asc,
                Unit = DistanceUnit.Meters,
                Mode = SortMode.Min,
                DistanceType = GeoDistanceType.Arc,
                IgnoreUnmapped = false
            }
        };
    }

    /// <summary>
    /// Creates a <see cref="PointInTimeReference"/> instance used to maintain
    /// search consistency across multiple Elasticsearch queries.
    /// </summary>
    /// <param name="pit">The point-in-time ID obtained from Elasticsearch.</param>
    /// <param name="keepAlive">The duration for which the PIT should remain active (e.g., "1m").</param>
    /// <returns>
    /// A new <see cref="PointInTimeReference"/> initialized with the specified parameters.
    /// </returns>
    /// <remarks>
    /// A PIT (Point-In-Time) is useful for ensuring consistent pagination in Elasticsearch
    /// without being affected by index updates between requests.
    /// </remarks>
    public static PointInTimeReference CreatePitReference(string pit, string keepAlive)
    {
        return new PointInTimeReference { Id = pit, KeepAlive = keepAlive };
    }
}
