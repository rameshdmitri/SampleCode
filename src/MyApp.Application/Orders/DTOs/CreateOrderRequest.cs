namespace MyApp.Application.Orders.DTOs;

public sealed record CreateOrderItemRequest(string ProductName, int Quantity, decimal UnitPrice);
public sealed record CreateOrderRequest(IReadOnlyList<CreateOrderItemRequest> Items);
