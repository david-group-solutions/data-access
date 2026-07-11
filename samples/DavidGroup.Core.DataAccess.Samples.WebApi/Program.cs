using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll.Extensions;
using DavidGroup.Core.DataAccess.Samples.WebApi.Data;
using DavidGroup.Core.DataAccess.Samples.WebApi.Swagger;
using DavidGroup.Core.DataAccess.Sql.Extensions;

using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddDynamicCursorJsonConverter();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SchemaFilter<SwaggerStronglyTypedIdFilter>());

builder.Services.AddDatabase<BookStoreDbContext>(
    (options, connStr, asmName) => options.UseSqlite(connStr, x => x.MigrationsAssembly(asmName)),
    builder.Configuration.GetConnectionString("BookstoreDb"),
    typeof(BookStoreDbContext).Assembly.GetName().Name
);

builder.Services.AddEfUnitOfWork<BookStoreDbContext>();

builder.Services.AddRepositoriesAuto();
builder.Services.AddServicesAuto();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.Services.MigrateDatabase<BookStoreDbContext>();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
