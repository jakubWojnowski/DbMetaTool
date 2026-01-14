using System.CommandLine;
using DbMetaTool.Databases;
using DbMetaTool.Features.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace DbMetaTool.Features.Commands.ExportMetadata;

public static class ExportMetadataCommandEntry
{
    public static RootCommand MapExportMetadata(this RootCommand rootCommand, IServiceProvider serviceProvider)
    {
        var command = new Command("export-scripts", "Eksportuje metadane z bazy danych do skryptów SQL");

        var databaseTypeOption = new Option<DatabaseType>("--database-type")
        {
            Description = "Typ bazy danych (Firebird, SqlServer, PostgreSQL, etc.)",
            Required = true
        };

        var connectionStringOption = new Option<string>("--connection-string")
        {
            Description = "Connection string do bazy danych",
            Required = true
        };

        var outputDirOption = new Option<string>("--output-dir")
        {
            Description = "Katalog wyjściowy dla wyeksportowanych skryptów",
            Required = true
        };

        command.Options.Add(databaseTypeOption);
        command.Options.Add(connectionStringOption);
        command.Options.Add(outputDirOption);

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var databaseType = parseResult.GetValue(databaseTypeOption);
            var connectionString = parseResult.GetValue(connectionStringOption);
            var outputDir = parseResult.GetValue(outputDirOption);
            var handler = serviceProvider.GetRequiredService<IAsyncHandler<ExportMetadataCommand, ExportMetadataResponse>>();
            
            await ExportMetadataAsync(databaseType, connectionString!, outputDir!, handler, cancellationToken);
        });

        rootCommand.Subcommands.Add(command);

        return rootCommand;
    }

    private static async Task ExportMetadataAsync(
        DatabaseType databaseType,
        string connectionString,
        string outputDir,
        IAsyncHandler<ExportMetadataCommand, ExportMetadataResponse> handler,
        CancellationToken cancellationToken = default)
    {
        var command = new ExportMetadataCommand(
            DatabaseType: databaseType,
            ConnectionString: connectionString,
            OutputDirectory: outputDir);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            Environment.Exit(1);
        }
    }
}
