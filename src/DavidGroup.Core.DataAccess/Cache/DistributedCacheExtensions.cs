using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Caching.Distributed;

namespace DavidGroup.Core.DataAccess.Cache;

/// <summary>
/// Provides extension methods for <see cref="IDistributedCache"/> to store and retrieve
/// objects as JSON.
/// </summary>
/// <remarks>
/// These methods simplify caching complex objects in a distributed cache by automatically
/// serializing to and deserializing from JSON using <see cref="System.Text.Json"/>.
/// </remarks>
public static class DistributedCacheExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = true,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes an object of type <typeparamref name="T"/> to JSON and stores it in the distributed cache.
    /// </summary>
    /// <typeparam name="T">The type of the object to cache.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance.</param>
    /// <param name="key">The cache key under which the value will be stored.</param>
    /// <param name="value">The object to cache.</param>
    /// <param name="options">Optional <see cref="DistributedCacheEntryOptions"/> to control cache behavior.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static Task SetAsync<T>(this IDistributedCache cache, string key, T value,
        DistributedCacheEntryOptions? options = null)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, SerializerOptions));

        return cache.SetAsync(key, bytes, options ?? new DistributedCacheEntryOptions());
    }

    /// <summary>
    /// Retrieves a cached object of type <typeparamref name="T"/> from the distributed cache.
    /// </summary>
    /// <typeparam name="T">The type of the object to retrieve.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance.</param>
    /// <param name="key">The cache key used to retrieve the value.</param>
    /// <returns>
    /// A <see cref="Task{T}"/> that returns the cached object, or <c>null</c> if the key does not exist
    /// or the cached value cannot be deserialized.
    /// </returns>
    public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key)
    {
        byte[]? val = await cache.GetAsync(key);

        return val == null
            ? default
            : JsonSerializer.Deserialize<T>(val, SerializerOptions);
    }

    /// <summary>
    /// Retrieves a cached object if it exists; otherwise, executes a delegate to obtain the value,
    /// stores it in the cache, and then returns it.
    /// </summary>
    /// <typeparam name="T">The type of the object to retrieve or generate.</typeparam>
    /// <param name="cache">The <see cref="IDistributedCache"/> instance.</param>
    /// <param name="key">The cache key used to retrieve or store the value.</param>
    /// <param name="task">A delegate that asynchronously produces the value if it is not in the cache.</param>
    /// <param name="options">Optional <see cref="DistributedCacheEntryOptions"/> to control cache behavior.</param>
    /// <returns>
    /// A <see cref="Task{T}"/> representing the cached or newly generated value.
    /// </returns>
    /// <remarks>
    /// This method is useful for read-through caching patterns, ensuring that expensive
    /// operations are only executed if the value is not already in the cache.
    /// </remarks>
    public static async Task<T?> GetOrSetAsync<T>(this IDistributedCache cache, string key, Func<Task<T>> task,
        DistributedCacheEntryOptions? options = null)
    {
        byte[]? valueByte = await cache.GetAsync(key);
        if (valueByte is not null)
        {
            return JsonSerializer.Deserialize<T>(valueByte, SerializerOptions);
        }

        T value = await task();
        if (value is not null)
        {
            await cache.SetAsync<T>(key, value, options);
        }

        return value;
    }
}
