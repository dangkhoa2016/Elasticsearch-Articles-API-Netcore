using System;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Repositories
{
    /// <summary>
    /// Unit of Work interface for managing transactions and repository lifecycle.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Get repository for the specified entity type.
        /// </summary>
        IGenericRepository<T> Repository<T>() where T : class;

        /// <summary>
        /// Save all changes made in this unit of work.
        /// </summary>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Begin a new transaction.
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commit the current transaction.
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rollback the current transaction.
        /// </summary>
        Task RollbackTransactionAsync();
    }
}
