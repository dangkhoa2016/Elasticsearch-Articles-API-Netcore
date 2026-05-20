using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.Middleware;
using Serilog;
using elasticsearch_netcore.Extensions;
using elasticsearch_netcore.Helpers;
using elasticsearch_netcore.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using AspNetCoreRateLimit;
using elasticsearch_netcore.Constants;
using elasticsearch_netcore.Mappings;
using elasticsearch_netcore.HealthChecks;
using elasticsearch_netcore.Factories;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

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

            // Response Compression
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
                options.Providers.Add<BrotliCompressionProvider>();
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Optimal;
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Optimal;
            });

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
                .AddDbContext<Models.ElasticsearchDBContext>(options =>
                    DatabaseProviderFactory.ConfigureProvider(options, Configuration));

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
                    queueCapacity = AppConstants.DefaultQueueCapacity;

                return new BackgroundWorkerQueue(queueCapacity);
            });

            services.AddScoped<IArticleRepository, ArticleRepository>();
            services.AddScoped<IAuthorRepository, AuthorRepository>();
            services.AddScoped<IAuthorshipRepository, AuthorshipRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IArticleService, ArticleService>();
            services.AddScoped<IAuthorService, AuthorService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICommentService, CommentService>();

            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<Startup>();

            // HSTS Configuration
            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
                options.IncludeSubDomains = true;
                options.Preload = true;
            });

            services.AddTransient<HealthChecks.IElasticsearchHealthClient, HealthChecks.NESTElasticsearchHealthClient>();

            services.AddHealthChecks()
                .AddCheck<DatabaseHealthCheck>("Database")
                .AddCheck<ElasticsearchHealthCheck>("Elasticsearch");

            // OpenTelemetry Configuration
            var serviceName = Configuration["OpenTelemetry:ServiceName"] ?? "ElasticsearchArticlesApi";
            var serviceVersion = Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
            var tracingEnabled = Configuration.GetValue("OpenTelemetry:Tracing:Enabled", true);
            var metricsEnabled = Configuration.GetValue("OpenTelemetry:Metrics:Enabled", true);
            var sampleRate = Configuration.GetValue("OpenTelemetry:Tracing:SampleRate", 1.0);
            var otlpEndpoint = Configuration["OpenTelemetry:Tracing:OtlpEndpoint"] ?? "http://localhost:4317";

            if (tracingEnabled)
            {
                services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                    .WithTracing(tracing =>
                    {
                        tracing
                            .SetSampler(new OpenTelemetry.Trace.ParentBasedSampler(
                                new OpenTelemetry.Trace.TraceIdRatioBasedSampler(sampleRate)))
                            .AddAspNetCoreInstrumentation(options =>
                            {
                                options.RecordException = true;
                                options.EnrichWithHttpRequest = (activity, request) =>
                                {
                                    activity.SetTag("http.route", request.Path);
                                };
                            })
                            .AddHttpClientInstrumentation()
                            .AddEntityFrameworkCoreInstrumentation(options =>
                            {
                                options.SetDbStatementForText = true;
                            })
                            .AddSource("ElasticsearchArticlesApi")
                            .AddConsoleExporter();

                        // Add OTLP exporter if endpoint is configured
                        if (!string.IsNullOrEmpty(otlpEndpoint))
                        {
                            tracing.AddOtlpExporter(otlpOptions =>
                            {
                                otlpOptions.Endpoint = new Uri(otlpEndpoint);
                            });
                        }
                    });
            }

            if (metricsEnabled)
            {
                services.AddOpenTelemetry()
                    .WithMetrics(metrics =>
                    {
                        metrics
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddMeter("ElasticsearchArticlesApi")
                            .AddConsoleExporter();

                        // Add OTLP exporter for metrics
                        if (!string.IsNullOrEmpty(otlpEndpoint))
                        {
                            metrics.AddOtlpExporter(otlpOptions =>
                            {
                                otlpOptions.Endpoint = new Uri(otlpEndpoint);
                            });
                        }
                    });
            }

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Security Headers Middleware
            app.UseMiddleware<Middleware.SecurityHeadersMiddleware>();

            // Correlation ID Middleware (must be early to propagate to all subsequent middleware)
            app.UseMiddleware<Middleware.CorrelationIdMiddleware>();

            // Telemetry Enrichment Middleware
            app.UseMiddleware<Telemetry.TelemetryEnrichmentMiddleware>();

            app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();

            // Rate Limiting Middleware
            app.UseIpRateLimiting();

            // Handle 404 errors
            app.Use(async (ctx, next) =>
            {
                await next();
                if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted)
                {
                    ctx.Request.Path = "/404";
                    await next();
                }
            });

            // Response Compression Middleware
            app.UseResponseCompression();

            // Request/Response Logging (after compression to capture actual response)
            app.UseMiddleware<Middleware.RequestResponseLoggingMiddleware>();

            app.UseSerilogRequestLogging();

            // HTTPS Redirection for Production
            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }

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