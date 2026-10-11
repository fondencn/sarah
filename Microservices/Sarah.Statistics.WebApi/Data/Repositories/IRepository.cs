namespace Sarah.Statistics.WebApi.Data.Repositories;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void RemoveRange(IEnumerable<T> entities);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
