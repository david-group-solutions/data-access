using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Samples.WebApi.Dtos;
using DavidGroup.Core.DataAccess.Samples.WebApi.Entities;
using DavidGroup.Core.DataAccess.Samples.WebApi.Models.Book;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;
using DavidGroup.Core.DataAccess.Sql.Services;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Services;

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
