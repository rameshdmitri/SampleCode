namespace MyApp.Infrastructure.Persistence;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Interfaces;
using MyApp.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context  = context;
        Employees = new EmployeeRepository(context);
        Orders    = new OrderRepository(context);
        Customers = new CustomerRepository(context);
    }

    public IEmployeeRepository Employees { get; }
    public IOrderRepository    Orders    { get; }
    public ICustomerRepository Customers { get; }

    public Task<int> CommitAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
    public Task<int> SaveAsync(CancellationToken ct = default)   => _context.SaveChangesAsync(ct);
}
