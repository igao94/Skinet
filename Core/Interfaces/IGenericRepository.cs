using Core.Entities;

namespace Core.Interfaces;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> ListAllAsync();
    Task<T?> GetEntityWithSpecAsync(ISpecification<T> spec);
    Task<IReadOnlyList<T>> ListAllWithSpecAsync(ISpecification<T> spec);    
    Task<TResult?> GetEntityWithSpecAsync<TResult>(ISpecification<T, TResult> spec);
    Task<IReadOnlyList<TResult>> ListAllWithSpecAsync<TResult>(ISpecification<T, TResult> spec);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<bool> SaveAllAsync();
    bool Exists(int id);
}
