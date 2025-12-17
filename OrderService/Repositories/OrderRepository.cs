namespace OrderService.Repositories;

public class OrderRepository : IOrderRepository
{
    public Task<bool> MarkCompletedAsync(Guid orderId)
    {
        return Task.FromResult(true);
    }

    public Task<bool> MarkFailedAsync(Guid orderId)
    {
        return Task.FromResult(true);
    }
}

public interface IOrderRepository
{
    Task<bool> MarkCompletedAsync(Guid orderId);
    
    Task<bool> MarkFailedAsync(Guid orderId);
}