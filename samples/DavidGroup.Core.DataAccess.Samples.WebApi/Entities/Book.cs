using DavidGroup.Core.DataAccess.Samples.WebApi.Models.Book;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;
using DavidGroup.Core.DataAccess.Sql.Entities;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Entities;

public sealed class Book : Entity<BookId>, IStronglyTypedSequentialId<BookId>,
    ITimedEntity, ISoftDeletable,
    ISelfManageable<Book, BookCreateModel, BookUpdateModel>
{
    private Book() { }

    public string Isbn { get; private init; } = null!;

    public string Title { get; private init; } = null!;

    public AuthorId AuthorId { get; private init; }
    public Author Author { get; private init; } = null!;

    public DateTime PublishedOn { get; private init; }

    public decimal Price { get; private set; }
    public int StockCount { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ModifiedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public static Book Create(BookCreateModel model)
    {
        return new Book
        {
            Isbn = model.Isbn,
            Title = model.Title,
            AuthorId = model.AuthorId,
            PublishedOn = model.PublishedOn,
            Price = model.Price,
            StockCount = model.StockCount
        };
    }

    public void Update(BookUpdateModel model)
    {
        Price = model.Price;
        StockCount = model.StockCount;
    }
}
