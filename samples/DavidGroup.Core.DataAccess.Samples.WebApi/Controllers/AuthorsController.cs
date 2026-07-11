using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Samples.WebApi.Dtos;
using DavidGroup.Core.DataAccess.Samples.WebApi.Models.Author;
using DavidGroup.Core.DataAccess.Samples.WebApi.Services;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;

using Microsoft.AspNetCore.Mvc;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController(IAuthorsService authorsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<OperationResult<List<AuthorReadDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        OperationResult<List<AuthorReadDto>> result = await authorsService.GetAllAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [Route("{id}")]
    [ProducesResponseType<OperationResult<AuthorReadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<OperationResult<AuthorReadDto>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] AuthorId id,
        CancellationToken cancellationToken)
    {
        OperationResult<AuthorReadDto> result =
            await authorsService.GetByIdAsync(id, cancellationToken: cancellationToken);

        return !result.Succeeded
            ? NotFound(result)
            : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<OperationResult<AuthorReadDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] AuthorCreateModel dto,
        CancellationToken cancellationToken)
    {
        OperationResult<AuthorReadDto> result =
            await authorsService.CreateAsync(dto, cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { result.Value!.Id }, result);
    }

    [HttpPut]
    [Route("{id}")]
    [ProducesResponseType<OperationResult<AuthorReadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<OperationResult<AuthorReadDto>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] AuthorId id,
        [FromBody] AuthorUpdateModel dto,
        CancellationToken cancellationToken)
    {
        OperationResult<AuthorReadDto> result =
            await authorsService.UpdateAsync(id, dto, cancellationToken: cancellationToken);

        return !result.Succeeded
            ? NotFound(result)
            : Ok(result);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType<OperationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<OperationResult>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] AuthorId id,
        CancellationToken cancellationToken)
    {
        OperationResult result =
            await authorsService.DeleteAsync(id, cancellationToken: cancellationToken);

        return !result.Succeeded
            ? NotFound(result)
            : Ok(result);
    }
}
