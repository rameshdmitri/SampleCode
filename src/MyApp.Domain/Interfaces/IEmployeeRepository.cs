namespace MyApp.Domain.Interfaces;
using MyApp.Domain.Entities;

public interface IEmployeeRepository
{
    Task<Employee?>                GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>>  GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Employee>>  GetByManagerUserIdAsync(string managerUserId, CancellationToken ct = default);
    Task                           AddAsync(Employee employee, CancellationToken ct = default);
    Task                           UpdateAsync(Employee employee, CancellationToken ct = default);
}
