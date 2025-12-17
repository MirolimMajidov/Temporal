using Shared.Contracts;

namespace PaymentService.Services;

public class PaymentService : IPaymentService
{
    public async Task<Guid> PayAsync(PaymentRequest request)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        var paymentId = Guid.NewGuid();
        return await Task.FromResult(paymentId);
    }

    public async Task<bool> RefundAsync(Guid paymentId)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        return await Task.FromResult(true);
    }
}

public interface IPaymentService
{
    Task<Guid> PayAsync(PaymentRequest request);
    
    Task<bool> RefundAsync(Guid paymentId);
}