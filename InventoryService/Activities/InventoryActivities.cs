using InventoryService.Repositories;
using Shared.Contracts;
using Temporalio.Activities;

namespace InventoryService.Activities;

public class InventoryActivities(IInventoryRepository repository) : IInventoryActivities
{
    [Activity]
    public async Task<InventoryReserveResult> ReserveInventoryAsync(
        InventoryReserveRequest request)
    {
        var reservationId = await repository.ReserveInventoryAsync(
            request.ItemId, request.Quantity, request.OrderId);

        await Task.Delay(TimeSpan.FromSeconds(2));

        return new InventoryReserveResult(reservationId, Success: true);
    }

    [Activity]
    public async Task<bool> ReservingProductExistsAsync(Guid itemId)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        return true;
    }

    [Activity]
    public async Task RestockInventoryAsync(Guid reservationId)
    {
        await repository.RestockInventoryAsync(reservationId);
    }
}