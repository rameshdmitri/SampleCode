namespace MyApp.Application.Employees.Errors;
using MyApp.Application.Common.Models;

public static class EmployeeErrors
{
    public static Error NotFound(int id) =>
        new("Employee.NotFound", $"Employee {id} not found.");

    public static readonly Error CannotDeactivateSelf =
        new("Employee.Conflict", "You cannot deactivate your own account.");
}
