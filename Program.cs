
using elasticsearch_netcore.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Linq;

namespace elasticsearch_netcore
{
    public class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
               .AddEnvironmentVariables()
               .Build();

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

            try
            {
                Log.Information("Starting web host");
                var host = CreateHostBuilder(args).Build();

                // test
                //TestCode(host);

                var helper = (Helpers.Helper)host.Services.GetService(typeof(Helpers.Helper));
                helper.InitIndex().Wait();

                // import
                //RunBulkIndex(host);

                host.Run();
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


        static void TestCode(IHost host)
        {
            var serviceScopeFactory = (IServiceScopeFactory)host.Services.GetService(typeof(IServiceScopeFactory));

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var services = scope.ServiceProvider;
                var dbContext = services.GetRequiredService<elasticsearch_netcore.Models.ElasticsearchDBContext>();

                System.Threading.Thread.Sleep(20000);

                /*
                // create
                var article = new Models.Article();
                article.Title = "test test";
                var lstac = new System.Collections.Generic.List<Models.ArticlesCategory>();
                lstac.Add(new Models.ArticlesCategory() { CategoryId = 1 });
                lstac.Add(new Models.ArticlesCategory() { CategoryId = 2 });
                article.ArticlesCategories = lstac;
                dbContext.Add(article);
                dbContext.SaveChanges();


                // update
                article = dbContext.Articles.AsQueryable()
                            .Include(a => a.Authorships).ThenInclude(a => a.Author)
                            .Include(a => a.ArticlesCategories).ThenInclude(a => a.Category)
                            .Include(a => a.Comments).SingleOrDefault(a => a.Id == 10);

                article.Title = "test test";
                lstac = new System.Collections.Generic.List<Models.ArticlesCategory>();
                lstac.Add(new Models.ArticlesCategory() { CategoryId = 20 });
                lstac.Add(new Models.ArticlesCategory() { CategoryId = 2 });
                article.ArticlesCategories = lstac;
                var lstau = new System.Collections.Generic.List<Models.Authorship>();
                lstau.Add(new Models.Authorship() { AuthorId = 20 });
                lstau.Add(new Models.Authorship() { AuthorId = 19 });
                article.Authorships = lstau;
                dbContext.SaveChanges();

                System.Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(new ViewModels.ArticleViewModel(article).AsIndexedJson(), Newtonsoft.Json.Formatting.Indented));


                var authors = dbContext.Authors.AsQueryable().AsNoTracking()
                    .Where(a => (a.FirstName + " " + a.LastName).Contains("an sc"));
                var count = authors.CountAsync();
                */
            }

        }

        static void RunBulkIndex(IHost host)
        {
            var serviceScopeFactory = (IServiceScopeFactory)host.Services.GetService(typeof(IServiceScopeFactory));

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var services = scope.ServiceProvider;
                var articleRepository = services.GetRequiredService<IArticleRepository>();
                articleRepository.BulkIndex().Wait();
            }
        }

    }
}