using Shared.Contracts;

namespace DeliveryService.Services;

public class DeliveryService : IDeliveryService
{
    public Task<Guid> DeliverAsync(DeliveryRequest request)
    {
        var deliveryId = Guid.NewGuid();
        return Task.FromResult(deliveryId);
    }
}

public interface IDeliveryService
{
    Task<Guid> DeliverAsync(DeliveryRequest request);
}