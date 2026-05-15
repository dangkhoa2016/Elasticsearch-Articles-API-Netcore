using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace elasticsearch_netcore.Repositories
{
    /// <summary>
    /// Generic repository interface providing common CRUD operations.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// Get all entities as an IQueryable.
        /// </summary>
        IQueryable<T> GetAll(bool asNoTracking = true);

        /// <summary>
        /// Get a single entity by its primary key.
        /// </summary>
        Task<T> GetByIdAsync(long id);

        /// <summary>
        /// Get a single entity matching the filter expression.
        /// </summary>
        Task<T> GetSingleAsync(Expression<Func<T, bool>> filter, bool asNoTracking = true);

        /// <summary>
        /// Find entities matching the filter with pagination.
        /// </summary>
        Task<List<T>> FindAsync(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            int skip = 0,
            int take = 10,
            bool asNoTracking = true);

        /// <summary>
        /// Find entities matching the filter with pagination, including related entities.
        /// </summary>
        Task<List<T>> FindWithIncludesAsync(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            List<Expression<Func<T, object>>> includes = null,
            int skip = 0,
            int take = 10,
            bool asNoTracking = true);

        /// <summary>
        /// Count entities matching the filter.
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>> filter = null);

        /// <summary>
        /// Add a new entity.
        /// </summary>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// Update an existing entity.
        /// </summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Delete an entity by its primary key.
        /// </summary>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Delete an entity instance.
        /// </summary>
        Task<bool> DeleteAsync(T entity);
    }
}
