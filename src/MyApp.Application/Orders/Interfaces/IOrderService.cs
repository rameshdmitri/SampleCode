namespace MyApp.Application.Orders.Interfaces;
using MyApp.Application.Common.Models;
using MyApp.Application.Orders.DTOs;

public interface IOrderService
{
    Task<Result<OrderDto>>                   GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<OrderDto>>>    GetAllAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<OrderDto>>>    GetMyOrdersAsync(CancellationToken ct = default);
    Task<Result<Guid>>                       CreateAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<Result>                             CancelAsync(Guid id, CancellationToken ct = default);
    Task<Result>                             DeleteAsync(Guid id, CancellationToken ct = default);
}
