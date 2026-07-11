namespace DavidGroup.Core.DataAccess.Samples.WebApi.Additional.Dtos;

public sealed class ProductDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public DateTime CreatedAt { get; init; }
}
