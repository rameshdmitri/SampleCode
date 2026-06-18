namespace MyApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using MyApp.Application.Auth.DTOs;
using MyApp.Application.Auth.Errors;
using MyApp.Application.Auth.Interfaces;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Common.Models;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser>            _userManager;
    private readonly SignInManager<AppUser>           _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IJwtTokenService                _jwtService;

    public AuthService(
        UserManager<AppUser>            userManager,
        SignInManager<AppUser>           signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IJwtTokenService                jwtService)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
        _roleManager   = roleManager;
        _jwtService    = jwtService;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);

        var signIn = await _signInManager
            .CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signIn.IsLockedOut)  return Result.Failure<LoginResponse>(AuthErrors.AccountLocked);
        if (!signIn.Succeeded)   return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);

        var roles = await _userManager.GetRolesAsync(user);
        var token = await _jwtService.GenerateAsync(user.Id, user.Email!, user.FullName, roles);

        return Result.Success(new LoginResponse(
            token, DateTime.UtcNow.AddHours(8), roles.ToList().AsReadOnly()));
    }

    public async Task<Result<Guid>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return Result.Failure<Guid>(AuthErrors.EmailAlreadyInUse);

        if (!await _roleManager.RoleExistsAsync(request.Role))
            return Result.Failure<Guid>(AuthErrors.RoleNotFound);

        var user = new AppUser
        {
            Email    = request.Email,
            UserName = request.Email,
            FullName = request.FullName,
            EmailConfirmed = true
        };

        var created = await _userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return Result.Failure<Guid>(new Error(
                "Auth.CreateFailed",
                string.Join(" | ", created.Errors.Select(e => e.Description))));

        await _userManager.AddToRoleAsync(user, request.Role);
        return Result.Success(user.Id);
    }
}
