using PaymentService.Activities;
using PaymentService.Services;
using Shared.Contracts;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddScoped<IPaymentService, PaymentService.Services.PaymentService>();

var temporalOptions = new TemporalOptions();
builder.Services
    .AddHostedTemporalWorker(
        clientTargetHost: temporalOptions.Host,
        clientNamespace: temporalOptions.Namespace,
        taskQueue: TaskQueues.Payment)
    .ConfigureOptions(opts => { opts.Interceptors = [new TracingInterceptor()]; })
    .AddScopedActivities<PaymentActivities>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/test", () => Results.Ok("Test endpoint is working!"));

app.Run();