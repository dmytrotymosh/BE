using System.Linq.Expressions;
using DB.Models;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

namespace DB.Repository;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;
    private readonly ILogger _logger;
    private const int DefaultBatchSize = 100;

    public GenericRepository(DbContext context, ILogger<GenericRepository<T>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<T>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Get

    public virtual async Task<Result<IEnumerable<TResult>>> GetListAsync<TResult>(
        Expression<Func<T, bool>> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        List<Func<IQueryable<T>, IIncludableQueryable<T, object>>> includes = null,
        Expression<Func<T, TResult>> selector = null,
        int? skip = null,
        int? take = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<T> query = ApplyConditions(_dbSet, filter, orderBy, includes, skip, take, asNoTracking);

            if (selector != null)
            {
                return Result<IEnumerable<TResult>>.Ok(await query.Select(selector).ToListAsync(cancellationToken));
            }

            return Result<IEnumerable<TResult>>.Ok(await query.Cast<TResult>().ToListAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetListAsync");
                
            return Result<IEnumerable<TResult>>.Fail("An error occurred while retrieving the data.");
        }
    }

    public virtual async Task<Result<TResult>> GetSingleAsync<TResult>(
        Expression<Func<T, bool>> filter = null,
        List<Func<IQueryable<T>, IIncludableQueryable<T, object>>> includes = null,
        Expression<Func<T, TResult>> selector = null,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<T> query = ApplyConditions(_dbSet, filter, null, includes, null, null, asNoTracking);

            if (selector != null)
            {
                return Result<TResult>.Ok(await query.Select(selector).FirstOrDefaultAsync(cancellationToken));
            }

            return Result<TResult>.Ok(await query.Cast<TResult>().FirstOrDefaultAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSingleAsync");
                
            return Result<TResult>.Fail("An error occurred while retrieving the data.");
        }
    }

    #endregion

    #region Add

    public virtual async Task<Result> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await _dbSet.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddAsync");
                
            return Result.Fail("An error occurred while adding the data.");
        }
    }

    public virtual async Task<Result> AddRangeAsync(IEnumerable<T> entities,
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (entities == null || !entities.Any())
            {
                throw new ArgumentNullException(nameof(entities));
            }

            foreach (var batch in entities.Batch(batchSize))
            {
                await _dbSet.AddRangeAsync(batch, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddRangeAsync");
                
            return Result.Fail("An error occurred while adding the data.");
        }
    }

    #endregion

    #region Update
        
    public virtual async Task<Result> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateAsync");
                
            return Result.Fail("An error occurred while updating the data.");
        }
    }
        
    public virtual async Task<Result<int>> UpdateRangeAsync(Expression<Func<T, bool>> filter,
        Action<T> updateAction,
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var entities = await query.ToListAsync(cancellationToken);

            int totalUpdated = 0;

            foreach (var batch in entities.Batch(batchSize))
            {
                foreach (var entity in batch)
                {
                    updateAction(entity);
                }
                    
                _context.UpdateRange(batch);
                totalUpdated += await _context.SaveChangesAsync(cancellationToken);
            }

            return Result<int>.Ok(totalUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateRangeAsync");
                
            return Result<int>.Fail("An error occurred while updating the data.");
        }
    }
        
    public virtual async Task<Result<int>> UpdateRangeAsync(Expression<Func<T, bool>> filter,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> updateExpression,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbSet.Where(filter);
            var updatedCount = await query.ExecuteUpdateAsync(updateExpression, cancellationToken);

            return Result<int>.Ok(updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateRangeAsync");
            return Result<int>.Fail("An error occurred while updating the data.");
        }
    }
        
    #endregion

    #region Remove

    public virtual async Task<Result> RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RemoveAsync");
                
            return Result.Fail("An error occurred while removing the data.");
        }
    }
        
    public virtual async Task<Result> RemoveRangeAsync(IEnumerable<T> entities,
        int batchSize = DefaultBatchSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (entities == null || !entities.Any())
            {
                throw new ArgumentNullException(nameof(entities));
            }

            foreach (var batch in entities.Batch(batchSize))
            {
                _dbSet.RemoveRange(batch);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RemoveRangeAsync");
                
            return Result.Fail("An error occurred while removing the data.");
        }
    }
        
    public virtual async Task<Result<int>> RemoveRangeAsync(Expression<Func<T, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbSet.Where(filter);
            var deletedCount = await query.ExecuteDeleteAsync(cancellationToken);

            return Result<int>.Ok(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RemoveRangeAsync");
            return Result<int>.Fail("An error occurred while removing the data.");
        }
    }

    #endregion

    #region Aggregations

    public virtual async Task<Result<bool>> ExistsAsync(Expression<Func<T, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return Result<bool>.Ok(await _dbSet.AnyAsync(filter, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExistsAsync");
                
            return Result<bool>.Fail("An error occurred while checking the existence of the data.");
        }
    }
        
    public virtual async Task<Result<int>> CountAsync(Expression<Func<T, bool>> filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return Result<int>.Ok(await query.CountAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CountAsync");
                
            return Result<int>.Fail("An error occurred while counting the data.");
        }
    }

    public virtual async Task<Result<decimal>> SumAsync(Expression<Func<T, bool>> filter,
        Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var sum = await query.SumAsync(selector, cancellationToken);

            return Result<decimal>.Ok(sum);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SumAsync");
                
            return Result<decimal>.Fail("An error occurred while calculating the sum.");
        }
    }

    public virtual async Task<Result<decimal>> AverageAsync(Expression<Func<T, bool>> filter,
        Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var average = await query.AverageAsync(selector, cancellationToken);

            return Result<decimal>.Ok(average);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AverageAsync");
                
            return Result<decimal>.Fail("An error occurred while calculating the average.");
        }
    }

    public virtual async Task<Result<decimal>> MaxAsync(Expression<Func<T, bool>> filter,
        Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var max = await query.MaxAsync(selector, cancellationToken);

            return Result<decimal>.Ok(max);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MaxAsync");
                
            return Result<decimal>.Fail("An error occurred while calculating the maximum.");
        }
    }

    public virtual async Task<Result<decimal>> MinAsync(Expression<Func<T, bool>> filter,
        Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var min = await query.MinAsync(selector, cancellationToken);

            return Result<decimal>.Ok(min);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MinAsync");
                
            return Result<decimal>.Fail("An error occurred while calculating the minimum.");
        }
    }

    #endregion

    #region Transactions

    public virtual async Task<Result<int>> ExecuteTransactionAsync(Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation();
            await transaction.CommitAsync(cancellationToken);

            return Result<int>.Ok(1);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error in ExecuteTransactionAsync");
                
            return Result<int>.Fail("An error occurred while executing the transaction.");
        }
    }

    #endregion

    #region Helpers

    private IQueryable<T> ApplyConditions(IQueryable<T> query, Expression<Func<T, bool>> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        List<Func<IQueryable<T>, IIncludableQueryable<T, object>>> includes = null, int? skip = null, int? take = null,
        bool asNoTracking = false)
    {
        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (includes != null)
        {
            foreach (var include in includes)
            {
                query = include(query);
            }
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        if (skip.HasValue)
        {
            query = query.Skip(skip.Value);
        }

        if (take.HasValue)
        {
            query = query.Take(take.Value);
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query;
    }

    #endregion
}