using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Models.Book;

public record BookCreateModel(
    string Isbn,
    string Title,
    AuthorId AuthorId,
    DateTime PublishedOn,
    decimal Price,
    int StockCount);
