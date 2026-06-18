namespace MyApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;
using MyApp.Infrastructure.Persistence;

public sealed class OrderRepository(AppDbContext context)
    : BaseRepository<Order>(context), IOrderRepository
{
    public override async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public override async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.Include(o => o.Items).OrderByDescending(o => o.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default) =>
        await DbSet.Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
}
