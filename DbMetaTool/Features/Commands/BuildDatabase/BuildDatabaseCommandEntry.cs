using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace DbMetaTool.Features.Commands.BuildDatabase;

public static class BuildDatabaseCommandEntry
{
    public static RootCommand MapBuildDatabase(this RootCommand rootCommand, IServiceProvider serviceProvider)
    {
        var command = new Command("build-db", "Buduje nową bazę danych Firebird z skryptów");

        var dbDirOption = new Option<string>("--db-dir")
        {
            Description = "Katalog, w którym zostanie utworzona baza danych",
            Required = true
        };

        var scriptsDirOption = new Option<string>("--scripts-dir")
        {
            Description = "Katalog zawierający skrypty SQL do wykonania",
            Required = true
        };

        command.Options.Add(dbDirOption);
        command.Options.Add(scriptsDirOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var dbDir = parseResult.GetValue(dbDirOption);
            var scriptsDir = parseResult.GetValue(scriptsDirOption);
            
            await BuildDatabaseAsync(dbDir!, scriptsDir!, serviceProvider, cancellationToken);
        });

        rootCommand.Subcommands.Add(command);

        return rootCommand;
    }

    private static async Task BuildDatabaseAsync(
        string dbDir,
        string scriptsDir,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var handler = serviceProvider.GetRequiredService<IAsyncHandler<BuildDatabaseCommand, BuildDatabaseResponse>>();
        
        var command = new BuildDatabaseCommand(
            DatabasePath: dbDir,
            ScriptsDirectory: scriptsDir);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            Environment.Exit(1);
        }
    }
}
