namespace MyApp.Application.Orders.Services;
using MyApp.Application.Common.Constants;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Common.Models;
using MyApp.Application.Common.Services;
using MyApp.Application.Orders.DTOs;
using MyApp.Application.Orders.Errors;
using MyApp.Application.Orders.Interfaces;
using MyApp.Application.Orders.Validators;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;
using MyApp.Domain.ValueObjects;

public sealed class OrderService(
    IOrderRepository     repository,
    IUnitOfWork          unitOfWork,
    ICurrentUser         currentUser,
    CreateOrderValidator validator)
    : AuthorizedService(currentUser), IOrderService
{
    public async Task<Result<OrderDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await repository.GetByIdAsync(id, ct);
        if (order is null)
            return Fail<OrderDto>(OrderErrors.NotFound);

        if (IsCustomer)
        {
            var guard = EnsureOwnerOrAdmin(order.CustomerId);
            if (guard.IsFailure)
                return Fail<OrderDto>(guard.Error);
        }

        return Ok(OrderDto.FromEntity(order));
    }

    public async Task<Result<IReadOnlyList<OrderDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var orders = await repository.GetAllAsync(ct);
        return Ok<IReadOnlyList<OrderDto>>(orders.Select(OrderDto.FromEntity).ToList());
    }

    public async Task<Result<IReadOnlyList<OrderDto>>> GetMyOrdersAsync(CancellationToken ct = default)
    {
        var orders = await repository.GetByCustomerIdAsync(CurrentUserId, ct);
        return Ok<IReadOnlyList<OrderDto>>(orders.Select(OrderDto.FromEntity).ToList());
    }

    public async Task<Result<Guid>> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Fail<Guid>("Order.Validation",
                string.Join(" | ", validation.Errors.Select(e => e.ErrorMessage)));

        var items = request.Items.Select(i => (i.ProductName, i.Quantity, new Money(i.UnitPrice)));
        var order = new Order(CurrentUserId, items);

        await repository.AddAsync(order, ct);
        await unitOfWork.CommitAsync(ct);
        return Ok(order.Id);
    }

    public async Task<Result> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var order = await repository.GetByIdAsync(id, ct);
        if (order is null)
            return Fail(OrderErrors.NotFound);

        var guard = EnsureOwnerOrAdmin(order.CustomerId);
        if (guard.IsFailure) return guard;

        try   { order.Cancel(); }
        catch { return Fail(OrderErrors.CannotCancel); }

        await repository.UpdateAsync(order, ct);
        await unitOfWork.CommitAsync(ct);
        return Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var order = await repository.GetByIdAsync(id, ct);
        if (order is null)
            return Fail(OrderErrors.NotFound);

        await repository.DeleteAsync(order, ct);
        await unitOfWork.CommitAsync(ct);
        return Ok();
    }
}
