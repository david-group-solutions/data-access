using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Samples.WebApi.Data;
using DavidGroup.Core.DataAccess.Samples.WebApi.Dtos;
using DavidGroup.Core.DataAccess.Samples.WebApi.Entities;
using DavidGroup.Core.DataAccess.Samples.WebApi.Mappers;
using DavidGroup.Core.DataAccess.Samples.WebApi.Models.Author;
using DavidGroup.Core.DataAccess.Samples.WebApi.Repositories;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;
using DavidGroup.Core.DataAccess.Sql.Services;
using DavidGroup.Core.DataAccess.Sql.UnitOfWork.EFCore;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Services;

public class AuthorsService(IAuthorsRepository repository, IEfUnitOfWork<BookStoreDbContext> unitOfWork)
    : BaseService<BookStoreDbContext, IAuthorsRepository,
            Author, AuthorId,
            AuthorCreateModel, AuthorUpdateModel,
            AuthorReadDto>(repository, unitOfWork),
        IAuthorsService
{
    protected override Expression<Func<Author, AuthorReadDto>> ToReadDto => author => author.ToDto();
}
