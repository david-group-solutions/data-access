using DavidGroup.Core.DataAccess.Samples.WebApi.Dtos;
using DavidGroup.Core.DataAccess.Samples.WebApi.Entities;
using DavidGroup.Core.DataAccess.Samples.WebApi.Models.Author;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;
using DavidGroup.Core.DataAccess.Sql.Services;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Services;

public interface IAuthorsService : IBaseService<Author, AuthorId, AuthorCreateModel, AuthorUpdateModel, AuthorReadDto>;
