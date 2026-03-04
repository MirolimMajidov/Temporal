using OrderService.Activities;
using Shared.Contracts;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace OrderService.Workflows;

[Workflow]
public class OrderProcessWorkflow : IOrderWorkflow
{
    private readonly ILogger _logger = Workflow.Logger;
    private PaymentApprovalStatus _approvalStatus = PaymentApprovalStatus.Pending;

    [WorkflowSignal("ReviewPayment")]
    public Task ReviewPaymentAsync(PaymentApprovalStatus status)
    {
        _approvalStatus = status;
        return Task.CompletedTask;
    }

    [WorkflowRun]
    public async Task<OrderStatus> RunAsync(OrderDetails order)
    {
        var compensations = new Stack<Func<Task>>();
        PaymentResult payment;
        InventoryReserveResult inventory;
        DeliveryResult delivery;

        try
        {
            // Rolling back 1: Mark order failed (Order service)
            compensations.Push(async () => await Workflow.ExecuteActivityAsync(
                (OrderActivities act) => act.MarkOrderFailedAsync(order.OrderId),
                new()
                {
                    TaskQueue = TaskQueues.OrderOrchestration,
                    StartToCloseTimeout = TimeSpan.FromMinutes(1)
                }));

            // 1. Charge payment (Payment service)
            var paymentRequest = new PaymentRequest(
                order.OrderId,
                order.CustomerId,
                order.Amount,
                order.Currency,
                Workflow.Info.WorkflowId,
                Workflow.Info.RunId);

            payment = await Workflow.ExecuteActivityAsync(
                (IPaymentActivities act) => act.PayAsync(paymentRequest),
                new()
                {
                    TaskQueue = order.ShouldCommunicateWithPhp ? TaskQueues.PaymentWithPhp : TaskQueues.Payment,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2)
                });

            if (!payment.Success)
                throw new ApplicationFailureException(
                    $"Payment failed for order {order.OrderId}: {payment.FailureReason}");

            if (order.ShouldConfirmedPayment)
            {
                if (order.ShouldUseSignalToConfirmPayment)
                {
                    // Wait for payment approval (via Signal)
                    _logger.LogInformation("Waiting for payment approval signal for order {OrderId}", order.OrderId);
                    var signalReceived = await Workflow.WaitConditionAsync(
                        () => _approvalStatus != PaymentApprovalStatus.Pending,
                        TimeSpan.FromMinutes(10)); // Timeout after 10 minutes

                    if (!signalReceived)
                        _approvalStatus = PaymentApprovalStatus.Rejected;
                }
                else
                {
                    // Wait for payment approval (Manually from payment service)
                    var paymentStatus = await Workflow.ExecuteActivityAsync(
                        (IPaymentActivities act) => act.WaitPaymentApprovalAsync(payment.PaymentId),
                        new()
                        {
                            TaskQueue = order.ShouldCommunicateWithPhp ? TaskQueues.PaymentWithPhp : TaskQueues.Payment,
                            StartToCloseTimeout = TimeSpan.FromMinutes(5)
                        });
                    _approvalStatus = paymentStatus ? PaymentApprovalStatus.Approved : PaymentApprovalStatus.Rejected;
                }

                if (_approvalStatus == PaymentApprovalStatus.Rejected)
                    throw new ApplicationFailureException($"Payment rejected for order {order.OrderId}");
            }

            // Rolling back 2: refund payment
            compensations.Push(async () => await Workflow.ExecuteActivityAsync(
                (IPaymentActivities act) => act.RefundAsync(payment.PaymentId),
                new()
                {
                    TaskQueue = order.ShouldCommunicateWithPhp ? TaskQueues.PaymentWithPhp : TaskQueues.Payment,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2),
                    // No cancellation token: we want compensation to run even if workflow is being canceled
                }));

            // 2. Reserve inventory (Inventory service)
            // Parallel execution example: First one check if product exists, and at the same time second one reserve inventory.
            var inventoryTask1 = Workflow.ExecuteActivityAsync(
                (IInventoryActivities act) => act.ReservingProductExistsAsync(order.ItemId),
                new()
                {
                    TaskQueue = TaskQueues.Inventory,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2)
                });
            var inventoryTask2 = Workflow.ExecuteActivityAsync(
                (IInventoryActivities act) => act.ReserveInventoryAsync(
                    new InventoryReserveRequest(order.OrderId, order.ItemId, order.Quantity)),
                new()
                {
                    TaskQueue = TaskQueues.Inventory,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2)
                });

            await Task.WhenAll(inventoryTask1, inventoryTask2);

            inventory = inventoryTask2.Result;
            if (!inventory.Success)
                throw new ApplicationFailureException(
                    $"Inventory reservation failed for order {order.OrderId}: {inventory.FailureReason}");

            // Rolling back 3: restock inventory
            compensations.Push(async () => await Workflow.ExecuteActivityAsync(
                (IInventoryActivities act) =>
                    act.RestockInventoryAsync(inventory!.ReservationId),
                new()
                {
                    TaskQueue = TaskQueues.Inventory,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2)
                }));

            // 3. Delivery product (Delivery service)
            delivery = await Workflow.ExecuteActivityAsync(
                (IDeliveryActivities act) => act.DeliveryAsync(
                    new DeliveryRequest(order.OrderId,
                        inventory.ReservationId,
                        order.ShippingAddress,
                        order.ShouldFailDelivery)),
                new()
                {
                    TaskQueue = TaskQueues.Delivery,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2)
                });

            if (!delivery.Success)
                throw new ApplicationFailureException(
                    $"Delivery product failed for order {order.OrderId}: {delivery.FailureReason}");

            // Mark order as completed (Order service)
            await Workflow.ExecuteActivityAsync(
                (OrderActivities act) =>
                    act.MarkAsCompletedAsync(order.OrderId),
                new()
                {
                    TaskQueue = TaskQueues.OrderOrchestration,
                    StartToCloseTimeout = TimeSpan.FromMinutes(1)
                });

            return OrderStatus.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failure for order {OrderId}, compensating", order.OrderId);
            await RunCompensationsAsync(compensations);
            throw; // Workflow will fail, but side effects have been compensated
        }
    }

    /// <summary>
    /// For rolling back actions in reverse order.
    /// </summary>
    /// <param name="compensations">Rolling back actions to run.</param>
    private async Task RunCompensationsAsync(Stack<Func<Task>> compensations)
    {
        while (compensations.Count > 0)
        {
            var comp = compensations.Pop();
            try
            {
                await comp();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compensation failed");
                // Usually log for manual intervention
            }
        }
    }
}