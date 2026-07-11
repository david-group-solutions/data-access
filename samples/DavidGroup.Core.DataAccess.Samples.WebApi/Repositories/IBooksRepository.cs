using DavidGroup.Core.DataAccess.Samples.WebApi.Entities;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;
using DavidGroup.Core.DataAccess.Sql.Repositories;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Repositories;

public interface IBooksRepository : IBaseRepository<Book, BookId>, IBaseAggregationRepository<Book>;
