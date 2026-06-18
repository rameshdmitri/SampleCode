namespace MyApp.Application.Customers.Errors;
using MyApp.Application.Common.Models;

public static class CustomerErrors
{
    public static readonly Error NotFound          = new("Customer.NotFound", "Customer not found.");
    public static readonly Error EmailAlreadyInUse = new("Customer.Conflict", "Email already in use.");
}
