namespace MyApp.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.Auth.DTOs;
using MyApp.Application.Auth.Interfaces;

[AllowAnonymous]
public sealed class AuthController(IAuthService authService) : ApiControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        => HandleResult(await authService.LoginAsync(request, ct));

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        => HandleResult(await authService.RegisterAsync(request, ct));
}
