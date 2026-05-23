using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Factories
{
    /// <summary>
    /// EF Core interceptor that sets SQLite PRAGMA options on each connection
    /// to enable WAL mode and improve concurrent write handling.
    /// </summary>
    public class SqlitePragmaInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            SetPragmaIfSqlite(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            SetPragmaIfSqlite(command);
            return ValueTask.FromResult(result);
        }

        private static void SetPragmaIfSqlite(DbCommand command)
        {
            if (command.Connection is SqliteConnection conn && conn.State == System.Data.ConnectionState.Open)
            {
                if (conn.ConnectionString.Contains("pragma_journal_mode"))
                    return; // already set via connection string

                using var pragmaCmd = conn.CreateCommand();
                pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                pragmaCmd.ExecuteNonQuery();
            }
        }
    }
}
