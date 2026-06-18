namespace MyApp.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Entities;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EmployeeNumber).ValueGeneratedOnAdd();
        builder.HasIndex(e => e.EmployeeNumber).IsUnique();
        builder.Property(e => e.FullName).HasMaxLength(200);
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.UserId).HasMaxLength(450);
        builder.Property(e => e.ManagerUserId).HasMaxLength(450);
    }
}
