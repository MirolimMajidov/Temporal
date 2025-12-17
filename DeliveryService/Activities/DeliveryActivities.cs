using DeliveryService.Services;
using Shared.Contracts;
using Temporalio.Activities;

namespace DeliveryService.Activities;

public class DeliveryActivities(IDeliveryService service)
{
    [Activity]
    public async Task<DeliveryResult> DeliveryAsync(DeliveryRequest  request)
    {
        if (request.ShouldFailDelivery)
        {
            return new DeliveryResult(Guid.Empty, Success: false);
        }
        else
        {
            var deliveryId = await service.DeliveryAsync(request);
            return new DeliveryResult(deliveryId, Success: true);
        }
    }
}