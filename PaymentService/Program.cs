using PaymentService.Activities;
using PaymentService.Repositories;
using PaymentService.Services;
using Shared.Contracts;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddScoped<IPaymentService, PaymentService.Services.PaymentService>();
builder.Services.AddSingleton<IApprovalRepository, ApprovalRepository>();

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
    .AddHostedTemporalWorker(taskQueue: TaskQueues.Payment)
    .ConfigureOptions(opts =>
    {
        opts.Interceptors = [new TracingInterceptor()];
    })
    .AddScopedActivities<PaymentActivities>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Enable the Swagger UI middleware, only in development for security best practices
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Payment API V1");
        options.RoutePrefix = "swagger"; // Access UI at /swagger
        options.EnableTryItOutByDefault();
    });
}

app.MapGet("/test", () => Results.Ok("Test endpoint is working!"));

// Payment Approval Endpoints
app.MapGet("/payments/{paymentId}/approval", async (Guid paymentId, IApprovalRepository approvalRepo) =>
{
    var approval = await approvalRepo.GetPaymentApprovalAsync(paymentId);
    if (approval == null)
        return Results.NotFound($"Payment {paymentId} not found");
    
    return Results.Ok(approval);
})
.WithName("GetPaymentApproval")
.WithOpenApi();

app.MapPost("/payments/{paymentId}/approve", async (Guid paymentId, IPaymentService service, IApprovalRepository repo, ITemporalClient client) =>
{
    if (await service.IsPaymentApprovedAsync(paymentId))
        return Results.BadRequest("Payment is already approved");
        
    await service.ApprovePaymentAsync(paymentId);
    
    // Retrieve approval to get workflow details
    var approval = await repo.GetPaymentApprovalAsync(paymentId);
    if (approval != null && !string.IsNullOrEmpty(approval.WorkflowId))
    {
        await client.GetWorkflowHandle<IOrderWorkflow>(approval.WorkflowId, approval.WorkflowRunId)
            .SignalAsync(wf => wf.ReviewPaymentAsync(PaymentApprovalStatus.Approved));
    }
    
    return Results.Ok($"Payment {paymentId} approved");
})
.WithName("ApprovePayment")
.WithOpenApi();

app.MapPost("/payments/{paymentId}/reject", async (Guid paymentId, IPaymentService service, IApprovalRepository repo, ITemporalClient client) =>
{
    // We could add a check if already rejected
    await service.RejectPaymentAsync(paymentId);
    
    // Retrieve approval to get workflow details
    var approval = await repo.GetPaymentApprovalAsync(paymentId);
    if (approval != null && !string.IsNullOrEmpty(approval.WorkflowId))
    {
        await client.GetWorkflowHandle<IOrderWorkflow>(approval.WorkflowId, approval.WorkflowRunId)
             .SignalAsync(wf => wf.ReviewPaymentAsync(PaymentApprovalStatus.Rejected));
    }
    
    return Results.Ok($"Payment {paymentId} rejected");
})
.WithName("RejectPayment")
.WithOpenApi();

app.Run();