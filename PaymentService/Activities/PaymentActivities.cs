using PaymentService.Services;
using Shared.Contracts;
using Temporalio.Activities;
using Temporalio.Workflows;

namespace PaymentService.Activities;

public class PaymentActivities(IPaymentService service) : IPaymentActivities
{
    [Activity]
    public async Task<PaymentResult> PayAsync(PaymentRequest request)
    {
        var paymentId = await service.PayAsync(request);

        return new PaymentResult(paymentId, Success: true);
    }

    [Activity]
    public async Task<bool> WaitPaymentApprovalAsync(Guid paymentId)
    {
        // Poll for approval status change
        // We poll for up to 5 minutes (handled by activity timeout)
        while (!await service.IsPaymentStatusChangedAsync(paymentId))
        {
            // Wait 2 seconds before checking again
            await Task.Delay(2000);
            
            // Check for cancellation
            ActivityExecutionContext.Current.CancellationToken.ThrowIfCancellationRequested();
        }
        
        return await service.IsPaymentApprovedAsync(paymentId);
    }

    [Activity]
    public async Task RefundAsync(Guid paymentId)
    {
        await service.RefundAsync(paymentId);
    }
}