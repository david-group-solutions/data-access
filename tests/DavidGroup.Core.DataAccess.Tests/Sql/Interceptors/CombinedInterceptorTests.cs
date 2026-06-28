using DavidGroup.Core.DataAccess.Sql.Interceptors;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Interceptors;

public class CombinedInterceptorTests
{
    private static TestDbContext CreateContext() =>
        DbContextFactory.Create(new SoftDeleteInterceptor(), new TimedEntitiesInterceptor());

    [Fact]
    public async Task Add_Sets_Both_Timestamps_And_IsDeleted_False()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        FullEntity entity = new() { Name = "full" };

        // Act
        ctx.FullEntities.Add(entity);
        await ctx.SaveChangesAsync();

        // Assert
        Assert.NotEqual(default, entity.CreatedAtUtc);
        Assert.NotEqual(default, entity.ModifiedAtUtc);

        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public async Task Soft_Delete_Sets_IsDeleted_True_And_Entity_Remains_In_Db()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        FullEntity entity = new() { Name = "full" };

        ctx.FullEntities.Add(entity);
        await ctx.SaveChangesAsync();

        // Act
        ctx.FullEntities.Remove(entity);
        await ctx.SaveChangesAsync();

        // Assert
        FullEntity? inDb = await ctx.FullEntities.FindAsync(entity.Id);

        Assert.NotNull(inDb);
        Assert.True(inDb.IsDeleted);
    }

    [Fact]
    public async Task Soft_Delete_Does_Update_ModifiedAtUtc_Via_TimedInterceptor()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        FullEntity entity = new() { Name = "full" };
        ctx.FullEntities.Add(entity);
        await ctx.SaveChangesAsync();

        DateTime modifiedAfterAdd = entity.ModifiedAtUtc;
        await Task.Delay(10);

        // Act
        ctx.FullEntities.Remove(entity);
        await ctx.SaveChangesAsync();

        // Assert
        FullEntity? inDb = await ctx.FullEntities.FindAsync(entity.Id);

        Assert.True(inDb!.ModifiedAtUtc > modifiedAfterAdd);
    }
}
