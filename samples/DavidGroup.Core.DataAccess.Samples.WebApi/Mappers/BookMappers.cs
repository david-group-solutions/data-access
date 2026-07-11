using DavidGroup.Core.DataAccess.Samples.WebApi.Dtos;
using DavidGroup.Core.DataAccess.Samples.WebApi.Entities;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Mappers;

public static class BookMappers
{
    public static BookReadDto ToDto(this Book book)
    {
        return new BookReadDto(book.Id,
            book.Isbn,
            book.Title,
            book.Author.ToDto(),
            book.PublishedOn,
            book.Price,
            book.StockCount
        );
    }
}
