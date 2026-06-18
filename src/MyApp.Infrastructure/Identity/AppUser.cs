namespace MyApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public string   FullName  { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool     IsActive  { get; set; } = true;
}
