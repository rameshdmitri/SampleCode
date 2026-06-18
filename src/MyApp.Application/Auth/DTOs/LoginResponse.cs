namespace MyApp.Application.Auth.DTOs;

public sealed record LoginResponse(
    string              Token,
    DateTime            ExpiresAt,
    IReadOnlyList<string> Roles);
