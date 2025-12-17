using InventoryService.Activities;
using InventoryService.Repositories;
using Shared.Contracts;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

builder.Services
    .AddHostedTemporalWorker(clientTargetHost: "localhost:7233", clientNamespace: "default",
        taskQueue: TaskQueues.Inventory)
    .ConfigureOptions(opts => { opts.Interceptors = [new TracingInterceptor()]; })
    .AddScopedActivities<InventoryActivities>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/test", () => Results.Ok("Test endpoint is working!"));

app.Run();