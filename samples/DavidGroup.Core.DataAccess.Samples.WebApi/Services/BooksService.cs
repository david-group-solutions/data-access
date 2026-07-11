using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Samples.WebApi.Data;
using DavidGroup.Core.DataAccess.Samples.WebApi.Dtos;
using DavidGroup.Core.DataAccess.Samples.WebApi.Entities;
using DavidGroup.Core.DataAccess.Samples.WebApi.Mappers;
using DavidGroup.Core.DataAccess.Samples.WebApi.Models.Book;
using DavidGroup.Core.DataAccess.Samples.WebApi.Repositories;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;
using DavidGroup.Core.DataAccess.Sql.Builders;
using DavidGroup.Core.DataAccess.Sql.Services;
using DavidGroup.Core.DataAccess.Sql.UnitOfWork.EFCore;
using DavidGroup.Core.Utilities.Cache;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Services;

public class BooksService(IBooksRepository repository, IEfUnitOfWork<BookStoreDbContext> unitOfWork)
    : BaseService<BookStoreDbContext, IBooksRepository,
            Book, BookId,
            BookCreateModel, BookUpdateModel,
            BookReadDto>(repository, unitOfWork),
        IBooksService
{
    protected override Expression<Func<Book, BookReadDto>> ToReadDto => book => book.ToDto();

    public override async Task<OperationResult<List<BookReadDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        List<BookReadDto> result = await Repository.GetAllAsync(
            include: i => i.Include(e => e.Author),
            selector: ToReadDto,
            cancellationToken: cancellationToken
        );

        return OperationResult<List<BookReadDto>>.Success(result);
    }

    public override async Task<OperationResult<PageData<BookReadDto>>> GetAllAsync(PageOptions options,
        string? orderBy = null,
        IReadOnlyList<Expression<Func<Book, object>>>? allowedToOrderBy = null,
        CancellationToken cancellationToken = default)
    {
        PageData<BookReadDto> result;

        if (orderBy is null)
        {
            result = await Repository.GetAllAsync(
                options,
                orderBy: null,
                include: i => i.Include(e => e.Author),
                selector: ToReadDto,
                cancellationToken: cancellationToken
            );
        }
        else
        {
            OperationResult<IReadOnlyList<OrderingSpecification<Book>>> orderingSpecificationsResult =
                OrderingSpecification<Book>.Parse(orderBy, allowedToOrderBy);

            if (!orderingSpecificationsResult.Succeeded)
                return OperationResult<PageData<BookReadDto>>.Failure(orderingSpecificationsResult.Messages[0]);

            result = await Repository.GetAllAsync(
                options,
                orderingSpecifications: orderingSpecificationsResult.Value,
                include: i => i.Include(e => e.Author),
                selector: ToReadDto,
                cancellationToken: cancellationToken
            );
        }

        return OperationResult<PageData<BookReadDto>>.Success(result);
    }

    public override async Task<OperationResult<InfinitePageData<BookReadDto>>> GetAllAsync(InfinitePageOptions options,
        string? orderBy = null,
        IReadOnlyList<Expression<Func<Book, object>>>? allowedToOrderBy = null,
        CancellationToken cancellationToken = default)
    {
        InfinitePageData<BookReadDto> result;

        if (orderBy is null)
        {
            result = await Repository.GetAllAsync(
                options,
                orderingSpecifications: [new OrderingSpecification<Book>(e => e.Id!, IsDescending: true)],
                include: i => i.Include(e => e.Author),
                selector: ToReadDto,
                cancellationToken: cancellationToken
            );
        }
        else
        {
            OperationResult<IReadOnlyList<OrderingSpecification<Book>>> orderingSpecificationsResult =
                OrderingSpecification<Book>.Parse(orderBy, allowedToOrderBy);

            if (!orderingSpecificationsResult.Succeeded)
                return OperationResult<InfinitePageData<BookReadDto>>.Failure(orderingSpecificationsResult.Messages[0]);

            result = await Repository.GetAllAsync(
                options,
                orderingSpecifications: orderingSpecificationsResult.Value,
                include: i => i.Include(e => e.Author),
                selector: ToReadDto,
                cancellationToken: cancellationToken
            );
        }

        return OperationResult<InfinitePageData<BookReadDto>>.Success(result);
    }

    public override async Task<OperationResult<BookReadDto>> GetByIdAsync(BookId id,
        CancellationToken cancellationToken = default)
    {
        Book? entity = await Repository.FirstOrDefaultAsync(
            predicate: e => e.Id == id,
            include: i => i.Include(e => e.Author),
            selector: e => e,
            cancellationToken: cancellationToken
        );

        if (entity is null)
        {
            return OperationResult<BookReadDto>.Failure(
                new OperationResultMessage(ErrorMessages.NotFound, OperationResultSeverity.Error));
        }

        BookReadDto readDto = InMemoryCompiledExpressionsCache.StoreOrRetrieve(ToReadDto).Invoke(entity);

        return OperationResult<BookReadDto>.Success(readDto);
    }

    public override async Task<OperationResult<BookReadDto>> CreateAsync(BookCreateModel model,
        CancellationToken cancellationToken = default)
    {
        Book entity = Book.Create(model);

        await Repository.CreateAsync(entity, cancellationToken);
        await UnitOfWork.SaveAsync(cancellationToken);

        await Repository.Context.Entry(entity).Reference(e => e.Author).LoadAsync(cancellationToken);

        BookReadDto readDto = InMemoryCompiledExpressionsCache.StoreOrRetrieve(ToReadDto).Invoke(entity);

        return OperationResult<BookReadDto>.Success(readDto);
    }

    public override async Task<OperationResult<BookReadDto>> UpdateAsync(BookId id, BookUpdateModel model,
        CancellationToken cancellationToken = default)
    {
        Book? entity = await Repository.FirstOrDefaultAsync(
            predicate: e => e.Id == id,
            include: i => i.Include(e => e.Author),
            selector: e => e,
            cancellationToken: cancellationToken
        );

        if (entity is null)
        {
            return OperationResult<BookReadDto>.Failure(
                new OperationResultMessage(ErrorMessages.NotFound, OperationResultSeverity.Error));
        }

        entity.Update(model);

        Repository.Update(entity);
        await UnitOfWork.SaveAsync(cancellationToken);

        BookReadDto readDto = InMemoryCompiledExpressionsCache.StoreOrRetrieve(ToReadDto).Invoke(entity);

        return OperationResult<BookReadDto>.Success(readDto);
    }

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
