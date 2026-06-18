namespace MyApp.Application.Employees.DTOs;
using MyApp.Domain.Entities;

public sealed record EmployeeResponseDto(
    Guid    Id,
    int     EmployeeNumber,
    string  FullName,
    string  Email,
    string  UserId,
    string? ManagerUserId,
    bool    IsActive)
{
    public static EmployeeResponseDto FromEntity(Employee e) => new(
        e.Id, e.EmployeeNumber, e.FullName, e.Email, e.UserId, e.ManagerUserId, e.IsActive);
}
