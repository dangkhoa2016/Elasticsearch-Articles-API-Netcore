using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.Middleware;
using Serilog;
using elasticsearch_netcore.Extensions;
using elasticsearch_netcore.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using AspNetCoreRateLimit;

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
            // Rate Limiting Configuration
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
            services.AddInMemoryRateLimiting();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            services.AddCors(option => option.AddPolicy("APIPolicy", builder =>
            {
                var allowedOrigins = Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                
                if (allowedOrigins.Length == 0)
                {
                    throw new InvalidOperationException("AllowedOrigins must be configured in appsettings.json");
                }

                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }));

            services
                //.AddEntityFrameworkSqlite()
                .AddDbContext<Models.ElasticsearchDBContext>(item => item.UseSqlite(Configuration.GetConnectionString("DBConnectionString")));

            services.AddElasticsearch(Configuration);
            services.AddSingleton<Helpers.Helper>();

            services.AddSingleton<Services.TokenService>();

            var jwtSecret = Configuration["JwtSettings:Secret"];
            var jwtIssuer = Configuration["JwtSettings:Issuer"];
            var jwtAudience = Configuration["JwtSettings:Audience"];

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                };
            });

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

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<Startup>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();

            // Rate Limiting Middleware
            app.UseIpRateLimiting();

            //Handle 404 errors
            app.Use(async (ctx, next) =>
            {
                await next();
                if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
                {
                    ctx.Request.Path = "/404";
                    await next();
                }
            });

            app.UseSerilogRequestLogging();

            // app.UseHttpsRedirection();

            app.UseCors("APIPolicy");

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<ValidationKeyTransformMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}