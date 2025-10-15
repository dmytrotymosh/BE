using System.Linq.Expressions;
using DB.Models;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore.Query;

namespace DB.Repository;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<Result<IEnumerable<TResult>>> GetListAsync<TResult>(
        Expression<Func<T, bool>> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        List<Func<IQueryable<T>, IIncludableQueryable<T, object>>> includes = null,
        Expression<Func<T, TResult>> selector = null,
        int? skip = null,
        int? take = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    Task<Result<TResult>> GetSingleAsync<TResult>(
        Expression<Func<T, bool>> filter = null,
        List<Func<IQueryable<T>, IIncludableQueryable<T, object>>> includes = null,
        Expression<Func<T, TResult>> selector = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default);

    Task<Result> AddAsync(T entity, CancellationToken cancellationToken = default);

    Task<Result> AddRangeAsync(IEnumerable<T> entities,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    
    Task<Result<int>> UpdateRangeAsync(Expression<Func<T, bool>> filter,
        Action<T> updateAction,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    Task<Result<int>> UpdateRangeAsync(Expression<Func<T, bool>> filter,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> updateExpression,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveAsync(T entity, CancellationToken cancellationToken = default);

    Task<Result> RemoveRangeAsync(IEnumerable<T> entities,
        int batchSize = 100,
        CancellationToken cancellationToken = default);
    
    Task<Result<int>> RemoveRangeAsync(Expression<Func<T, bool>> filter,
        CancellationToken cancellationToken = default);
    
    Task<Result<bool>> ExistsAsync(Expression<Func<T, bool>> filter,
        CancellationToken cancellationToken = default);
    
    Task<Result<int>> CountAsync(Expression<Func<T, bool>> filter = null,
        CancellationToken cancellationToken = default);

    Task<Result<decimal>> SumAsync(Expression<Func<T, bool>> filter,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default);

    Task<Result<decimal>> AverageAsync(Expression<Func<T, bool>> filter,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default);

    Task<Result<decimal>> MaxAsync(Expression<Func<T, bool>> filter,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default);

    Task<Result<decimal>> MinAsync(Expression<Func<T, bool>> filter,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken = default);
    
    Task<Result<int>> ExecuteTransactionAsync(Func<Task> operation,
        CancellationToken cancellationToken = default);
}