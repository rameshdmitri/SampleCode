namespace MyApp.Application.Employees.Services;
using MyApp.Application.Auth.Errors;
using MyApp.Application.Common.Constants;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Common.Models;
using MyApp.Application.Common.Services;
using MyApp.Application.Employees.DTOs;
using MyApp.Application.Employees.Errors;
using MyApp.Application.Employees.Interfaces;

public sealed class EmployeeServices(
    IUnitOfWork  uow,
    IMapper      mapper,
    ICurrentUser currentUser)
    : AuthorizedService(currentUser), IEmployeeServices
{
    // ── Caso 1: Result<T> tipado + guard de ownership ─────────
    // Fail<T>(code, message) → 404 ; Fail<T>(AuthErrors.Forbidden) → 403
    public async Task<Result<EmployeeResponseDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var employee = await uow.Employees.GetByIdAsync(id, ct);
        if (employee is null)
            return Fail<EmployeeResponseDto>(EmployeeErrors.NotFound(id));        // → 404

        // Customer só vê o próprio cadastro; Admin/Manager veem todos
        if (!IsAdminOrManager && employee.UserId != CurrentUserId.ToString())
            return Fail<EmployeeResponseDto>(AuthErrors.Forbidden);               // → 403

        return Ok(mapper.Map<EmployeeResponseDto>(employee));                     // → 200
    }

    // ── Caso 2: Result sem valor (comando) + guard de role ────
    public async Task<Result> DeactivateAsync(int id, CancellationToken ct = default)
    {
        // O guard já devolve um Result pronto — basta propagar em caso de falha
        var guard = EnsureRole(AppRoles.Admin);
        if (guard.IsFailure)
            return guard;                                                        // → 403

        var employee = await uow.Employees.GetByIdAsync(id, ct);
        if (employee is null)
            return Fail(EmployeeErrors.NotFound(id));                            // → 404

        if (employee.UserId == CurrentUserId.ToString())
            return Fail(EmployeeErrors.CannotDeactivateSelf);                    // → 409

        employee.IsActive  = false;
        employee.UpdatedBy = CurrentUserId.ToString();   // auditoria sem parâmetro extra
        await uow.SaveAsync(ct);

        return Ok();                                                             // → 204 NoContent
    }

    // ── Caso 3: Result<T> com filtragem implícita por identidade ──
    // Sem guard: a identidade muda O QUE retorna, não SE é permitido
    public async Task<Result<IEnumerable<EmployeeResponseDto>>> GetMyTeamAsync(CancellationToken ct = default)
    {
        var employees = IsAdmin
            ? await uow.Employees.GetAllAsync(ct)                                    // Admin: todos
            : await uow.Employees.GetByManagerUserIdAsync(CurrentUserId.ToString(), ct); // Manager: só o time

        return Ok(mapper.Map<IEnumerable<EmployeeResponseDto>>(employees));
    }
}
