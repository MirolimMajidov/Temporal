using OrderService.Activities;
using Shared.Contracts;
using Temporalio.Workflows;

namespace OrderService.Workflows;

[Workflow]
public class ProcessOrderWorkflow
{
    private readonly ILogger _logger = Workflow.Logger;

    [WorkflowRun]
    public async Task<OrderStatus> RunAsync(OrderDetails order)
    {
        var compensations = new List<Func<Task>>();
        PaymentResult? payment = null;
        InventoryReserveResult? inventory = null;
        DeliveryResult? delivery = null;

        try
        {
            // Compensation 1: Mark order failed
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (OrderActivities act) => act.MarkOrderFailedAsync(order.OrderId),
                new()
                {
                    TaskQueue = TaskQueues.OrderOrchestration,
                    StartToCloseTimeout = TimeSpan.FromMinutes(1)
                }));
            
            // 1. Charge payment (Payment service)
            payment = await Workflow.ExecuteActivityAsync(
                (IPaymentActivities act) => act.PayAsync(
                    new PaymentRequest(order.OrderId, order.CustomerId, order.Amount, order.Currency)),
                new()
                {
                    TaskQueue = TaskQueues.Payment,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2)
                });

            if (!payment.Success)
            {
                _logger.LogWarning("Payment failed for order {OrderId}: {Reason}",
                    order.OrderId, payment.FailureReason);
                return OrderStatus.Failed;
            }

            // Compensation 2: refund payment
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (IPaymentActivities act) => act.RefundPaymentAsync(payment.PaymentId),
                new()
                {
                    TaskQueue = TaskQueues.Payment,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2),
                    // No cancellation token: we want compensation to run even if workflow is being cancelled
                }));

            // 2. Reserve inventory (Inventory service)
            inventory = await Workflow.ExecuteActivityAsync(
                (IInventoryActivities act) => act.ReserveInventoryAsync(
                    new InventoryReserveRequest(order.OrderId, order.ItemId, order.Quantity)),
                new()
                {
                    TaskQueue = TaskQueues.Inventory,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2)
                });
            
            if (!inventory.Success)
            {
                _logger.LogWarning("Inventory reservation failed for order {OrderId}: {Reason}",
                    order.OrderId, inventory.FailureReason);
                // Run compensations (refund payment)
                await RunCompensationsAsync(compensations);
                return OrderStatus.Failed;
            }
            
            // Compensation 3: restock inventory
            compensations.Add(() => Workflow.ExecuteActivityAsync(
                (IInventoryActivities act) =>
                    act.RestockInventoryAsync(inventory!.ReservationId),
                new()
                {
                    TaskQueue = TaskQueues.Inventory,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2)
                }));
            
            // 3. Arrange delivery (Delivery service)
            delivery = await Workflow.ExecuteActivityAsync(
                (IDeliveryActivities act) => act.ScheduleDeliveryAsync(
                    new DeliveryRequest(order.OrderId,
                                        inventory.ReservationId,
                                        order.ShippingAddress)),
                new()
                {
                    TaskQueue = TaskQueues.Delivery,
                    StartToCloseTimeout = TimeSpan.FromMinutes(5)
                });
            
            if (!delivery.Success)
            {
                _logger.LogWarning("Delivery scheduling failed for order {OrderId}: {Reason}",
                    order.OrderId, delivery.FailureReason);
                // Run compensations (release inventory, refund payment)
                await RunCompensationsAsync(compensations);
                return OrderStatus.Failed;
            }

            // Success – mark order completed in Order service
            await Workflow.ExecuteActivityAsync(
                (OrderActivities act) =>
                    act.MarkOrderCompletedAsync(order.OrderId),
                new()
                {
                    TaskQueue = TaskQueues.OrderOrchestration,
                    StartToCloseTimeout = TimeSpan.FromMinutes(1)
                });

            return OrderStatus.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure for order {OrderId}, compensating", order.OrderId);
            await RunCompensationsAsync(compensations);
            throw; // Workflow will fail, but side effects have been compensated
        }
    }

    private async Task RunCompensationsAsync(List<Func<Task>> compensations)
    {
        // Run in reverse order like a classic Saga
        compensations.Reverse();
        foreach (var comp in compensations)
        {
            try
            {
                await comp();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Compensation failed");
                // Usually log for manual intervention
            }
        }
    }
}