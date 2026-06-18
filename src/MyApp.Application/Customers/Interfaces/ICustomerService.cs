namespace MyApp.Application.Customers.Interfaces;
using MyApp.Application.Common.Models;
using MyApp.Application.Customers.DTOs;

public interface ICustomerService
{
    Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<CustomerDto>>                GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CustomerDto>>                GetMyProfileAsync(CancellationToken ct = default);
    Task<Result>                             UpdateProfileAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
}
