using DavidGroup.Core.DataAccess.ElasticSearch;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Moq;

using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace DavidGroup.Core.DataAccessTests.ElasticSearch;

public class ElasticSearchHealthCheckTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static HealthCheckContext CreateContext() =>
        new()
        {
            Registration = new HealthCheckRegistration(
                name: "elasticsearch",
                instance: new Mock<IHealthCheck>().Object,
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };

    private static ElasticsearchClient CreateClientWithResponse(bool isValid)
    {
        // The Elastic .NET client is sealed, so we drive it through an in-memory
        // transport that returns a 200 (valid) or 503 (invalid) HTTP response.
        ElasticsearchClientSettings settings = new(
            new InMemoryRequestInvoker(
                statusCode: isValid ? 200 : 503,
                responseBody: [],
                headers: new Dictionary<string, IEnumerable<string>> { { "x-elastic-product", ["Elasticsearch"] } }
            )
        );

        return new ElasticsearchClient(settings);
    }

    // -------------------------------------------------------------------------
    // Healthy path
    // -------------------------------------------------------------------------

    public class HealthyPath
    {
        [Fact]
        public async Task Returns_Healthy_When_Ping_Succeeds()
        {
            // Arrange
            ElasticsearchClient client = CreateClientWithResponse(isValid: true);
            ElasticSearchHealthCheck healthCheck = new(client);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task Returns_Healthy_Description_When_Ping_Succeeds()
        {
            // Arrange
            ElasticsearchClient client = CreateClientWithResponse(isValid: true);
            ElasticSearchHealthCheck healthCheck = new(client);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.Equal("Elasticsearch is healthy", result.Description);
        }

        [Fact]
        public async Task Does_Not_Set_Exception_When_Ping_Succeeds()
        {
            // Arrange
            ElasticsearchClient client = CreateClientWithResponse(isValid: true);
            ElasticSearchHealthCheck healthCheck = new(client);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.Null(result.Exception);
        }
    }

    // -------------------------------------------------------------------------
    // Unhealthy path — invalid response (no exception)
    // -------------------------------------------------------------------------

    public class UnhealthyPath
    {
        [Fact]
        public async Task Returns_Unhealthy_When_Ping_Returns_Invalid_Response()
        {
            // Arrange
            ElasticsearchClient client = CreateClientWithResponse(isValid: false);
            ElasticSearchHealthCheck healthCheck = new(client);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task Returns_Unhealthy_Description_When_Ping_Returns_Invalid_Response()
        {
            // Arrange
            ElasticsearchClient client = CreateClientWithResponse(isValid: false);
            ElasticSearchHealthCheck healthCheck = new(client);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.Equal("Elasticsearch ping failed", result.Description);
        }

        [Fact]
        public async Task Does_Not_Set_Exception_When_Ping_Returns_Invalid_Response()
        {
            // Arrange
            ElasticsearchClient client = CreateClientWithResponse(isValid: false);
            ElasticSearchHealthCheck healthCheck = new(client);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.Null(result.Exception);
        }
    }

    // -------------------------------------------------------------------------
    // Unhealthy path — exception thrown
    // -------------------------------------------------------------------------

    public class UnhealthyPathWithException
    {
        [Fact]
        public async Task Returns_Unhealthy_When_Ping_Throws()
        {
            // Arrange
            Mock<ElasticsearchClient> clientMock = new();

            clientMock.Setup(x => x.PingAsync(cancellationToken: It.IsAny<CancellationToken>()))
                .Throws(new Exception("connection refused"));

            ElasticSearchHealthCheck healthCheck = new(clientMock.Object);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task Returns_Unhealthy_Description_When_Ping_Throws()
        {
            // Arrange
            Mock<ElasticsearchClient> clientMock = new();

            clientMock.Setup(x => x.PingAsync(cancellationToken: It.IsAny<CancellationToken>()))
                .Throws(new Exception("connection refused"));

            ElasticSearchHealthCheck healthCheck = new(clientMock.Object);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.Equal("Elasticsearch ping exception", result.Description);
        }

        [Fact]
        public async Task Attaches_Exception_To_Result_When_Ping_Throws()
        {
            // Arrange
            Exception thrown = new("connection refused");

            Mock<ElasticsearchClient> clientMock = new();

            clientMock.Setup(x => x.PingAsync(cancellationToken: It.IsAny<CancellationToken>()))
                .Throws(thrown);

            ElasticSearchHealthCheck healthCheck = new(clientMock.Object);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.Same(thrown, result.Exception);
        }

        [Fact]
        public async Task Preserves_Original_Exception_Type_When_Ping_Throws()
        {
            // Arrange
            Mock<ElasticsearchClient> clientMock = new();

            clientMock.Setup(x => x.PingAsync(cancellationToken: It.IsAny<CancellationToken>()))
                .Throws(new TimeoutException("timed out"));

            ElasticSearchHealthCheck healthCheck = new(clientMock.Object);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.IsType<TimeoutException>(result.Exception);
        }

        // -------------------------------------------------------------------------
        // Cancellation
        // -------------------------------------------------------------------------

        [Fact]
        public async Task Passes_CancellationToken_Through_To_Client()
        {
            // Arrange
            using CancellationTokenSource cts = new();

            Mock<ElasticsearchClient> client = new();

            // ReSharper disable AccessToDisposedClosure
            client.Setup(c => c.PingAsync(cts.Token))
                .ReturnsAsync(new PingResponse());

            ElasticSearchHealthCheck healthCheck = new(client.Object);

            // Act
            await healthCheck.CheckHealthAsync(CreateContext(), cts.Token);

            // Assert
            client.Verify(c => c.PingAsync(cts.Token), Times.Once);
            // ReSharper restore AccessToDisposedClosure
        }

        [Fact]
        public async Task Returns_Unhealthy_When_Ping_Throws_OperationCanceledException()
        {
            // Arrange
            Mock<ElasticsearchClient> client = new();

            client.Setup(c => c.PingAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("cancelled"));

            ElasticSearchHealthCheck healthCheck = new(client.Object);

            // Act
            HealthCheckResult result = await healthCheck.CheckHealthAsync(CreateContext());

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.IsType<OperationCanceledException>(result.Exception);
        }
    }
}
