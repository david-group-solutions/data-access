using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Dtos;

public record BookReadDto(
    BookId Id,
    string Isbn,
    string Title,
    AuthorReadDto Author,
    DateTime PublishedOn,
    decimal Price,
    int StockCount);
