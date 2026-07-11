using DavidGroup.Core.DataAccess.Samples.WebApi.Data;
using DavidGroup.Core.DataAccess.Samples.WebApi.Entities;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;
using DavidGroup.Core.DataAccess.Sql.Repositories;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Repositories;

public class AuthorsRepository(BookStoreDbContext context)
    : BaseRepository<Author, AuthorId>(context), IAuthorsRepository;
