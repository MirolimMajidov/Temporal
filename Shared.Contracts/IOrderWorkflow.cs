using Shared.Contracts;
using Temporalio.Workflows;

namespace Shared.Contracts;

[Workflow]
public interface IOrderWorkflow
{
    [WorkflowRun]
    Task<OrderStatus> RunAsync(OrderDetails order);

    [WorkflowSignal("ReviewPayment")]
    Task ReviewPaymentAsync(PaymentApprovalStatus status);
}

