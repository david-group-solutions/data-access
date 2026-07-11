using DavidGroup.Core.DataAccess.Samples.WebApi.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.EntityConfigurations;

public class BookEntityConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.Property(e => e.Price)
            .HasConversion<double>(); // We do convert decimal to double in order to make it compatible with SQLite.
    }
}
