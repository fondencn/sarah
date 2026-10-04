using Microsoft.EntityFrameworkCore;

namespace Sarah.Statistics.WebApi.Data.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly StatisticsDbContext _context;
    private readonly DbSet<T> _entities;

    public Repository(StatisticsDbContext context)
    {
        _context = context;
        _entities = context.Set<T>();
    }

    public IQueryable<T> Query() => _entities;

    public void Add(T entity) => _entities.Add(entity);

    public void AddRange(IEnumerable<T> entities) => _entities.AddRange(entities);

    public void RemoveRange(IEnumerable<T> entities) => _entities.RemoveRange(entities);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
