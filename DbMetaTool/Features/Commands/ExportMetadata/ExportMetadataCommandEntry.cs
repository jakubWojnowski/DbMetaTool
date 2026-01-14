using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace DbMetaTool.Features.Commands.ExportMetadata;

public static class ExportMetadataCommandEntry
{
    public static RootCommand MapExportMetadata(this RootCommand rootCommand, IServiceProvider serviceProvider)
    {
        var command = new Command("export-scripts", "Eksportuje metadane z bazy Firebird do skryptów SQL");

        var connectionStringOption = new Option<string>("--connection-string")
        {
            Description = "Connection string do bazy danych Firebird",
            Required = true
        };

        var outputDirOption = new Option<string>("--output-dir")
        {
            Description = "Katalog wyjściowy dla wyeksportowanych skryptów",
            Required = true
        };

        command.Options.Add(connectionStringOption);
        command.Options.Add(outputDirOption);

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var connectionString = parseResult.GetValue(connectionStringOption);
            var outputDir = parseResult.GetValue(outputDirOption);
            
            await ExportMetadataAsync(connectionString!, outputDir!, serviceProvider, cancellationToken);
        });

        rootCommand.Subcommands.Add(command);

        return rootCommand;
    }

    private static async Task ExportMetadataAsync(
        string connectionString,
        string outputDir,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var handler = serviceProvider.GetRequiredService<IAsyncHandler<ExportMetadataCommand, ExportMetadataResponse>>();
        
        var command = new ExportMetadataCommand(
            ConnectionString: connectionString,
            OutputDirectory: outputDir);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            Environment.Exit(1);
        }
    }
}
