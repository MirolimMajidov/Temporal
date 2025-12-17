using DeliveryService.Services;
using Shared.Contracts;
using Temporalio.Activities;

namespace DeliveryService.Activities;

public class DeliveryActivities(IDeliveryService service)
{
    [Activity]
    public async Task<DeliveryResult> ScheduleDeliveryAsync(DeliveryRequest  request)
    {
        var deliveryId = await service.DeliverAsync(request);
        return new DeliveryResult(deliveryId, Success: true);
    }
}