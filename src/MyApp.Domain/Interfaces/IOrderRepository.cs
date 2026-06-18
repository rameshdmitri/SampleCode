namespace MyApp.Domain.Interfaces;
using MyApp.Domain.Entities;

public interface IOrderRepository
{
    Task<Order?>                  GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>>    GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Order>>    GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task                          AddAsync(Order order, CancellationToken ct = default);
    Task                          UpdateAsync(Order order, CancellationToken ct = default);
    Task                          DeleteAsync(Order order, CancellationToken ct = default);
}
