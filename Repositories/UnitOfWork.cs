using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using elasticsearch_netcore.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace elasticsearch_netcore.Repositories
{
    /// <summary>
    /// Unit of Work implementation managing transactions and repository instances.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ElasticsearchDBContext _context;
        private readonly ConcurrentDictionary<Type, object> _repositories = new();
        private IDbContextTransaction _currentTransaction;

        public UnitOfWork(ElasticsearchDBContext context)
        {
            _context = context;
        }

        public IGenericRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T);
            return (IGenericRepository<T>)_repositories.GetOrAdd(type, _ => new GenericRepository<T>(_context));
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
                return;

            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_currentTransaction == null)
                return;

            try
            {
                await _currentTransaction.CommitAsync();
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction == null)
                return;

            try
            {
                await _currentTransaction.RollbackAsync();
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
