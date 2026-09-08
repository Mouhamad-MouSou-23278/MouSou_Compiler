using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Infrastructure.AI;
using OnlineCompiler.Infrastructure.Docker;
using OnlineCompiler.Infrastructure.Email;
using OnlineCompiler.Infrastructure.Persistence;
using OnlineCompiler.Infrastructure.Queue;
using OnlineCompiler.Infrastructure.Security;

namespace OnlineCompiler.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<OnlineCompilerDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("YOUR_POSTGRES"))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                // Fallback for immediate test runs without PostgreSQL container up
                options.UseInMemoryDatabase("OnlineCompilerInMemoryDb");
            }
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<OnlineCompilerDbContext>());

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<IExecutionQueue, RedisExecutionQueue>();
        services.AddSingleton<IDockerSandboxService, DockerSandboxService>();

        services.AddHttpClient<IAIService, OpenRouterAIService>();

        return services;
    }
}
