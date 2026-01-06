using DbMetaTool.Firebird;
using DbMetaTool.Models;
using DbMetaTool.Services;
using DbMetaTool.Utilities;

namespace DbMetaTool.Commands.BuildDatabase;

public static class BuildDatabaseCommandHandler
{
    public static void Handle(BuildDatabaseCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        Console.WriteLine("=== Budowanie bazy danych Firebird ===");
        Console.WriteLine();

        var (databaseDirectory, databaseFilePath) = DatabasePathHelper.BuildDatabasePaths(command.DatabasePath);

        Console.WriteLine($"Katalog bazy: {databaseDirectory}");
        Console.WriteLine($"Plik bazy: {databaseFilePath}");
        Console.WriteLine($"Katalog skryptów: {command.ScriptsDirectory}");
        Console.WriteLine();

        FirebirdDatabaseCreator.CreateDatabase(databaseFilePath);
        Console.WriteLine("✓ Utworzono pustą bazę danych");

        var scripts = ScriptLoader.LoadScriptsInOrder(command.ScriptsDirectory);

        if (scripts.Count == 0)
        {
            Console.WriteLine("⚠ Nie znaleziono żadnych skryptów do wykonania");
            return;
        }

        Console.WriteLine($"Znaleziono {scripts.Count} skryptów do wykonania:");
        Console.WriteLine($"  - Domeny: {scripts.Count(s => s.Type == ScriptType.Domain)}");
        Console.WriteLine($"  - Tabele: {scripts.Count(s => s.Type == ScriptType.Table)}");
        Console.WriteLine($"  - Procedury: {scripts.Count(s => s.Type == ScriptType.Procedure)}");
        Console.WriteLine();

        var connectionString = FirebirdConnectionFactory.BuildConnectionString(databaseFilePath);
        var connectionFactory = new FirebirdConnectionFactory(connectionString);

        using var sqlExecutor = new FirebirdSqlExecutor(connectionFactory);

        var executedCount = 0;
        var failedCount = 0;

        sqlExecutor.ExecuteInTransaction(executor =>
        {
            foreach (var script in scripts)
            {
                try
                {
                    Console.Write($"Wykonywanie: {script.Type}/{script.FileName}... ");
                    
                    var sql = ScriptLoader.ReadScriptContent(script);
                    executor.ExecuteScript(sql);
                    
                    executedCount++;
                    Console.WriteLine("✓");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    Console.WriteLine($"✗ Błąd: {ex.Message}");
                    throw;
                }
            }
        });

        Console.WriteLine();
        Console.WriteLine("=== Podsumowanie ===");
        Console.WriteLine($"Wykonano pomyślnie: {executedCount}");
        Console.WriteLine($"Błędy: {failedCount}");
        Console.WriteLine();
        Console.WriteLine("Connection String:");
        Console.WriteLine(connectionString);
    }
}
