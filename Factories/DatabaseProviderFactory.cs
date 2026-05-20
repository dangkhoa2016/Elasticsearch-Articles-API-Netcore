using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace elasticsearch_netcore.Factories
{
    /// <summary>
    /// Factory to configure the appropriate database provider based on configuration.
    /// Supports SQLite (development), PostgreSQL, and SQL Server (production).
    /// </summary>
    public static class DatabaseProviderFactory
    {
        /// <summary>
        /// Configures the DbContext options with the appropriate database provider.
        /// </summary>
        /// <param name="optionsBuilder">The DbContextOptionsBuilder to configure.</param>
        /// <param name="configuration">The application configuration.</param>
        public static void ConfigureProvider(DbContextOptionsBuilder optionsBuilder, IConfiguration configuration)
        {
            var provider = configuration["DatabaseProvider:Provider"] ?? "SQLite";
            var connectionString = configuration.GetConnectionString("DBConnectionString");
            var commandTimeout = configuration.GetValue("DatabaseProvider:CommandTimeout", 30);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("DBConnectionString must be configured in appsettings.json or environment variables.");
            }

            switch (provider.ToLowerInvariant())
            {
                case "sqlite":
                    optionsBuilder.UseSqlite(connectionString, sqlite =>
                    {
                        sqlite.CommandTimeout(commandTimeout);
                        sqlite.MigrationsAssembly("ElasticsearchArticlesApiNetcore");
                    });
                    break;

                case "postgresql":
                case "npgsql":
                    optionsBuilder.UseNpgsql(connectionString, npgsql =>
                    {
                        npgsql.CommandTimeout(commandTimeout);
                        npgsql.MigrationsAssembly("ElasticsearchArticlesApiNetcore");
                        npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
                    });
                    break;

                case "sqlserver":
                case "mssql":
                    optionsBuilder.UseSqlServer(connectionString, sql =>
                    {
                        sql.CommandTimeout(commandTimeout);
                        sql.MigrationsAssembly("ElasticsearchArticlesApiNetcore");
                        sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
                    });
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported database provider: {provider}. Supported providers: SQLite, PostgreSQL, SQLServer.");
            }
        }
    }
}
