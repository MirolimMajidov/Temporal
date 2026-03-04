using Shared.Contracts;

namespace PaymentService.Repositories;

public interface IApprovalRepository
{
    Task CreateApprovalAsync(Guid paymentId, string? workflowId = null, string? runId = null);
    Task<PaymentApprovalStatus?> GetApprovalStatusAsync(Guid paymentId);
    Task ApprovePaymentAsync(Guid paymentId, string approvedBy);
    Task RejectPaymentAsync(Guid paymentId);
    Task<PaymentApproval?> GetPaymentApprovalAsync(Guid paymentId);
}

public class ApprovalRepository : IApprovalRepository
{
    private readonly Dictionary<Guid, PaymentApproval> _approvals = new();
    private readonly Lock _lockObj = new();

    public Task CreateApprovalAsync(Guid paymentId, string? workflowId = null, string? runId = null)
    {
        lock (_lockObj)
        {
            _approvals[paymentId] = new PaymentApproval(
                paymentId,
                PaymentApprovalStatus.Pending,
                DateTime.UtcNow,
                workflowId,
                runId);
        }
        return Task.CompletedTask;
    }

    public Task<PaymentApprovalStatus?> GetApprovalStatusAsync(Guid paymentId)
    {
        lock (_lockObj)
        {
            if (_approvals.TryGetValue(paymentId, out var approval))
            {
                return Task.FromResult<PaymentApprovalStatus?>(approval.Status);
            }
        }
        return Task.FromResult<PaymentApprovalStatus?>(null);
    }

    public Task ApprovePaymentAsync(Guid paymentId, string approvedBy)
    {
        lock (_lockObj)
        {
            if (_approvals.TryGetValue(paymentId, out var approval))
            {
                _approvals[paymentId] = approval with
                {
                    Status = PaymentApprovalStatus.Approved,
                    ApprovedAt = DateTime.UtcNow,
                    ApprovedBy = approvedBy
                };
            }
        }
        return Task.CompletedTask;
    }

    public Task RejectPaymentAsync(Guid paymentId)
    {
        lock (_lockObj)
        {
            if (_approvals.TryGetValue(paymentId, out var approval))
            {
                _approvals[paymentId] = approval with
                {
                    Status = PaymentApprovalStatus.Rejected,
                    ApprovedAt = DateTime.UtcNow
                };
            }
        }
        return Task.CompletedTask;
    }

    public Task<PaymentApproval?> GetPaymentApprovalAsync(Guid paymentId)
    {
        lock (_lockObj)
        {
            if (_approvals.TryGetValue(paymentId, out var approval))
            {
                return Task.FromResult<PaymentApproval?>(approval);
            }
        }
        return Task.FromResult<PaymentApproval?>(null);
    }
}

