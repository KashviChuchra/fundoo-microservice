using Fundoo.Messaging.Application.Interfaces;
using Fundoo.Messaging.Application.Services;
using Fundoo.Messaging.Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fundoo.Messaging.Application;

/// <summary>
/// Extension methods to register Application-layer services with the DI container.
/// </summary>
public static class MessagingApplicationExtensions
{
    
    public static IServiceCollection AddMessagingApplication(this IServiceCollection services,IConfiguration configuration)
    {
        // Bind EmailSettings from the "EmailSettings" section
        services.Configure<EmailSettings>(options =>
            configuration.GetSection("EmailSettings").Bind(options));

        // Register the email service
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
