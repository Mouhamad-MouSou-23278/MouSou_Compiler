using Microsoft.Extensions.DependencyInjection;
using OnlineCompiler.Application.Interfaces;
using OnlineCompiler.Application.Services;

namespace OnlineCompiler.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IExecutionService, ExecutionService>();
        services.AddScoped<IAIFacadeService, AIFacadeService>();

        return services;
    }
}
