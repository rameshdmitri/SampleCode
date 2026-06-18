namespace MyApp.Application.Orders.DTOs;
using MyApp.Domain.Entities;
using MyApp.Domain.Enums;

public sealed record OrderDto(
    Guid                     Id,
    Guid                     CustomerId,
    OrderStatus              Status,
    decimal                  Total,
    DateTime                 CreatedAt,
    IReadOnlyList<OrderItemDto> Items)
{
    public static OrderDto FromEntity(Order o) => new(
        o.Id,
        o.CustomerId,
        o.Status,
        o.Total.Amount,
        o.CreatedAt,
        o.Items.Select(i => new OrderItemDto(
            i.Id, i.ProductName, i.Quantity,
            i.UnitPrice.Amount, i.TotalPrice.Amount))
            .ToList().AsReadOnly());
}
