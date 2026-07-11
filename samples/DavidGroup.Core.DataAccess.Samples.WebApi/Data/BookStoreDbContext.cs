using DavidGroup.Core.DataAccess.Samples.WebApi.Entities;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;
using DavidGroup.Core.DataAccess.Sql.Extensions;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Data;

public sealed class BookStoreDbContext(DbContextOptions<BookStoreDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookStoreDbContext).Assembly);

        modelBuilder.ApplyStronglyTypedSequentialIds();
        modelBuilder.ApplyQueryFiltersForSoftDeletedEntities();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<BookId>().HaveConversion<BookId.EfCoreValueConverter>();
        configurationBuilder.Properties<AuthorId>().HaveConversion<AuthorId.EfCoreValueConverter>();
    }
}
