using DavidGroup.Core.DataAccess.Samples.WebApi.Models.Author;
using DavidGroup.Core.DataAccess.Samples.WebApi.StronglyTypedIds;
using DavidGroup.Core.DataAccess.Sql.Entities;

namespace DavidGroup.Core.DataAccess.Samples.WebApi.Entities;

public sealed class Author : Entity<AuthorId>, IStronglyTypedSequentialId<AuthorId>,
    ISelfManageable<Author, AuthorCreateModel, AuthorUpdateModel>
{
    private Author() { }

    public string Name
    {
        get;
        private set;
    } = null!;

    public string? Biography
    {
        get;
        private set;
    }

    public ICollection<Book> Books
    {
        get;
        private init;
    } = new List<Book>();

    public static Author Create(AuthorCreateModel model)
    {
        return new Author { Name = model.Name };
    }

    public void Update(AuthorUpdateModel model)
    {
        Name = model.Name;
        Biography = model.Biography;
    }
}
