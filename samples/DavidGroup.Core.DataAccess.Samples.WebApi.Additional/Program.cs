using DavidGroup.Core.DataAccess.Cache;
using DavidGroup.Core.DataAccess.ElasticSearch;
using DavidGroup.Core.DataAccess.EventBus;

using HealthChecks.UI.Client;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

#if DEBUG
if (builder.Configuration["USE_REAL_EVENTBUS"] == "0")
    builder.Services.AddInMemoryEventBus();
else
    builder.Services.AddRabbitMq("EventBus");
#else
builder.Services.AddRabbitMq("EventBus");
#endif

builder.Services.AddElasticsearchClient();

builder.Services.AddDistributedCache(builder.Environment, builder.Configuration);

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddRedLock(builder.Configuration);
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddCheck<ElasticSearchHealthCheck>("elasticsearch", tags: ["ready", "elasticsearch"]);

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
