using PaymentService.Repositories;
using Shared.Contracts;

namespace PaymentService.Services;

public class PaymentService(IApprovalRepository repository) : IPaymentService
{
    public async Task<Guid> PayAsync(PaymentRequest request)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        var paymentId = Guid.NewGuid();
        await repository.CreateApprovalAsync(paymentId, request.WorkflowId, request.WorkflowRunId);

        return await Task.FromResult(paymentId);
    }

    public async Task<Guid> ApprovePaymentAsync(Guid paymentId)
    {
        await repository.ApprovePaymentAsync(paymentId, "System");

        return paymentId;
    }

    public async Task<Guid> RejectPaymentAsync(Guid paymentId)
    {
        await repository.RejectPaymentAsync(paymentId);

        return paymentId;
    }

    public async Task<bool> RefundAsync(Guid paymentId)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        return await Task.FromResult(true);
    }
    
    public async Task<bool> IsPaymentStatusChangedAsync(Guid paymentId)
    {
        var status = await repository.GetApprovalStatusAsync(paymentId);
        return status != PaymentApprovalStatus.Pending;
    }

    public async Task<bool> IsPaymentApprovedAsync(Guid paymentId)
    {
        Console.WriteLine($"Time: {DateTime.Now}, Checking approval status for payment ID: {paymentId}");
        var status = await repository.GetApprovalStatusAsync(paymentId);
        return status == PaymentApprovalStatus.Approved;
    }
}

public interface IPaymentService
{
    Task<Guid> PayAsync(PaymentRequest request);

    Task<Guid> ApprovePaymentAsync(Guid paymentId);

    Task<Guid> RejectPaymentAsync(Guid paymentId);

    Task<bool> RefundAsync(Guid paymentId);

    Task<bool> IsPaymentStatusChangedAsync(Guid paymentId);

    Task<bool> IsPaymentApprovedAsync(Guid paymentId);
}