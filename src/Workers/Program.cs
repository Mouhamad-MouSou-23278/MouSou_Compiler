using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OnlineCompiler.Application;
using OnlineCompiler.Infrastructure;
using OnlineCompiler.Workers;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, lc) => lc
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console())
    .ConfigureServices((hostContext, services) =>
    {
        services.AddApplication();
        services.AddInfrastructure(hostContext.Configuration);
        services.AddHostedService<ExecutionWorker>();
    })
    .Build();

await host.RunAsync();
