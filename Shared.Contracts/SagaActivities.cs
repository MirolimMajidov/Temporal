using Temporalio.Activities;

namespace Shared.Contracts;

public interface IPaymentActivities
{
    [Activity]
    public Task<PaymentResult> PayAsync(PaymentRequest request);

    [Activity]
    public Task RefundAsync(Guid paymentId);
}

public interface IInventoryActivities
{
    [Activity]
    public Task<InventoryReserveResult> ReserveInventoryAsync(
        InventoryReserveRequest request);

    [Activity]
    public Task RestockInventoryAsync(Guid reservationId);
}

public interface IDeliveryActivities
{
    [Activity]
    public Task<DeliveryResult> DeliveryAsync(DeliveryRequest request);
}