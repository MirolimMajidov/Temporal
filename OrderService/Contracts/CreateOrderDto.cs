namespace OrderService.Contracts;

public record CreateOrderDto(
    Guid CustomerId,
    Guid ItemId,
    int Quantity,
    decimal Amount,
    string Currency,
    string ShippingAddress,
    bool ShouldCommunicateWithPhp = false,
    bool ShouldConfirmedPayment = false,
    bool ShouldUseSignalToConfirmPayment = false,
    bool ShouldWaitChildWorkflows = false,
    bool ShouldFailDelivery = false);