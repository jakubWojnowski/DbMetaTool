using System.CommandLine;
using DbMetaTool.Features.Commands;
using DbMetaTool.Services.Build;
using DbMetaTool.Services.Export;
using DbMetaTool.Services.Firebird;
using DbMetaTool.Services.Metadata;
using DbMetaTool.Services.SqlScripts;
using DbMetaTool.Services.Update;
using Microsoft.Extensions.DependencyInjection;

namespace DbMetaTool.Services;

public static class Extensions
{
    public static IServiceCollection AddDbMetaToolServices(this IServiceCollection services)
    {
        services.RegisterServices();
        services.RegisterHandlers();

        return services;
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IDatabaseCreator, FirebirdDatabaseCreator>();
        services.AddSingleton<IScriptLoader, ScriptLoader>();
        services.AddSingleton<IMetadataReader, FirebirdMetadataReader>();

        // Business services
        services.AddScoped<IDatabaseBuildService, DatabaseBuildService>();
        services.AddScoped<IMetadataExportService, MetadataExportService>();

        // Report generators
        services.AddSingleton<IBuildReportGenerator, BuildReportGenerator>();
        services.AddSingleton<IExportReportGenerator, ExportReportGenerator>();
        services.AddSingleton<IUpdateReportGenerator, UpdateReportGenerator>();

        return services;
    }

    private static IServiceCollection RegisterHandlers(this IServiceCollection services)
    {
        var assembly = typeof(Extensions).Assembly;

        var openGenericHandlerInterfaces = new[]
        {
            typeof(IAsyncHandler<,>)
        };

        foreach (var implementationType in assembly.GetTypes())
        {
            if (!implementationType.IsClass || implementationType.IsAbstract)
            {
                continue;
            }

            var serviceInterfaces = implementationType
                .GetInterfaces()
                .Where(i => i.IsGenericType && openGenericHandlerInterfaces.Contains(i.GetGenericTypeDefinition()))
                .ToArray();

            foreach (var serviceType in serviceInterfaces)
            {
                services.AddScoped(serviceType, implementationType);
            }
        }

        return services;
    }

    public static RootCommand RegisterCommands(this RootCommand rootCommand, IServiceProvider serviceProvider)
    {
        rootCommand.MapApplicationCommands(serviceProvider);

        return rootCommand;
    }
}
