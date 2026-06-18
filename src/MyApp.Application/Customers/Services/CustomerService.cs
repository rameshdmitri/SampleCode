namespace MyApp.Application.Customers.Services;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Common.Models;
using MyApp.Application.Common.Services;
using MyApp.Application.Customers.DTOs;
using MyApp.Application.Customers.Errors;
using MyApp.Application.Customers.Interfaces;
using MyApp.Domain.Interfaces;

public sealed class CustomerService(
    ICustomerRepository repository,
    IUnitOfWork         unitOfWork,
    ICurrentUser        currentUser)
    : AuthorizedService(currentUser), ICustomerService
{
    public async Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var customers = await repository.GetAllAsync(ct);
        return Ok<IReadOnlyList<CustomerDto>>(customers.Select(CustomerDto.FromEntity).ToList());
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdAsync(id, ct);
        if (customer is null)
            return Fail<CustomerDto>(CustomerErrors.NotFound);

        var guard = EnsureOwnerOrAdmin(customer.UserId);
        if (guard.IsFailure)
            return Fail<CustomerDto>(guard.Error);

        return Ok(CustomerDto.FromEntity(customer));
    }

    public async Task<Result<CustomerDto>> GetMyProfileAsync(CancellationToken ct = default)
    {
        var customer = await repository.GetByUserIdAsync(CurrentUserId, ct);
        return customer is null
            ? Fail<CustomerDto>(CustomerErrors.NotFound)
            : Ok(CustomerDto.FromEntity(customer));
    }

    public async Task<Result> UpdateProfileAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await repository.GetByIdAsync(id, ct);
        if (customer is null)
            return Fail(CustomerErrors.NotFound);

        var guard = EnsureOwnerOrAdmin(customer.UserId);
        if (guard.IsFailure) return guard;

        customer.Update(request.FullName, request.Phone);
        await repository.UpdateAsync(customer, ct);
        await unitOfWork.CommitAsync(ct);
        return Ok();
    }
}
