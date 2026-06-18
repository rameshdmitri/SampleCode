namespace MyApp.Application.Common.Interfaces;
using MyApp.Domain.Interfaces;

public interface IUnitOfWork
{
    IEmployeeRepository Employees { get; }
    IOrderRepository    Orders    { get; }
    ICustomerRepository Customers { get; }

    Task<int> CommitAsync(CancellationToken ct = default);
    Task<int> SaveAsync(CancellationToken ct = default);
}
