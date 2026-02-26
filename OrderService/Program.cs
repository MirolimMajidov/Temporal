using Microsoft.AspNetCore.Mvc;
using OrderService.Activities;
using OrderService.Contracts;
using OrderService.Repositories;
using OrderService.Workflows;
using Shared.Contracts;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Required for Minimal APIs
builder.Services.AddEndpointsApiExplorer();
// Add the built-in OpenAPI service
builder.Services.AddOpenApi();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var temporalOptions = new TemporalOptions();
// 1. Register Temporal client as ITemporalClient
builder.Services.AddTemporalClient(options =>
{
    options.TargetHost = temporalOptions.Host;
    options.Namespace = temporalOptions.Namespace;

    if (temporalOptions.UseTls)
        options.Tls = new TlsOptions();

    // Add tracing interceptor
    options.Interceptors = [new TracingInterceptor()];
});

// 2. Optionally run a worker in this service for order-related work
builder.Services.AddHostedTemporalWorker(TaskQueues.OrderOrchestration)
    .AddScopedActivities<OrderActivities>()
    .AddWorkflow<OrderProcessWorkflow>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Map the OpenAPI document endpoint (e.g., /openapi/v1.json)
    app.MapOpenApi();

    // Enable the Swagger UI middleware, only in development for security best practices
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Order API V1");
        options.RoutePrefix = "swagger"; // Access UI at /swagger
        options.EnableTryItOutByDefault();
    });
}

app.MapGet("/test", () => Results.Ok("Test endpoint is working!"));
app.MapPost("/create-order", async (CreateOrderDto dto, [FromServices] ITemporalClient client) =>
{
    var orderId = Guid.NewGuid();
    var order = new OrderDetails(
        OrderId: orderId,
        CustomerId: dto.CustomerId,
        ItemId: dto.ItemId,
        Quantity: dto.Quantity,
        Amount: dto.Amount,
        Currency: dto.Currency,
        ShippingAddress: dto.ShippingAddress,
        ShouldCommunicateWithPhp: dto.ShouldCommunicateWithPhp,
        ShouldFailDelivery: dto.ShouldFailDelivery);

    var options = new WorkflowOptions(
        id: $"order-{order.OrderId}",
        taskQueue: TaskQueues.OrderOrchestration);

    //await client.ExecuteWorkflowAsync((OrderProcessWorkflow wf) => wf.RunAsync(order), options);
    await client.StartWorkflowAsync((OrderProcessWorkflow wf) => wf.RunAsync(order), options);

    return Results.Created(string.Empty, value: new { orderId = orderId });
});

app.Run();