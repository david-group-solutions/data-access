using DavidGroup.Core.DataAccess.Samples.WebApi.Dtos;
using DavidGroup.Core.DataAccess.Samples.WebApi.Entities;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Mappers;

public static class AuthorMappers
{
    public static AuthorReadDto ToDto(this Author author)
    {
        return new AuthorReadDto(
            author.Id,
            author.Name,
            author.Biography
        );
    }
}
