using PaymentService.Services;
using Shared.Contracts;
using Temporalio.Activities;

namespace PaymentService.Activities;

public class PaymentActivities(IPaymentService service)
{
    [Activity]
    public async Task<PaymentResult> PayAsync(PaymentRequest request)
    {
        var paymentId = await service.PayAsync(request);
        return new PaymentResult(paymentId, Success: true);
    }

    [Activity]
    public async Task RefundPaymentAsync(Guid paymentId)
    {
        await service.RefundAsync(paymentId);
    }
}