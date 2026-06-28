using DavidGroup.Core.DataAccess.Sql.Interceptors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DavidGroup.Core.DataAccessTests.Sql.Interceptors;

public class SoftDeleteInterceptorTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static TestDbContext CreateContext() =>
        DbContextFactory.Create(new SoftDeleteInterceptor());

    // -------------------------------------------------------------------------
    // Core behaviour
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Deleted_ISoftDeletable_Entity_Is_Not_Physically_Removed()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        SoftDeletableEntity entity = new() { Name = "to-delete" };
        ctx.SoftDeletables.Add(entity);
        await ctx.SaveChangesAsync();

        // Act
        ctx.SoftDeletables.Remove(entity);
        await ctx.SaveChangesAsync();

        // Assert
        SoftDeletableEntity? inDb = await ctx.SoftDeletables.FindAsync(entity.Id);
        Assert.NotNull(inDb);
    }

    [Fact]
    public async Task Deleted_ISoftDeletable_Entity_Has_IsDeleted_Set_To_True()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        SoftDeletableEntity entity = new() { Name = "to-delete" };
        ctx.SoftDeletables.Add(entity);
        await ctx.SaveChangesAsync();

        // Act
        ctx.SoftDeletables.Remove(entity);
        await ctx.SaveChangesAsync();

        // Assert
        SoftDeletableEntity? inDb = await ctx.SoftDeletables.FindAsync(entity.Id);
        Assert.True(inDb!.IsDeleted);
    }

    [Fact]
    public async Task Non_Deleted_ISoftDeletable_Entity_Keeps_IsDeleted_False()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        SoftDeletableEntity entity = new() { Name = "keep" };
        ctx.SoftDeletables.Add(entity);
        await ctx.SaveChangesAsync();

        // Assert
        SoftDeletableEntity? inDb = await ctx.SoftDeletables.FindAsync(entity.Id);
        Assert.False(inDb!.IsDeleted);
    }

    [Fact]
    public async Task Multiple_Deleted_Entities_Are_All_Soft_Deleted()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        SoftDeletableEntity a = new() { Name = "a" };
        SoftDeletableEntity b = new() { Name = "b" };
        SoftDeletableEntity c = new() { Name = "c" };

        ctx.SoftDeletables.AddRange(a, b, c);
        await ctx.SaveChangesAsync();

        // Act
        ctx.SoftDeletables.RemoveRange(a, b, c);
        await ctx.SaveChangesAsync();

        // Assert
        List<SoftDeletableEntity> all = await ctx.SoftDeletables.ToListAsync();
        Assert.All(all, e => Assert.True(e.IsDeleted));
    }

    [Fact]
    public async Task Only_Deleted_Entities_Are_Affected_Not_Unchanged_Ones()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        SoftDeletableEntity keep = new() { Name = "keep" };
        SoftDeletableEntity remove = new() { Name = "remove" };

        ctx.SoftDeletables.AddRange(keep, remove);
        await ctx.SaveChangesAsync();

        // Act
        ctx.SoftDeletables.Remove(remove);
        await ctx.SaveChangesAsync();

        // Assert
        SoftDeletableEntity? keepInDb = await ctx.SoftDeletables.FindAsync(keep.Id);
        SoftDeletableEntity? removeInDb = await ctx.SoftDeletables.FindAsync(remove.Id);

        Assert.False(keepInDb!.IsDeleted);
        Assert.True(removeInDb!.IsDeleted);
    }

    [Fact]
    public async Task Entity_State_Is_Correct_After_Soft_Delete()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        SoftDeletableEntity entity = new() { Name = "x" };
        ctx.SoftDeletables.Add(entity);
        await ctx.SaveChangesAsync();

        EntityEntry<SoftDeletableEntity> entry = ctx.Entry(entity);

        // Act
        ctx.SoftDeletables.Remove(entity);

        // Assert
        Assert.Equal(EntityState.Deleted, entry.State);
        await ctx.SaveChangesAsync();
        Assert.Equal(EntityState.Unchanged, entry.State);
    }

    // -------------------------------------------------------------------------
    // Entities that do NOT implement ISoftDeletable are physically removed
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Non_ISoftDeletable_Entities_Are_Physically_Removed()
    {
        // Arrange
        await using TestDbContext ctx = CreateContext();

        TimedEntity entity = new() { Name = "plain" };
        ctx.TimedEntities.Add(entity);
        await ctx.SaveChangesAsync();

        // Act
        ctx.TimedEntities.Remove(entity);
        await ctx.SaveChangesAsync();

        // Assert
        TimedEntity? inDb = await ctx.TimedEntities.FindAsync(entity.Id);
        Assert.Null(inDb);
    }
}
