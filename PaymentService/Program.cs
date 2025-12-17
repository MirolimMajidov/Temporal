using PaymentService.Activities;
using PaymentService.Services;
using Shared.Contracts;
using Temporalio.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<IPaymentService, PaymentService.Services.PaymentService>();

builder.Services
    .AddHostedTemporalWorker(
        clientTargetHost: "localhost:7233",
        clientNamespace: "default",
        taskQueue: TaskQueues.Payment)
    .AddScopedActivities<PaymentActivities>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/test", () => Results.Ok("Test endpoint is working!"));

app.Run();