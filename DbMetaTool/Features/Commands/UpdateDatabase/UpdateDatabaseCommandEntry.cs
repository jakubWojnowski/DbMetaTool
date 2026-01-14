using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace DbMetaTool.Features.Commands.UpdateDatabase;

public static class UpdateDatabaseCommandEntry
{
    public static RootCommand MapUpdateDatabase(this RootCommand rootCommand, IServiceProvider serviceProvider)
    {
        var command = new Command("update-db", "Aktualizuje istniejącą bazę danych Firebird skryptami SQL");

        var connectionStringOption = new Option<string>("--connection-string")
        {
            Description = "Connection string do bazy danych Firebird",
            Required = true
        };

        var scriptsDirOption = new Option<string>("--scripts-dir")
        {
            Description = "Katalog zawierający skrypty SQL do wykonania",
            Required = true
        };

        command.Options.Add(connectionStringOption);
        command.Options.Add(scriptsDirOption);

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var connectionString = parseResult.GetValue(connectionStringOption);
            var scriptsDir = parseResult.GetValue(scriptsDirOption);
            
            await UpdateDatabaseAsync(connectionString!, scriptsDir!, serviceProvider, cancellationToken);
        });

        rootCommand.Subcommands.Add(command);

        return rootCommand;
    }

    private static async Task UpdateDatabaseAsync(
        string connectionString,
        string scriptsDir,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var handler = serviceProvider.GetRequiredService<IAsyncHandler<UpdateDatabaseCommand, UpdateDatabaseResponse>>();
        
        var command = new UpdateDatabaseCommand(
            ConnectionString: connectionString,
            ScriptsDirectory: scriptsDir);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            Environment.Exit(1);
        }
    }
}
