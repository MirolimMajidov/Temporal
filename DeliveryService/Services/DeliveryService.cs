using Shared.Contracts;
using Temporalio.Exceptions;

namespace DeliveryService.Services;

public class DeliveryService : IDeliveryService
{
    public async Task<Guid> DeliveryAsync(DeliveryRequest request)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        var deliveryId = Guid.NewGuid();
        //throw new ApplicationFailureException("The delivery service is currently unavailable.");
        return await Task.FromResult(deliveryId);
    }
}

public interface IDeliveryService
{
    Task<Guid> DeliveryAsync(DeliveryRequest request);
}