using System;
using System.IO;
using System.Linq;
using DbMetaTool.Firebird;

namespace DbMetaTool.Commands
{
    public class BuildDatabaseCommand
    {
        public static void Execute(string databaseDirectory, string scriptsDirectory)
        {
            var fullDatabaseDirectory = databaseDirectory;
            if (!databaseDirectory.Contains(Path.DirectorySeparatorChar) && 
                !databaseDirectory.Contains('/') && 
                !databaseDirectory.Contains('\\') &&
                !Path.IsPathRooted(databaseDirectory))
            {
                fullDatabaseDirectory = Path.Combine("compose", "data", databaseDirectory);
            }
            
            Directory.CreateDirectory(fullDatabaseDirectory);

            var directoryName = new DirectoryInfo(Path.GetFullPath(fullDatabaseDirectory)).Name;
            var databaseFileName = $"{directoryName}.fdb";
            
            var dockerDatabasePath = $"/var/lib/firebird/data/{directoryName}/{databaseFileName}";
            var localDatabasePath = Path.Combine(fullDatabaseDirectory, databaseFileName);

            Console.WriteLine("Tworzenie bazy danych...");
            Console.WriteLine($"  Lokalna ścieżka: {localDatabasePath}");
            Console.WriteLine($"  Docker ścieżka: {dockerDatabasePath}");
            Console.WriteLine();

            var createConnectionString = FirebirdConnectionFactory.BuildConnectionString(
                dataSource: "localhost",
                database: dockerDatabasePath,
                userId: "SYSDBA",
                password: "masterkey",
                charset: "UTF8"
            );

            try
            {
                FirebirdConnectionFactory.CreateDatabase(createConnectionString);
                Console.WriteLine("✓ Baza danych utworzona pomyślnie");
            }
            catch (Exception ex)
            {
                var errorMsg = ex.Message;
                if (errorMsg.Contains("I/O error") || errorMsg.Contains("open"))
                {
                    throw new Exception(
                        $"Nie można utworzyć bazy danych przez sieć.\n" +
                        $"Upewnij się, że:\n" +
                        $"  1. Kontener Docker jest uruchomiony (docker ps)\n" +
                        $"  2. Volume jest poprawnie zmapowany (./compose/data -> /var/lib/firebird/data)\n" +
                        $"  3. Firebird ma uprawnienia do zapisu w volume\n" +
                        $"\nSzczegóły: {errorMsg}", ex);
                }
                throw;
            }

            Console.WriteLine();
            Console.WriteLine("Wykonywanie skryptów...");

            var useConnectionString = FirebirdConnectionFactory.BuildConnectionString(
                dataSource: "localhost",
                database: dockerDatabasePath,
                userId: "SYSDBA",
                password: "masterkey",
                charset: "UTF8"
            );

            var connectionFactory = new FirebirdConnectionFactory(useConnectionString);
            var scriptExecutor = new ScriptExecutor(connectionFactory);

            var results = scriptExecutor.ExecuteScriptsFromDirectory(scriptsDirectory);

            PrintExecutionReport(results);

            var failedCount = results.Count(r => !r.Success);
            if (failedCount > 0)
            {
                throw new Exception($"Wykonanie skryptów zakończone z błędami: {failedCount} niepowodzeń.");
            }
        }

        private static void PrintExecutionReport(List<ExecutionResult> results)
        {
            Console.WriteLine();
            Console.WriteLine("=== Raport wykonania skryptów ===");
            Console.WriteLine();

            var grouped = results.GroupBy(r => r.Category);

            foreach (var group in grouped)
            {
                Console.WriteLine($"{group.Key}:");
                foreach (var result in group)
                {
                    var status = result.Success ? "[OK]" : "[BŁĄD]";
                    var fileName = Path.GetFileName(result.ScriptPath);
                    Console.WriteLine($"  {status} {fileName}");

                    if (!result.Success && result.ErrorMessage != null)
                    {
                        Console.WriteLine($"       {result.ErrorMessage}");
                    }
                }
                Console.WriteLine();
            }

            var successCount = results.Count(r => r.Success);
            var failedCount = results.Count(r => !r.Success);

            Console.WriteLine($"Podsumowanie: {successCount} sukces, {failedCount} błąd");
            Console.WriteLine();
        }
    }
}

