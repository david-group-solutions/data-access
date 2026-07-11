# DavidGroup.Core.DataAccess

#### [![Release](https://github.com/david-group-solutions/data-access/actions/workflows/release.yml/badge.svg)](https://github.com/david-group-solutions/data-access/actions/workflows/release.yml) [![Nuget](https://img.shields.io/nuget/v/DavidGroup.Core.DataAccess)](https://www.nuget.org/packages/DavidGroup.Core.DataAccess/)

Foundation library providing data access abstractions for Entity Framework, ADO.NET, and other .NET technologies, along
with common patterns and helper extensions.

---

## 🚀 Getting Started

### Install NuGet Package

Using the .NET CLI:

```bash
dotnet add package DavidGroup.Core.DataAccess
```

Or via the Package Manager Console:

```bash
Install-Package DavidGroup.Core.DataAccess
```

### How to use it?

Feel free to explore the [samples](https://github.com/david-group-solutions/data-access/tree/main/samples) to find
practical examples for each feature.
New samples are added continuously as more features are developed.

## 📦 Key Features

### Entities

```csharp
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

public sealed class Author : Entity<AuthorId>, IStronglyTypedSequentialId<AuthorId>,
    ISelfManageable<Author, AuthorCreateModel, AuthorUpdateModel>
{
    private Author() { }

    public string Name { get; private set; } = null!;

    public string? Biography { get; private set; }

    public ICollection<Book> Books { get; private init; } = new List<Book>();

    public static Author Create(AuthorCreateModel model)
    {
        return new Author { Name = model.Name };
    }

    public void Update(AuthorUpdateModel model)
    {
        Name = model.Name;
        Biography = model.Biography;
    }
}

[StronglyTypedId]
public partial struct BookId;
public record BookCreateModel(string Isbn, string Title, AuthorId AuthorId, DateTime PublishedOn, decimal Price, int StockCount);
public record BookUpdateModel(decimal Price, int StockCount);

[StronglyTypedId]
public partial struct AuthorId;
public record AuthorCreateModel(string Name);
public record AuthorUpdateModel(string Name, string? Biography);
```

### DbContext

```csharp
public class BookStoreDbContext(DbContextOptions<BookStoreDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyStronglyTypedSequentialIds();    // Sets SequentialStronglyTypedIdValueGenerator<TKey> for Id property.

        // Other options
        // modelBuilder.ApplySequentialGuids();            // Sets SequentialGuidValueGenerator for Id property.
        // modelBuilder.ApplySqlServerSequentialIds(this); // Sets NEWSEQUENTIALID() as a default SQL Server value for Id property.

        modelBuilder.ApplyQueryFiltersForSoftDeletedEntities();
    }
}
```

### Repository abstraction

```csharp
public interface IBooksRepository
    : IBaseRepository<Book, BookId>, IBaseAggregationRepository<Book>;

public class BooksRepository(BookStoreDbContext context)
    : BaseRepository<Book, BookId>(context), IBooksRepository;
```

### Service abstraction

```csharp
public interface IBooksService : IBaseService<Book, BookId, BookCreateModel, BookUpdateModel, BookReadDto>
{
    Task<OperationResult<PageData<BookReadDto>>> GetByAuthorAsync(AuthorId authorId,
        PageOptions options,
        string orderBy,
        CancellationToken cancellationToken = default);

    Task<OperationResult<InfinitePageData<BookReadDto>>> GetByAuthorAsync(AuthorId authorId,
        InfinitePageOptions options,
        string orderBy,
        CancellationToken cancellationToken = default);
}

public class BooksService(IBooksRepository repository, IEfUnitOfWork<BookStoreDbContext> unitOfWork)
    : BaseService<BookStoreDbContext, IBooksRepository,
    Book, BookId,
    BookCreateModel, BookUpdateModel,
    BookReadDto>(repository, unitOfWork),
    IBooksService
{
    protected override Expression<Func<Book, BookReadDto>> ToReadDto => book => book.ToDto();

    public async Task<OperationResult<PageData<BookReadDto>>> GetByAuthorAsync(AuthorId authorId,
        PageOptions options,
        string orderBy,
        CancellationToken cancellationToken = default)
    {
        OperationResult<IReadOnlyList<OrderingSpecification<Book>>> orderingSpecificationsResult =
            OrderingSpecification<Book>.Parse(orderBy, allowedProperties:
            [
                e => e.Id,
                e => e.Title,
                e => e.PublishedOn
            ]);

        if (!orderingSpecificationsResult.Succeeded)
            return OperationResult<PageData<BookReadDto>>.Failure(orderingSpecificationsResult.Messages[0]);

        PageData<BookReadDto> result = await Repository.GetAllAsync(
            options,
            predicate => predicate.AuthorId == authorId,
            orderingSpecifications: orderingSpecificationsResult.Value,
            include: i => i.Include(e => e.Author),
            selector: ToReadDto,
            cancellationToken: cancellationToken);

        return OperationResult<PageData<BookReadDto>>.Success(result);
    }

    public async Task<OperationResult<InfinitePageData<BookReadDto>>> GetByAuthorAsync(AuthorId authorId,
        InfinitePageOptions options,
        string orderBy,
        CancellationToken cancellationToken = default)
    {
        OperationResult<IReadOnlyList<OrderingSpecification<Book>>> orderingSpecificationsResult =
            OrderingSpecification<Book>.Parse(orderBy, allowedProperties:
            [
                e => e.Id,
                e => e.Title,
                e => e.PublishedOn
            ]);

        if (!orderingSpecificationsResult.Succeeded)
            return OperationResult<InfinitePageData<BookReadDto>>.Failure(orderingSpecificationsResult.Messages[0]);

        InfinitePageData<BookReadDto> result = await Repository.GetAllAsync(
            options,
            orderingSpecifications: orderingSpecificationsResult.Value,
            predicate => predicate.AuthorId == authorId,
            include: i => i.Include(e => e.Author),
            selector: ToReadDto,
            cancellationToken: cancellationToken);

        return OperationResult<InfinitePageData<BookReadDto>>.Success(result);
    }
}

public record BookReadDto(BookId Id, string Isbn, string Title, AuthorReadDto Author, DateTime PublishedOn, decimal Price, int StockCount);
public record AuthorReadDto(AuthorId Id, string Name, string? Biography);

public static class BookMappers
{
    public static BookReadDto ToDto(this Book book)
    {
        return new BookReadDto(book.Id,
            book.Isbn,
            book.Title,
            new AuthorReadDto(
                book.Author.Id,
                book.Author.Name,
                book.Author.Biography
            ),
            book.PublishedOn,
            book.Price,
            book.StockCount
        );
    }
}
```

### Extensions

```csharp
var sqlConnectionString = builder.Configuration.GetConnectionString("BookstoreDb");

builder.Services.AddSqlServerDatabase<BookStoreDbContext>(
    sqlConnectionString,
    typeof(BookStoreDbContext).Assembly.GetName().Name
);

builder.Services.AddEfUnitOfWork<BookStoreDbContext>();

builder.Services.AddRepositoriesAuto();
builder.Services.AddServicesAuto();
```

## 🤝 Contributing

Found a bug? Have an idea? Want to contribute?

* Submit an issue:
  https://github.com/david-group-solutions/data-access/issues
* Create a pull request:
  https://github.com/david-group-solutions/data-access/pulls

Contributions of any size are appreciated!

## 📝 License

Distributed under the **MIT license**.
See [License](https://github.com/david-group-solutions/data-access/blob/main/LICENSE.txt) for more information.

Copyright © 2025-2026 David Khachatryan (David Group Solutions)
