namespace MyApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;
using MyApp.Infrastructure.Persistence;

public sealed class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    private readonly DbSet<Employee> _set = context.Set<Employee>();

    public async Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await _set.FirstOrDefaultAsync(e => e.EmployeeNumber == id, ct);

    public async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken ct = default) =>
        await _set.OrderBy(e => e.FullName).ToListAsync(ct);

    public async Task<IReadOnlyList<Employee>> GetByManagerUserIdAsync(string managerUserId, CancellationToken ct = default) =>
        await _set.Where(e => e.ManagerUserId == managerUserId).ToListAsync(ct);

    public async Task AddAsync(Employee employee, CancellationToken ct = default) =>
        await _set.AddAsync(employee, ct);

    public Task UpdateAsync(Employee employee, CancellationToken ct = default)
    {
        _set.Update(employee);
        return Task.CompletedTask;
    }
}
