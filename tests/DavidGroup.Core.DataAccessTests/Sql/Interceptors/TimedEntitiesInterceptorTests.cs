using DavidGroup.Core.DataAccess.Sql.Interceptors;

namespace DavidGroup.Core.DataAccessTests.Sql.Interceptors;

public class TimedEntitiesInterceptorTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static TestDbContext CreateContext() =>
        DbContextFactory.Create(new TimedEntitiesInterceptor());

    // -------------------------------------------------------------------------
    // Added state
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreatedAtUtc_Is_Set_On_Add()
    {
        // Arrange, Act
        await using TestDbContext ctx = CreateContext();

        DateTime before = DateTime.UtcNow;

        TimedEntity entity = new() { Name = "new" };
        ctx.TimedEntities.Add(entity);
        await ctx.SaveChangesAsync();

        DateTime after = DateTime.UtcNow;

        // Assert
        Assert.InRange(entity.CreatedAtUtc, before, after);
    }

    [Fact]
    public async Task ModifiedAtUtc_Equals_CreatedAtUtc_On_Add()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        TimedEntity entity = new() { Name = "new" };

        // Act
        ctx.TimedEntities.Add(entity);
        await ctx.SaveChangesAsync();

        // Assert
        Assert.Equal(entity.CreatedAtUtc, entity.ModifiedAtUtc);
    }

    [Fact]
    public async Task CreatedAtUtc_Is_In_Utc_Kind_On_Add()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        TimedEntity entity = new() { Name = "new" };

        // Act
        ctx.TimedEntities.Add(entity);
        await ctx.SaveChangesAsync();

        // Assert
        Assert.Equal(DateTimeKind.Utc, entity.CreatedAtUtc.Kind);
    }

    // -------------------------------------------------------------------------
    // Modified state
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ModifiedAtUtc_Is_Updated_On_Modify()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        TimedEntity entity = new() { Name = "original" };
        ctx.TimedEntities.Add(entity);
        await ctx.SaveChangesAsync();

        DateTime createdAt = entity.CreatedAtUtc;
        await Task.Delay(10); // ensure clock advances

        // Act
        entity.Name = "updated";
        await ctx.SaveChangesAsync();

        Assert.True(entity.ModifiedAtUtc > createdAt);
    }

    [Fact]
    public async Task CreatedAtUtc_Is_Not_Changed_On_Modify()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        TimedEntity entity = new() { Name = "original" };
        ctx.TimedEntities.Add(entity);
        await ctx.SaveChangesAsync();

        DateTime originalCreatedAt = entity.CreatedAtUtc;
        await Task.Delay(10);

        // Act
        entity.Name = "updated";
        await ctx.SaveChangesAsync();

        // Assert
        Assert.Equal(originalCreatedAt, entity.CreatedAtUtc);
    }

    [Fact]
    public async Task ModifiedAtUtc_Is_In_Utc_Kind_On_Modify()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        TimedEntity entity = new() { Name = "original" };
        ctx.TimedEntities.Add(entity);
        await ctx.SaveChangesAsync();

        entity.Name = "updated";
        await ctx.SaveChangesAsync();

        // Assert
        Assert.Equal(DateTimeKind.Utc, entity.ModifiedAtUtc.Kind);
    }

    // -------------------------------------------------------------------------
    // Multiple entities
    // -------------------------------------------------------------------------

    [Fact]
    public async Task All_Added_Entities_Receive_Timestamps()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        List<TimedEntity> entities = Enumerable.Range(1, 5)
            .Select(i => new TimedEntity { Name = $"entity-{i}" })
            .ToList();

        // Act
        ctx.TimedEntities.AddRange(entities);
        await ctx.SaveChangesAsync();

        // Assert
        Assert.All(entities, e =>
        {
            Assert.NotEqual(default, e.CreatedAtUtc);
            Assert.NotEqual(default, e.ModifiedAtUtc);
        });
    }

    // -------------------------------------------------------------------------
    // Deleted state — timestamps should be touched
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Deleted_ITimedEntity_Does_Have_Timestamps_Modified()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        TimedEntity entity = new() { Name = "to-remove" };
        ctx.TimedEntities.Add(entity);
        await ctx.SaveChangesAsync();

        DateTime originalModified = entity.ModifiedAtUtc;
        await Task.Delay(10);

        // Act
        ctx.TimedEntities.Remove(entity);
        await ctx.SaveChangesAsync();

        // Assert
        Assert.True(originalModified < entity.ModifiedAtUtc);
    }
}
