namespace OrderService.Repositories;

public class OrderRepository : IOrderRepository
{
    public async Task<bool> MarkCompletedAsync(Guid orderId)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        return await Task.FromResult(true);
    }

    public async Task<bool> MarkFailedAsync(Guid orderId)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        return await Task.FromResult(true);
    }
}

public interface IOrderRepository
{
    Task<bool> MarkCompletedAsync(Guid orderId);
    
    Task<bool> MarkFailedAsync(Guid orderId);
}