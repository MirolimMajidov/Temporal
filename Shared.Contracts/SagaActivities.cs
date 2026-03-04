using Temporalio.Activities;

namespace Shared.Contracts;

public interface IPaymentActivities
{
    [Activity]
    public Task<PaymentResult> PayAsync(PaymentRequest request);

    [Activity]
    public Task<bool> WaitPaymentApprovalAsync(Guid paymentId);

    [Activity]
    public Task RefundAsync(Guid paymentId);
}

public interface IInventoryActivities
{
    [Activity]
    public Task<InventoryReserveResult> ReserveInventoryAsync(
        InventoryReserveRequest request);
    
    [Activity]
    public Task<bool> ReservingProductExistsAsync(Guid itemId);

    [Activity]
    public Task RestockInventoryAsync(Guid reservationId);
}

public interface IDeliveryActivities
{
    [Activity]
    public Task<DeliveryResult> DeliveryAsync(DeliveryRequest request);
}