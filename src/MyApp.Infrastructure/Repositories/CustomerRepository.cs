namespace MyApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Entities;
using MyApp.Domain.Interfaces;
using MyApp.Infrastructure.Persistence;

public sealed class CustomerRepository(AppDbContext context)
    : BaseRepository<Customer>(context), ICustomerRepository
{
    public async Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(c => c.UserId == userId, ct);
}
