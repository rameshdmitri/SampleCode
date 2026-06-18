namespace MyApp.Application.Orders.DTOs;

public sealed record UpdateOrderRequest(IReadOnlyList<CreateOrderItemRequest> Items);
