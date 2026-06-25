using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Configuration;

namespace DavidGroup.Core.DataAccess.Pagination;

/// <summary>
/// Used to retrieve maximum allowed value from configuration injected in service provider.
/// The key is "Pagination:MaxPageSize". If not defined fallbacks to default 100.
/// </summary>
public class MaxPageSizeAttribute : ValidationAttribute
{
    /// <summary>
    /// Validates if value is within the maximum allowed value from configuration. Fallbacks to default 100.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        IConfiguration? config = ctx.GetService(typeof(IConfiguration)) as IConfiguration;
        int max = config?.GetValue<int>("Pagination:MaxPageSize") ?? 100;
        if (max <= 0) max = 100;

        if (value is int size && size > max)
            return new ValidationResult($"{ErrorMessages.PageSizeShouldNotExceedMaximum}={max}");

        return ValidationResult.Success;
    }
}
