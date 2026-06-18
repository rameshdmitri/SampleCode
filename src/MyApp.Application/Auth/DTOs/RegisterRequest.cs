namespace MyApp.Application.Auth.DTOs;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Role);
