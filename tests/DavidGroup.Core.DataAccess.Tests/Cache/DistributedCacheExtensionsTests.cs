using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using DavidGroup.Core.DataAccess.Cache;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DavidGroup.Core.DataAccess.Tests.Cache;

file class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

file static class CacheSerializerHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static byte[] Serialize<T>(T value) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, SerializerOptions));
}

file static class CacheFactory
{
    public static IDistributedCache CreateMemoryCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public static class DistributedCacheExtensionsTests
{
    // -------------------------------------------------------------------------
    // SetAsync Tests
    // -------------------------------------------------------------------------
    public class SetAsyncTests
    {
        [Fact]
        public async Task SetAsync_WhenCalledWithoutOptions_ValueCanBeRetrievedFromCache()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            Product product = new()
            {
                Id = 1,
                Name = "Widget",
                Price = 9.99m
            };

            const string key = "product:1";

            // Act
            await cache.SetAsync(key, product);

            Product? result = await cache.GetAsync<Product>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(product.Id, result.Id);
            Assert.Equal(product.Name, result.Name);
            Assert.Equal(product.Price, result.Price);
        }

        [Fact]
        public async Task SetAsync_WhenCalledWithExpiredOptions_ValueIsNoLongerRetrievable()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            Product product = new()
            {
                Id = 5,
                Name = "Ephemeral",
                Price = 0.01m
            };

            const string key = "product:5";

            DistributedCacheEntryOptions options = new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(1)
            };

            // Act
            await cache.SetAsync(key, product, options);

            await Task.Delay(50);

            Product? result = await cache.GetAsync<Product>(key);

            // Assert
            Assert.Null(result);
        }
    }

    // -------------------------------------------------------------------------
    // GetAsync Tests
    // -------------------------------------------------------------------------
    public class GetAsyncTests
    {
        [Fact]
        public async Task GetAsync_WhenKeyExists_ReturnsDeserializedObject()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            Product product = new()
            {
                Id = 1,
                Name = "Widget",
                Price = 9.99m
            };

            const string key = "product:1";

            await cache.SetAsync(key, CacheSerializerHelper.Serialize(product));

            // Act
            Product? result = await cache.GetAsync<Product>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(product.Id, result.Id);
            Assert.Equal(product.Name, result.Name);
            Assert.Equal(product.Price, result.Price);
        }

        [Fact]
        public async Task GetAsync_WhenKeyDoesNotExist_ReturnsDefault()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            const string key = "product:missing";

            // Act
            Product? result = await cache.GetAsync<Product>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_WhenKeyExistsForValueType_ReturnsDeserializedValue()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            const int number = 42;
            const string key = "number:42";

            await cache.SetAsync(key, CacheSerializerHelper.Serialize(number));

            // Act
            int result = await cache.GetAsync<int>(key);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task GetAsync_WhenCalledTwiceForSameKey_ReturnsSameValue()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            Product product = new()
            {
                Id = 7,
                Name = "Repeat",
                Price = 3.33m
            };

            const string key = "product:7";

            await cache.SetAsync(key, product);

            // Act
            Product? first = await cache.GetAsync<Product>(key);
            Product? second = await cache.GetAsync<Product>(key);

            // Assert
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.Id, second.Id);
        }
    }

    // -------------------------------------------------------------------------
    // GetOrSetAsync Tests
    // -------------------------------------------------------------------------
    public class GetOrSetAsyncTests
    {
        [Fact]
        public async Task GetOrSetAsync_WhenKeyExistsInCache_ReturnsCachedValueWithoutInvokingTask()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            Product product = new()
            {
                Id = 1,
                Name = "Widget",
                Price = 9.99m
            };

            const string key = "product:1";

            await cache.SetAsync(key, product);

            bool taskInvoked = false;

            Func<Task<Product>> task = () =>
            {
                taskInvoked = true;

                return Task.FromResult(new Product
                {
                    Id = 99
                });
            };

            // Act
            Product? result = await cache.GetOrSetAsync(key, task);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(product.Id, result.Id);
            Assert.False(taskInvoked);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenKeyDoesNotExist_InvokesTaskAndStoresResult()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            Product product = new()
            {
                Id = 2,
                Name = "Gadget",
                Price = 49.99m
            };

            const string key = "product:2";

            Func<Task<Product>> task = () => Task.FromResult(product);

            // Act
            Product? result = await cache.GetOrSetAsync(key, task);

            Product? subsequent = await cache.GetAsync<Product>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(product.Id, result.Id);
            Assert.NotNull(subsequent);
            Assert.Equal(product.Id, subsequent.Id);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenTaskReturnsNull_DoesNotStoreInCache()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            const string key = "product:null";

            Func<Task<Product?>> task = () => Task.FromResult<Product?>(null);

            // Act
            Product? result = await cache.GetOrSetAsync(key, task);
            byte[]? rawBytes = await cache.GetAsync(key);

            // Assert
            Assert.Null(result);
            Assert.Null(rawBytes);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenKeyExistsInCache_DoesNotOverwriteExistingValue()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            Product original = new()
            {
                Id = 5,
                Name = "Original",
                Price = 3.50m
            };
            Product replacement = new()
            {
                Id = 99,
                Name = "Replacement",
                Price = 0m
            };

            const string key = "product:5";

            await cache.SetAsync(key, original);

            Func<Task<Product>> task = () => Task.FromResult(replacement);

            // Act
            await cache.GetOrSetAsync(key, task);

            Product? result = await cache.GetAsync<Product>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(original.Id, result.Id);
            Assert.Equal(original.Name, result.Name);
            Assert.Equal(original.Price, result.Price);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenKeyDoesNotExist_ReturnsValueProducedByTask()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            Product product = new()
            {
                Id = 6,
                Name = "Thingamajig",
                Price = 7.77m
            };

            const string key = "product:6";

            Func<Task<Product>> task = () => Task.FromResult(product);

            // Act
            Product? result = await cache.GetOrSetAsync(key, task);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(6, result.Id);
            Assert.Equal("Thingamajig", result.Name);
            Assert.Equal(7.77m, result.Price);
        }

        [Fact]
        public async Task GetOrSetAsync_WhenTaskInvokedOnlyOnce_SubsequentCallsUseCachedValue()
        {
            // Arrange
            IDistributedCache cache = CacheFactory.CreateMemoryCache();

            const string key = "product:invoke-count";

            int invocationCount = 0;

            Func<Task<Product>> task = () =>
            {
                invocationCount++;

                return Task.FromResult(new Product
                {
                    Id = invocationCount,
                    Name = "Counted"
                });
            };

            // Act
            Product? first = await cache.GetOrSetAsync(key, task);
            Product? second = await cache.GetOrSetAsync(key, task);

            // Assert
            Assert.Equal(1, invocationCount);
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(first.Id, second.Id);
        }
    }
}
