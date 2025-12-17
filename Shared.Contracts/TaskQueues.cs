namespace Shared.Contracts;

public static class TaskQueues
{
    public const string OrderOrchestration = "order-orchestration-tq";
    public const string Payment = "payment-tq";
    public const string Inventory = "inventory-tq";
    public const string Delivery = "delivery-tq";
}
