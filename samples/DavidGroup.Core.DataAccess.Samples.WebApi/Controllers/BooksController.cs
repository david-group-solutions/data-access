using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Samples.WebApi.Dtos;
using DavidGroup.Core.DataAccess.Samples.WebApi.Models.Book;
using DavidGroup.Core.DataAccess.Samples.WebApi.Services;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;

using Microsoft.AspNetCore.Mvc;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(IBooksService booksService) : ControllerBase
{
    [HttpGet]
    [Route("all")]
    [ProducesResponseType<OperationResult<List<BookReadDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        OperationResult<List<BookReadDto>> result = await booksService.GetAllAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [Route("all/offset-pagination")]
    [ProducesResponseType<OperationResult<PageData<BookReadDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOffsetPagination([FromQuery] PageOptions options,
        CancellationToken cancellationToken)
    {
        OperationResult<PageData<BookReadDto>> result =
            await booksService.GetAllAsync(options, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [Route("all/offset-pagination/string-ordering")]
    [ProducesResponseType<OperationResult<PageData<BookReadDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOffsetPaginationWithStringOrdering(
        [FromQuery] PageOptions options,
        [FromQuery] string orderBy,
        CancellationToken cancellationToken)
    {
        OperationResult<PageData<BookReadDto>> result =
            await booksService.GetAllAsync(
                options,
                orderBy: orderBy,
                allowedToOrderBy: [e => e.Id, e => e.Title, e => e.PublishedOn, e => e.Price],
                cancellationToken: cancellationToken
            );

        return Ok(result);
    }

    [HttpGet]
    [Route("all/cursor-pagination")]
    [ProducesResponseType<OperationResult<InfinitePageData<BookReadDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCursorPagination([FromQuery] InfinitePageOptions options,
        CancellationToken cancellationToken)
    {
        OperationResult<InfinitePageData<BookReadDto>> result =
            await booksService.GetAllAsync(options, cancellationToken: cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [Route("all/cursor-pagination/string-ordering")]
    [ProducesResponseType<OperationResult<InfinitePageData<BookReadDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllInfinitePaginationWithStringOrdering(
        [FromQuery] InfinitePageOptions options,
        [FromQuery] string orderBy,
        CancellationToken cancellationToken)
    {
        OperationResult<InfinitePageData<BookReadDto>> result =
            await booksService.GetAllAsync(
                options,
                orderBy: orderBy,
                allowedToOrderBy: [e => e.Id, e => e.Title, e => e.PublishedOn, e => e.Price],
                cancellationToken: cancellationToken
            );

        return Ok(result);
    }

    [HttpGet]
    [Route("all/by-author/{id}/offset-pagination")]
    [ProducesResponseType<OperationResult<PageData<BookReadDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllByAuthorIdOffsetPagination([FromRoute] AuthorId id,
        [FromQuery] PageOptions options,
        [FromQuery] string orderBy,
        CancellationToken cancellationToken)
    {
        OperationResult<PageData<BookReadDto>> result =
            await booksService.GetByAuthorAsync(id, options, orderBy, cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [Route("all/by-author/{id}/cursor-pagination")]
    [ProducesResponseType<OperationResult<InfinitePageData<BookReadDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllByAuthorIdCursorPagination([FromRoute] AuthorId id,
        [FromQuery] InfinitePageOptions options,
        [FromQuery] string orderBy,
        CancellationToken cancellationToken)
    {
        OperationResult<InfinitePageData<BookReadDto>> result =
            await booksService.GetByAuthorAsync(id, options, orderBy, cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType<OperationResult<BookReadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<OperationResult<BookReadDto>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] BookId id,
        CancellationToken cancellationToken)
    {
        OperationResult<BookReadDto> result =
            await booksService.GetByIdAsync(id, cancellationToken: cancellationToken);

        return !result.Succeeded
            ? NotFound(result)
            : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<OperationResult<BookReadDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] BookCreateModel dto,
        CancellationToken cancellationToken)
    {
        OperationResult<BookReadDto> result =
            await booksService.CreateAsync(dto, cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { result.Value!.Id }, result);
    }

    [HttpPut]
    [Route("{id}")]
    [ProducesResponseType<OperationResult<BookReadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<OperationResult<BookReadDto>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] BookId id,
        [FromBody] BookUpdateModel dto,
        CancellationToken cancellationToken)
    {
        OperationResult<BookReadDto> result =
            await booksService.UpdateAsync(id, dto, cancellationToken: cancellationToken);

        return !result.Succeeded
            ? NotFound(result)
            : Ok(result);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType<OperationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<OperationResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] BookId id,
        CancellationToken cancellationToken)
    {
        OperationResult result =
            await booksService.DeleteAsync(id, cancellationToken: cancellationToken);

        return !result.Succeeded
            ? NotFound(result)
            : Ok(result);
    }
}
