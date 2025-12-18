using OrderService.Activities;
using Shared.Contracts;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace OrderService.Workflows;

[Workflow]
public class OrderProcessWorkflow
{
    private readonly ILogger _logger = Workflow.Logger;

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
            var paymentRequest = new PaymentRequest(order.OrderId, order.CustomerId, order.Amount, order.Currency);
            // payment = await Workflow.ExecuteActivityAsync(
            //     (IPaymentActivities act) => act.PayAsync(paymentRequest),
            //     new()
            //     {
            //         TaskQueue = TaskQueues.Payment,
            //         StartToCloseTimeout = TimeSpan.FromMinutes(2)
            //     });
            payment = await Workflow.ExecuteActivityAsync<PaymentResult>(
                activity: "Pay",
                args: [paymentRequest],
                new()
                {
                    TaskQueue = TaskQueues.Payment,
                    StartToCloseTimeout = TimeSpan.FromMinutes(2)
                });

            if (!payment.Success)
                throw new ApplicationFailureException($"Payment failed for order {order.OrderId}: {payment.FailureReason}");

            // Rolling back 2: refund payment
            compensations.Push(async () => await Workflow.ExecuteActivityAsync(
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
                throw new ApplicationFailureException($"Inventory reservation failed for order {order.OrderId}: {inventory.FailureReason}");
            
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
                throw new ApplicationFailureException($"Delivery product failed for order {order.OrderId}: {delivery.FailureReason}");

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