namespace MyApp.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Common.Constants;
using MyApp.Application.Employees.Interfaces;

[Authorize]
public sealed class EmployeesController(IEmployeeServices employeeServices)
    : ApiControllerBase
{
    // GET /api/employees/{id}
    // Todos os roles entram; o Service decide via ownership (Caso 1)
    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.All)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => HandleResult(await employeeServices.GetByIdAsync(id, ct));

    // PATCH /api/employees/{id}/deactivate
    // Admin ou Operator no atributo; EnsureRole(Admin) refina no Service (Caso 2)
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = AppRoles.AdminOrOperator)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
        => HandleResult(await employeeServices.DeactivateAsync(id, ct));

    // GET /api/employees/my-team
    // Admin e Manager; a identidade filtra o resultado (Caso 3)
    [HttpGet("my-team")]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public async Task<IActionResult> GetMyTeam(CancellationToken ct)
        => HandleResult(await employeeServices.GetMyTeamAsync(ct));
}
