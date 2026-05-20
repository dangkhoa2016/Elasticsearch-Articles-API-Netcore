using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace elasticsearch_netcore.Resilience
{
    /// <summary>
    /// Provides resilience policies for database operations: retry, circuit breaker, and timeout.
    /// </summary>
    public static class DatabaseResilience
    {
        /// <summary>
        /// Creates a combined resilience pipeline with retry, circuit breaker, and timeout.
        /// </summary>
        public static ResiliencePipeline GetPipeline(IConfiguration configuration)
        {
            var retryConfig = configuration.GetSection("Resilience:Retry");
            var circuitBreakerConfig = configuration.GetSection("Resilience:CircuitBreaker");
            var timeoutConfig = configuration.GetSection("Resilience:Timeout");

            var maxRetries = retryConfig.GetValue("MaxRetries", 3);
            var retryDelayMs = retryConfig.GetValue("DelayMs", 1000);

            var failureThreshold = circuitBreakerConfig.GetValue("FailureThreshold", 0.5);
            var minimumThroughput = circuitBreakerConfig.GetValue("MinimumThroughput", 10);
            var durationOfBreakSeconds = circuitBreakerConfig.GetValue("DurationOfBreakSeconds", 30);

            var timeoutSeconds = timeoutConfig.GetValue("Seconds", 30);

            var builder = new ResiliencePipelineBuilder();

            // Retry policy with exponential backoff
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromMilliseconds(retryDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    Serilog.Log.Warning(
                        "Database retry attempt {Attempt} for operation {Operation} after {Delay}ms due to: {Exception}",
                        args.AttemptNumber,
                        args.Context.OperationKey ?? "Unknown",
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return default;
                }
            });

            // Circuit breaker policy
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = failureThreshold,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = minimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(durationOfBreakSeconds),
                OnOpened = args =>
                {
                    Serilog.Log.Error(
                        "Database circuit breaker opened after {FailureRatio}% failure rate. Will reopen after {BreakDuration}s",
                        failureThreshold * 100,
                        durationOfBreakSeconds);
                    return default;
                },
                OnClosed = args =>
                {
                    Serilog.Log.Information("Database circuit breaker closed - service recovered");
                    return default;
                }
            });

            // Timeout policy
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds),
                OnTimeout = args =>
                {
                    Serilog.Log.Warning(
                        "Database operation timed out after {Timeout}s. Operation: {Operation}",
                        timeoutSeconds,
                        args.Context.OperationKey ?? "Unknown");
                    return default;
                }
            });

            return builder.Build();
        }

        /// <summary>
        /// Executes a database operation with resilience policies applied.
        /// </summary>
        public static async Task<T> ExecuteAsync<T>(ResiliencePipeline pipeline, Func<CancellationToken, Task<T>> action, string operationKey = null)
        {
            return await pipeline.ExecuteAsync(async ct => await action(ct));
        }

        /// <summary>
        /// Executes a database operation with resilience policies applied (void return).
        /// </summary>
        public static async Task ExecuteAsync(ResiliencePipeline pipeline, Func<CancellationToken, Task> action, string operationKey = null)
        {
            await pipeline.ExecuteAsync(async ct => await action(ct));
        }
    }
}
