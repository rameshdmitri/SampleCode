namespace MyApp.Application.Employees.Interfaces;
using MyApp.Application.Common.Models;
using MyApp.Application.Employees.DTOs;

public interface IEmployeeServices
{
    Task<Result<EmployeeResponseDto>>              GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result>                                   DeactivateAsync(int id, CancellationToken ct = default);
    Task<Result<IEnumerable<EmployeeResponseDto>>> GetMyTeamAsync(CancellationToken ct = default);
}
