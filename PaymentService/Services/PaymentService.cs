using Shared.Contracts;

namespace PaymentService.Services;

public class PaymentService : IPaymentService
{
    public Task<Guid> PayAsync(PaymentRequest request)
    {
        var paymentId = Guid.NewGuid();
        return Task.FromResult(paymentId);
    }

    public Task<bool> RefundAsync(Guid paymentId)
    {
        return Task.FromResult(true);
    }
}

public interface IPaymentService
{
    Task<Guid> PayAsync(PaymentRequest request);
    
    Task<bool> RefundAsync(Guid paymentId);
}