using OrderService.Repositories;
using Temporalio.Activities;

namespace OrderService.Activities;

public class OrderActivities(IOrderRepository repository)
{
    [Activity]
    public async Task MarkOrderCompletedAsync(Guid orderId)
        => await repository.MarkCompletedAsync(orderId);

    [Activity]
    public async Task MarkOrderFailedAsync(Guid orderId)
        => await repository.MarkFailedAsync(orderId);
}