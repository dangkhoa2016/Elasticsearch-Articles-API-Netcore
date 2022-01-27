using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.Models;
using Serilog;
using elasticsearch_netcore.Extensions;
using elasticsearch_netcore.Helpers;

namespace elasticsearch_netcore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(option => option.AddPolicy("APIPolicy", builder =>
            {
                builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
            }));

            services
                //.AddEntityFrameworkSqlite()
                .AddDbContext<Models.ElasticsearchDBContext>(item => item.UseSqlite(Configuration.GetConnectionString("DBConnectionString")));

            services.AddElasticsearch(Configuration);
            services.AddSingleton<Helpers.Helper>();

            services.AddHostedService<LongRunningService>();
            services.AddSingleton<IBackgroundWorkerQueue>(sp =>
            {
                if (!int.TryParse(Configuration["QueueCapacity"], out var queueCapacity))
                    queueCapacity = 100;

                return new BackgroundWorkerQueue(queueCapacity);
            });

            services.AddScoped<IArticleRepository, ArticleRepository>();
            services.AddScoped<IAuthorRepository, AuthorRepository>();
            services.AddScoped<IAuthorshipRepository, AuthorshipRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // if (env.IsDevelopment())
            // {
            //     app.UseDeveloperExceptionPage();
            // }

            //Handle 500 errors
            app.UseExceptionHandler("/500");
            //Handle 404 errors
            app.Use(async (ctx, next) =>
            {
                await next();
                if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
                {
                    //Re-execute the request so the user gets the error page
                    ctx.Request.Path = "/404";
                    await next();
                }
            });

            app.UseSerilogRequestLogging();

            // app.UseHttpsRedirection();

            app.UseCors("APIPolicy");

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}