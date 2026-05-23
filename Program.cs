
using elasticsearch_netcore.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Threading.Tasks;

namespace elasticsearch_netcore
{
    public partial class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
               .AddEnvironmentVariables()
               .Build();

        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.WithProperty("Application", "ElasticsearchArticlesApi")
                .Enrich.WithProperty("Environment", Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production")
                .CreateLogger();

            try
            {
                Log.Information("Starting web host");

                var provider = Configuration["DatabaseProvider:Provider"] ?? "SQLite";
                if (provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
                {
                    var connStr = Configuration.GetConnectionString("DBConnectionString");
                    if (!string.IsNullOrEmpty(connStr))
                    {
                        var dbPath = connStr.Replace("Data Source=", "").Split(';')[0];
                        if (!string.IsNullOrEmpty(dbPath) && dbPath != ":memory:")
                        {
                            var dirPath = Path.GetDirectoryName(dbPath);
                            if (!string.IsNullOrEmpty(dirPath) && Directory.Exists(dirPath))
                            {
                                var testFile = Path.Combine(dirPath, ".write_test");
                                try
                                {
                                    File.WriteAllText(testFile, "");
                                    File.Delete(testFile);
                                }
                                catch (Exception ex)
                                {
                                    Log.Warning(ex, "Database directory may not be writable. SQLite write operations will fail. Path: {DirPath}", dirPath);
                                }
                            }
                        }
                    }
                }

                var host = CreateHostBuilder(args).Build();

                var helper = (Helpers.Helper)host.Services.GetService(typeof(Helpers.Helper));
                await helper.InitIndex();

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        static async Task RunBulkIndex(IHost host)
        {
            var serviceScopeFactory = (IServiceScopeFactory)host.Services.GetService(typeof(IServiceScopeFactory));

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var services = scope.ServiceProvider;
                var articleRepository = services.GetRequiredService<IArticleRepository>();
                await articleRepository.BulkIndex();
            }
        }

    }
}