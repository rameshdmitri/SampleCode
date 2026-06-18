namespace MyApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Common;
using MyApp.Infrastructure.Persistence;

public abstract class BaseRepository<T>(AppDbContext context) where T : BaseEntity
{
    protected readonly AppDbContext Context = context;
    protected readonly DbSet<T>     DbSet   = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.FindAsync([id], ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.ToListAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default) =>
        await DbSet.AddAsync(entity, ct);

    public virtual Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
