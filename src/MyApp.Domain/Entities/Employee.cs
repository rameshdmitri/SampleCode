namespace MyApp.Domain.Entities;
using MyApp.Domain.Common;

public sealed class Employee : BaseEntity, IAggregateRoot
{
    // Id de negócio sequencial (além do Guid herdado de BaseEntity)
    public int     EmployeeNumber { get; private set; }
    public string  FullName       { get; private set; } = string.Empty;
    public string  Email          { get; private set; } = string.Empty;
    public string  UserId         { get; private set; } = string.Empty;
    public string? ManagerUserId  { get; private set; }
    public bool    IsActive       { get; set; } = true;
    public string? UpdatedBy      { get; set; }
    public DateTime CreatedAt     { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt    { get; set; }

    private Employee() { }

    public Employee(string fullName, string email, string userId, string? managerUserId = null)
    {
        FullName      = fullName;
        Email         = email;
        UserId        = userId;
        ManagerUserId = managerUserId;
    }
}
