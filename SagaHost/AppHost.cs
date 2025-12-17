var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject("order-service", "../OrderService/OrderService.csproj");
builder.AddProject("payment-service", "../PaymentService/PaymentService.csproj");
builder.AddProject("inventory-service", "../InventoryService/InventoryService.csproj");
builder.AddProject("delivery-service", "../DeliveryService/DeliveryService.csproj");

builder.Build().Run();