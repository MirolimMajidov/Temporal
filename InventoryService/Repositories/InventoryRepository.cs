namespace InventoryService.Repositories;

public class InventoryRepository : IInventoryRepository
{
    public async Task<Guid> ReserveInventoryAsync(Guid itemId, int quantity, Guid orderId)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        var reservationId = Guid.NewGuid();
        return await Task.FromResult(reservationId);
    }

    public async Task<bool> RestockInventoryAsync(Guid reservationId)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        return await Task.FromResult(true);
    }
}

public interface IInventoryRepository
{
    Task<Guid> ReserveInventoryAsync(Guid itemId, int quantity, Guid orderId);
    
    Task<bool> RestockInventoryAsync(Guid reservationId);
}