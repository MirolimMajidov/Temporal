using InventoryService.Repositories;
using Shared.Contracts;
using Temporalio.Activities;

namespace InventoryService.Activities;

public class InventoryActivities(IInventoryRepository repository)
{
    [Activity]
    public async Task<InventoryReserveResult> ReserveInventoryAsync(
        InventoryReserveRequest request)
    {
        var reservationId = await repository.ReserveInventoryAsync(
            request.ItemId, request.Quantity, request.OrderId);

        return new InventoryReserveResult(reservationId, Success: true);
    }

    [Activity]
    public async Task RestockInventoryAsync(Guid reservationId)
    {
        await repository.RestockInventoryAsync(reservationId);
    }
}