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
    bool ShouldConfirmedPayment = false,
    bool ShouldUseSignalToConfirmPayment = false,
    bool ShouldFailDelivery = false);

public record PaymentRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string? WorkflowId = null,
    string? WorkflowRunId = null);

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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public record PaymentApproval(
    Guid PaymentId,
    PaymentApprovalStatus Status,
    DateTime CreatedAt,
    string? WorkflowId = null,
    string? WorkflowRunId = null,
    DateTime? ApprovedAt = null,
    string? ApprovedBy = null);
