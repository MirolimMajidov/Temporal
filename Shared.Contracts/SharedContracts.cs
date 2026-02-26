using System.Text.Json.Serialization;

namespace Shared.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending,
    Paid,
    Reserved,
    Delivering,
    Completed,
    Failed
}

public record OrderDetails(
    Guid OrderId,
    Guid CustomerId,
    Guid ItemId,
    int Quantity,
    decimal Amount,
    string Currency,
    string ShippingAddress,
    bool ShouldCommunicateWithPhp = false,
    bool ShouldFailDelivery = false);

public record PaymentRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency);

public record PaymentResult(
    Guid PaymentId,
    bool Success,
    string? FailureReason = null);

public record InventoryReserveRequest(
    Guid OrderId,
    Guid ItemId,
    int Quantity);

public record InventoryReserveResult(
    Guid ReservationId,
    bool Success,
    string? FailureReason = null);

public record DeliveryRequest(
    Guid OrderId,
    Guid ReservationId,
    string ShippingAddress,
    bool ShouldFailDelivery);

public record DeliveryResult(
    Guid DeliveryId,
    bool Success,
    string? FailureReason = null);