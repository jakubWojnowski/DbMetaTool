using System.CommandLine;
using DbMetaTool.Databases;
using DbMetaTool.Features.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace DbMetaTool.Features.Commands.BuildDatabase;

public static class BuildDatabaseCommandEntry
{
    public static RootCommand MapBuildDatabase(this RootCommand rootCommand, IServiceProvider serviceProvider)
    {
        var command = new Command("build-db", "Buduje nową bazę danych z skryptów");

        var databaseTypeOption = new Option<DatabaseType>("--database-type")
        {
            Description = "Typ bazy danych (Firebird, SqlServer, PostgreSQL, etc.)",
            Required = true
        };

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

        command.Options.Add(databaseTypeOption);
        command.Options.Add(dbDirOption);
        command.Options.Add(scriptsDirOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var databaseType = parseResult.GetValue(databaseTypeOption);
            var dbDir = parseResult.GetValue(dbDirOption);
            var scriptsDir = parseResult.GetValue(scriptsDirOption);
            var handler = serviceProvider.GetRequiredService<IAsyncHandler<BuildDatabaseCommand, BuildDatabaseResponse>>();
            
            await BuildDatabaseAsync(databaseType, dbDir!, scriptsDir!, handler, cancellationToken);
        });

        rootCommand.Subcommands.Add(command);

        return rootCommand;
    }

    private static async Task BuildDatabaseAsync(
        DatabaseType databaseType,
        string dbDir,
        string scriptsDir,
        IAsyncHandler<BuildDatabaseCommand, BuildDatabaseResponse> handler,
        CancellationToken cancellationToken = default)
    {
        var command = new BuildDatabaseCommand(
            DatabaseType: databaseType,
            DatabasePath: dbDir,
            ScriptsDirectory: scriptsDir);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.Success)
        {
            Environment.Exit(1);
        }
    }
}
