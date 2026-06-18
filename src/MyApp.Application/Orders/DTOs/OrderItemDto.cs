namespace MyApp.Application.Orders.DTOs;

public sealed record OrderItemDto(
    Guid   Id,
    string ProductName,
    int    Quantity,
    decimal UnitPrice,
    decimal TotalPrice);
