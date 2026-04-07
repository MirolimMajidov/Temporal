using OrderService.Attributes;
using OrderService.Repositories;
using Shared.Contracts;
using Temporalio.Activities;

namespace OrderService.Activities;

[TemporalTaskQueue(TemporalTaskQueues.OrderWorker)]
public class OrderActivities(IOrderRepository repository)
{
    [Activity("MarkAsCompleted2")]
    public async Task MarkAsCompletedAsync(Guid orderId)
    {
        await repository.MarkCompletedAsync(orderId);
    }

    [Activity]
    public async Task MarkOrderFailedAsync(Guid orderId)
    {
        await repository.MarkFailedAsync(orderId);
    }
}