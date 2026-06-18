namespace MyApp.Infrastructure.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyApp.Application.Common.Constants;
using MyApp.Application.Common.Interfaces;

public sealed class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid Id
    {
        get
        {
            var value = User?.FindFirstValue(AppClaimTypes.UserId);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public string Email    => User?.FindFirstValue(AppClaimTypes.Email) ?? string.Empty;
    public string Name     => User?.FindFirstValue(AppClaimTypes.Name)  ?? string.Empty;
    public string UserName => User?.FindFirstValue(AppClaimTypes.Email) ?? string.Empty;

    public IReadOnlyList<string> Roles =>
        User is null ? [] : User.FindAll(AppClaimTypes.Role).Select(c => c.Value).ToList();

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public bool IsOwnerOrAdmin(Guid resourceOwnerId) =>
        Id == resourceOwnerId || IsInRole(AppRoles.Admin);
}
