namespace MyApp.Domain.Entities;
using MyApp.Domain.Common;

public sealed class Customer : BaseEntity, IAggregateRoot
{
    public Guid   UserId    { get; private set; }
    public string FullName  { get; private set; } = string.Empty;
    public string Email     { get; private set; } = string.Empty;
    public string Phone     { get; private set; } = string.Empty;
    public bool   IsActive  { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Customer() { }

    public Customer(Guid userId, string fullName, string email, string phone = "")
    {
        UserId   = userId;
        FullName = fullName;
        Email    = email;
        Phone    = phone;
    }

    public void Update(string fullName, string phone)
    {
        FullName = fullName;
        Phone    = phone;
    }

    public void Deactivate() => IsActive = false;
}
