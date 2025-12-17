using DeliveryService.Activities;
using DeliveryService.Services;
using Shared.Contracts;
using Temporalio.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<IDeliveryService, DeliveryService.Services.DeliveryService>();

builder.Services
    .AddHostedTemporalWorker(
        clientTargetHost: "localhost:7233",
        clientNamespace: "default",
        taskQueue: TaskQueues.Delivery)
    .AddScopedActivities<DeliveryActivities>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/test", () => Results.Ok("Test endpoint is working!"));

app.Run();