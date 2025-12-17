using Microsoft.AspNetCore.Mvc;
using OrderService.Activities;
using OrderService.Contracts;
using OrderService.Repositories;
using OrderService.Workflows;
using Shared.Contracts;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// 1. Register Temporal client as ITemporalClient
builder.Services.AddTemporalClient(options =>
{
    options.TargetHost = "localhost:7233";
    options.Namespace = "default";
});

// 2. Optionally run a worker in this service for order-related work
builder.Services.AddHostedTemporalWorker(
        clientTargetHost: "localhost:7233",
        clientNamespace: "default",
        taskQueue: TaskQueues.OrderOrchestration)
    .AddScopedActivities<OrderActivities>()
    .AddWorkflow<ProcessOrderWorkflow>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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
        ShippingAddress: dto.ShippingAddress);

    var options = new WorkflowOptions(
        id: $"order-{order.OrderId}",
        taskQueue: TaskQueues.OrderOrchestration);

    await client.ExecuteWorkflowAsync(
        (ProcessOrderWorkflow wf) =>
            wf.RunAsync(order),
        options);

    return  Results.Created(string.Empty, value: new { orderId = orderId });
});

app.Run();