using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class GenericRepository<T>(StoreContext context) : IGenericRepository<T> where T : BaseEntity
{
    public void Add(T entity) => context.Set<T>().Add(entity);

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        var query = context.Set<T>().AsQueryable();

        query = spec.ApplyCriteria(query);

        return await query.CountAsync();
    }

    public bool Exists(int id) => context.Set<T>().Any(x => x.Id == id);

    public async Task<T?> GetByIdAsync(int id) => await context.Set<T>().FindAsync(id);

    public async Task<T?> GetEntityWithSpecAsync(ISpecification<T> spec)
    {
        return await ApplySpecifications(spec).FirstOrDefaultAsync();
    }

    public async Task<TResult?> GetEntityWithSpecAsync<TResult>(ISpecification<T, TResult> spec)
    {
        return await ApplySpecifications(spec).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<T>> ListAllAsync() => await context.Set<T>().ToListAsync();

    public async Task<IReadOnlyList<T>> ListAllWithSpecAsync(ISpecification<T> spec)
    {
        return await ApplySpecifications(spec).ToListAsync();
    }

    public async Task<IReadOnlyList<TResult>> ListAllWithSpecAsync<TResult>(ISpecification<T, TResult> spec)
    {
        return await ApplySpecifications(spec).ToListAsync();
    }

    public void Remove(T entity) => context.Set<T>().Remove(entity);

    public void Update(T entity)
    {
        context.Set<T>().Attach(entity);

        context.Entry(entity).State = EntityState.Modified;
    }

    private IQueryable<T> ApplySpecifications(ISpecification<T> spec)
    {
        return SpecificationEvaluator<T>.GetQuery(context.Set<T>().AsQueryable(), spec);
    }

    private IQueryable<TResult> ApplySpecifications<TResult>(ISpecification<T, TResult> spec)
    {
        return SpecificationEvaluator<T>.GetQuery<T, TResult>(context.Set<T>().AsQueryable(), spec);
    }
}
