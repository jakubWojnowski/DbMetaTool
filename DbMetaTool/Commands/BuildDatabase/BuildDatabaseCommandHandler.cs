using DbMetaTool.Services;
using DbMetaTool.Services.Build;
using DbMetaTool.Utilities;

namespace DbMetaTool.Commands.BuildDatabase;

public static class BuildDatabaseCommandHandler
{
    public static void Handle(BuildDatabaseCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        Console.WriteLine("=== Budowanie bazy danych Firebird ===");
        Console.WriteLine();

        var (databaseDirectory, databaseFilePath) = DatabasePathHelper.BuildDatabasePaths(command.DatabasePath);

        Console.WriteLine($"Katalog bazy: {databaseDirectory}");
        Console.WriteLine($"Plik bazy: {databaseFilePath}");
        Console.WriteLine($"Katalog skryptów: {command.ScriptsDirectory}");
        Console.WriteLine();

        var result = DatabaseBuildService.BuildDatabase(databaseFilePath, command.ScriptsDirectory);

        BuildReportGenerator.DisplayReport(result);
    }
}
