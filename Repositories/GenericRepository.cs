using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using elasticsearch_netcore.Constants;
using elasticsearch_netcore.Models;
using Microsoft.EntityFrameworkCore;

namespace elasticsearch_netcore.Repositories
{
    /// <summary>
    /// Generic repository base class implementing common CRUD operations.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ElasticsearchDBContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ElasticsearchDBContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> GetAll(bool asNoTracking = true)
        {
            var query = _dbSet.AsQueryable();
            return asNoTracking ? query.AsNoTracking() : query;
        }

        public async Task<T> GetByIdAsync(long id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T> GetSingleAsync(Expression<Func<T, bool>> filter, bool asNoTracking = true)
        {
            var query = _dbSet.AsQueryable();
            if (asNoTracking)
                query = query.AsNoTracking();

            if (filter != null)
                query = query.Where(filter);

            return await query.SingleOrDefaultAsync();
        }

        public async Task<List<T>> FindAsync(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            int skip = 0,
            int take = AppConstants.DefaultPageSize,
            bool asNoTracking = true)
        {
            return await FindWithIncludesAsync(filter, orderBy, null, skip, take, asNoTracking);
        }

        public async Task<List<T>> FindWithIncludesAsync(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            List<Expression<Func<T, object>>> includes = null,
            int skip = 0,
            int take = AppConstants.DefaultPageSize,
            bool asNoTracking = true)
        {
            if (skip < 0) skip = 0;
            if (take > AppConstants.MaxPageSize || take <= 0) take = AppConstants.DefaultPageSize;

            IQueryable<T> query = _dbSet.AsQueryable();

            if (asNoTracking)
                query = query.AsNoTracking();

            if (filter != null)
                query = query.Where(filter);

            if (includes != null)
            {
                foreach (var include in includes)
                    query = query.Include(include);
            }

            if (orderBy != null)
                query = orderBy(query);

            if (skip > 0)
                query = query.Skip(skip);

            query = query.Take(take);

            return await query.ToListAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> filter = null)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();

            if (filter != null)
                query = query.Where(filter);

            return await query.CountAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            var result = await _dbSet.AddAsync(entity);
            return result.Entity;
        }

        public async Task UpdateAsync(T entity)
        {
            await Task.Run(() =>
            {
                var entry = _context.Entry(entity);
                entry.State = EntityState.Modified;
            });
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteAsync(T entity)
        {
            await Task.Run(() => _dbSet.Remove(entity));
            return true;
        }
    }
}
