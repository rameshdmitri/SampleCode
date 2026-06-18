namespace MyApp.Application.Employees.DTOs;

public sealed record CreateEmployeeRequest(
    string  FullName,
    string  Email,
    string  UserId,
    string? ManagerUserId);
