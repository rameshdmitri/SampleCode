namespace MyApp.Application.Common.Services;
using MyApp.Application.Auth.Errors;
using MyApp.Application.Common.Constants;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Common.Models;

/// <summary>
/// Estende BaseService adicionando acesso ao usuário corrente
/// e guards de autorização. Services que precisam de identidade
/// herdam desta classe; os que não precisam herdam de BaseService.
/// </summary>
public abstract class AuthorizedService(ICurrentUser currentUser) : BaseService
{
    // ── Identidade ────────────────────────────────────────────

    protected Guid   CurrentUserId    => currentUser.Id;
    protected string CurrentUserEmail => currentUser.Email;
    protected string CurrentUserName  => currentUser.Name;
    protected bool   IsAuthenticated  => currentUser.IsAuthenticated;

    // ── Perfis ────────────────────────────────────────────────

    protected bool IsAdmin          => currentUser.IsInRole(AppRoles.Admin);
    protected bool IsManager        => currentUser.IsInRole(AppRoles.Manager);
    protected bool IsSupport        => currentUser.IsInRole(AppRoles.Support);
    protected bool IsCustomer       => currentUser.IsInRole(AppRoles.Customer);
    protected bool IsAdminOrManager => IsAdmin || IsManager;

    // ── Guards ────────────────────────────────────────────────

    protected Result EnsureAuthenticated() =>
        IsAuthenticated ? Ok() : Fail(AuthErrors.Unauthenticated);

    protected Result EnsureRole(string role) =>
        currentUser.IsInRole(role) ? Ok() : Fail(AuthErrors.Forbidden);

    protected Result EnsureOwnerOrAdmin(Guid ownerId) =>
        IsAdmin || CurrentUserId == ownerId ? Ok() : Fail(AuthErrors.Forbidden);
}
