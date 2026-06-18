namespace MyApp.Application.Customers.DTOs;
using MyApp.Domain.Entities;

public sealed record CustomerDto(Guid Id, Guid UserId, string FullName, string Email, string Phone, bool IsActive)
{
    public static CustomerDto FromEntity(Customer c) =>
        new(c.Id, c.UserId, c.FullName, c.Email, c.Phone, c.IsActive);
}
