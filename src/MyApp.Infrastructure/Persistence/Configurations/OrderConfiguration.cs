namespace MyApp.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Entities;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).HasConversion<string>();
        builder.OwnsMany(o => o.Items, item =>
        {
            item.HasKey(i => i.Id);
            item.OwnsOne(i => i.UnitPrice, price =>
            {
                price.Property(p => p.Amount).HasColumnName("UnitPrice");
                price.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
        });
        builder.Navigation(o => o.Items).Metadata.SetField("_items");
        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(o => o.Total);
    }
}
