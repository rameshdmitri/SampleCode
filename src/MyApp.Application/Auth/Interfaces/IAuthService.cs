namespace MyApp.Application.Auth.Interfaces;
using MyApp.Application.Auth.DTOs;
using MyApp.Application.Common.Models;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<Guid>>          RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}
