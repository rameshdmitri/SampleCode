namespace MyApp.Domain.Interfaces;
using MyApp.Domain.Entities;

public interface ICustomerRepository
{
    Task<Customer?>             GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer?>             GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task                        AddAsync(Customer customer, CancellationToken ct = default);
    Task                        UpdateAsync(Customer customer, CancellationToken ct = default);
}
