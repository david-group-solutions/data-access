using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Dtos;

public record AuthorReadDto(AuthorId Id, string Name, string? Biography);
