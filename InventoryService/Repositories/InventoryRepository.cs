namespace InventoryService.Repositories;

public class InventoryRepository : IInventoryRepository
{
    public Task<Guid> ReserveInventoryAsync(Guid itemId, int quantity, Guid orderId)
    {
        var reservationId = Guid.NewGuid();
        return Task.FromResult(reservationId);
    }

    public Task<bool> RestockInventoryAsync(Guid reservationId)
    {
        return Task.FromResult(true);
    }
}

public interface IInventoryRepository
{
    Task<Guid> ReserveInventoryAsync(Guid itemId, int quantity, Guid orderId);
    
    Task<bool> RestockInventoryAsync(Guid reservationId);
}