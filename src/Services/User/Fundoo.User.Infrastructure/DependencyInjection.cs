using Fundoo.User.Application.Interface;
using Fundoo.User.Infrastructure.Http;
using Fundoo.User.Infrastructure.Persistence;
using Fundoo.User.Infrastructure.Persistence.Repositories;
using Fundoo.User.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;
using Polly;
using Microsoft.Extensions.Http.Resilience;

namespace Fundoo.User.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddUserInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("UserDb")
                ?? throw new InvalidOperationException("UserDb connection string is not configured.");

            services.AddDbContext<UserDbContext>(options =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName);
                    });
            });


            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

            // Messaging client with resilience
            string messagingBaseUrl = configuration["MessagingService:BaseUrl"]
                ?? throw new InvalidOperationException("MessagingService:BaseUrl is not configured.");

            services.AddHttpClient<IMessagingClient, HttpMessagingClient>(client =>
            {
                client.BaseAddress = new Uri(messagingBaseUrl);
            })
            // It is a predefined pipeline, everything is included here
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout =TimeSpan.FromSeconds(10);  // Timeout

                options.TotalRequestTimeout.Timeout =TimeSpan.FromSeconds(30); // Total Request Timeout
                // Entire resilience operation including retries has a maximum duration of 30 seconds.

                options.RateLimiter.DefaultRateLimiterOptions.PermitLimit = 4;
                options.RateLimiter.DefaultRateLimiterOptions.QueueLimit = 0;  // Rate Limiter
                // This configuration uses a concurrency limiter internally.


                options.Retry.MaxRetryAttempts = 3; // Retry Strategy
                options.Retry.BackoffType = DelayBackoffType.Exponential; // here polly is used, wait exponentially -> 2,4,8
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                options.Retry.UseJitter = true; // Retry times are randomized, spreading requests over time. 10.0.1,10.0.2

                // Circuit Breaker- protects application when Repeated downstream failures
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            });


            // custom pipeline, add manually
            //.AddResilienceHandler("MessagingPipeline", pipelineBuilder =>
            //{
            //    // 1. Rate Limiter
            //    pipelineBuilder.AddRateLimiter(
            //        new SlidingWindowRateLimiter(
            //            new SlidingWindowRateLimiterOptions
            //            {
            //                PermitLimit = 3,
            //                Window = TimeSpan.FromMinutes(1),
            //                SegmentsPerWindow = 3,
            //                QueueLimit = 0,
            //                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            //                AutoReplenishment = true
            //            }));
            //    // 2. Retry

            //    // 3. Timeout

            //    // 4. Circuit Breaker

            //    // 5. Concurrency Limiter

            //    // 6. Bulkhead
            //});

            return services;
        }
    }
}
