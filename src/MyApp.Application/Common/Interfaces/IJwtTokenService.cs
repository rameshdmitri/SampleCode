namespace MyApp.Application.Common.Interfaces;

public interface IJwtTokenService
{
    Task<string> GenerateAsync(Guid userId, string email, string name, IEnumerable<string> roles);
}
