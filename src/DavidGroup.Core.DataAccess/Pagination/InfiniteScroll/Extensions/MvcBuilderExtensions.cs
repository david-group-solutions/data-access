using Microsoft.Extensions.DependencyInjection;

namespace DavidGroup.Core.DataAccess.Pagination.InfiniteScroll.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IMvcBuilder"/>.
/// </summary>
public static class MvcBuilderExtensions
{
    /// <summary>
    /// Registers the <see cref="DynamicCursorJsonConverter"/> with the MVC JSON serializer.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IMvcBuilder"/> used to configure MVC services.
    /// </param>
    /// <returns>
    /// The same <see cref="IMvcBuilder"/> instance so that additional MVC configuration
    /// can be chained.
    /// </returns>
    public static IMvcBuilder AddDynamicCursorJsonConverter(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new DynamicCursorJsonConverter());
        });

        return builder;
    }
}
