using DeliveryService.Activities;
using DeliveryService.Services;
using Shared.Contracts;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddScoped<IDeliveryService, DeliveryService.Services.DeliveryService>();

var temporalOptions = new TemporalOptions();
// 1. Register Temporal client with TLS
builder.Services.AddTemporalClient(options =>
{
    options.TargetHost = temporalOptions.Host;
    options.Namespace  = temporalOptions.Namespace;

    if (temporalOptions.UseTls)
        options.Tls = new TlsOptions();

    options.Interceptors = [new TracingInterceptor()];
});

// 2. Worker reuses that client
builder.Services
    .AddHostedTemporalWorker(taskQueue: TaskQueues.Delivery)
    .ConfigureOptions(opts =>
    {
        opts.Interceptors = [new TracingInterceptor()];
    })
    .AddScopedActivities<DeliveryActivities>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/test", () => Results.Ok("Test endpoint is working!"));

app.Run();