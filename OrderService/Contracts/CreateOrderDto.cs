namespace OrderService.Contracts;

public record CreateOrderDto(
    Guid CustomerId,
    Guid ItemId,
    int Quantity,
    decimal Amount,
    string Currency,
    string ShippingAddress,
    bool ShouldCommunicateWithPhp = false,
    bool ShouldFailDelivery = false);