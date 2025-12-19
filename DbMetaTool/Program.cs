using System;
using System.IO;
using System.Linq;

namespace DbMetaTool
{
    public static class Program
    {
        // Przykładowe wywołania:
        // DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
        // DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
        // DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                return RunInteractiveMode();
            }

            try
            {
                var command = args[0].ToLowerInvariant();

                switch (command)
                {
                    case "build-db":
                        {
                            string dbDir = GetArgValue(args, "--db-dir");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            BuildDatabase(dbDir, scriptsDir);
                            Console.WriteLine("Baza danych została zbudowana pomyślnie.");
                            return 0;
                        }

                    case "export-scripts":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string outputDir = GetArgValue(args, "--output-dir");

                            ExportScripts(connStr, outputDir);
                            Console.WriteLine("Skrypty zostały wyeksportowane pomyślnie.");
                            return 0;
                        }

                    case "update-db":
                        {
                            string connStr = GetArgValue(args, "--connection-string");
                            string scriptsDir = GetArgValue(args, "--scripts-dir");

                            UpdateDatabase(connStr, scriptsDir);
                            Console.WriteLine("Baza danych została zaktualizowana pomyślnie.");
                            return 0;
                        }

                    default:
                        Console.WriteLine($"Nieznane polecenie: {command}");
                        Console.WriteLine();
                        ShowUsage();
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Błąd: " + ex.Message);
                return -1;
            }
        }

        private static int RunInteractiveMode()
        {
            Console.Clear();
            Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║   DbMetaTool - Firebird Metadata Exporter            ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Wybierz operację:");
            Console.WriteLine();
            Console.WriteLine("  [1] Build Database      - Zbuduj bazę ze skryptów");
            Console.WriteLine("  [2] Export Scripts      - Eksportuj metadane do plików");
            Console.WriteLine("  [3] Update Database     - Zaktualizuj bazę ze skryptów");
            Console.WriteLine("  [0] Wyjście");
            Console.WriteLine();
            Console.Write("Wybór: ");

            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        return ExecuteBuildDatabaseInteractive();

                    case "2":
                        return ExecuteExportScriptsInteractive();

                    case "3":
                        return ExecuteUpdateDatabaseInteractive();

                    case "0":
                        return 0;

                    default:
                        Console.WriteLine("Nieprawidłowy wybór.");
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"Błąd: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Naciśnij Enter aby kontynuować...");
                Console.ReadLine();
                return -1;
            }
        }

        private static string GetProjectRootDirectory()
        {
            var baseDir = AppContext.BaseDirectory;
            var currentDir = Directory.GetCurrentDirectory();

            var searchDirs = new List<string> { currentDir };

            var dir = baseDir;
            for (int i = 0; i < 10; i++)
            {
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    searchDirs.Add(dir);
                    var parent = Directory.GetParent(dir);
                    if (parent == null)
                    {
                        break;
                    }
                    dir = parent.FullName;
                }
                else
                {
                    break;
                }
            }

            foreach (var searchDir in searchDirs)
            {
                if (string.IsNullOrEmpty(searchDir))
                {
                    continue;
                }

                var composePath = Path.Combine(searchDir, "compose", "docker-compose.yaml");
                if (File.Exists(composePath))
                {
                    return searchDir;
                }

                var slnPath = Directory.GetFiles(searchDir, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (slnPath != null)
                {
                    return searchDir;
                }
            }

            var fallbackDir = Directory.GetParent(Directory.GetParent(Directory.GetParent(baseDir)?.FullName ?? "")?.FullName ?? "")?.FullName ?? "";
            if (!string.IsNullOrEmpty(fallbackDir) && Directory.Exists(fallbackDir))
            {
                return fallbackDir;
            }

            return currentDir;
        }

        private static int ExecuteBuildDatabaseInteractive()
        {
            Console.WriteLine();
            Console.WriteLine("=== Build Database ===");
            Console.WriteLine();
            Console.WriteLine("UWAGA: Przed budową bazy upewnij się, że kontener Docker jest uruchomiony:");
            Console.WriteLine("       cd compose && docker compose up -d");
            Console.WriteLine();

            var projectRoot = GetProjectRootDirectory();
            Console.WriteLine($"Katalog projektu: {projectRoot}");
            Console.WriteLine($"Aktualny katalog roboczy: {Environment.CurrentDirectory}");
            Console.WriteLine();

            Console.Write("Katalog dla bazy danych (domyślnie: ./compose/data): ");
            var dbDir = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(dbDir))
            {
                dbDir = "./compose/data";
            }

            Console.Write("Katalog ze skryptami (domyślnie: ./scripts): ");
            var scriptsDir = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(scriptsDir))
            {
                scriptsDir = "./scripts";
            }

            var fullDbDir = Path.IsPathRooted(dbDir) ? dbDir : Path.GetFullPath(Path.Combine(projectRoot, dbDir));
            var fullScriptsDir = Path.IsPathRooted(scriptsDir) ? scriptsDir : Path.GetFullPath(Path.Combine(projectRoot, scriptsDir));
            var localDbPath = Path.Combine(fullDbDir, "database.fdb");
            
            Console.WriteLine();
            Console.WriteLine($"Pełna ścieżka bazy: {fullDbDir}");
            Console.WriteLine($"Pełna ścieżka skryptów: {fullScriptsDir}");
            Console.WriteLine();

            if (File.Exists(localDbPath))
            {
                Console.WriteLine($"⚠ Plik bazy już istnieje: {localDbPath}");
                Console.Write("Czy chcesz go usunąć i utworzyć nową bazę? (t/n): ");
                var confirm = Console.ReadLine()?.Trim().ToLower();
                
                if (confirm == "t" || confirm == "y" || confirm == "tak" || confirm == "yes")
                {
                    try
                    {
                        File.Delete(localDbPath);
                        Console.WriteLine("✓ Stary plik bazy został usunięty.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Nie można usunąć pliku: {ex.Message}");
                        Console.WriteLine("Naciśnij Enter aby kontynuować...");
                        Console.ReadLine();
                        return 1;
                    }
                }
                else
                {
                    Console.WriteLine("Operacja anulowana.");
                    Console.WriteLine("Naciśnij Enter aby kontynuować...");
                    Console.ReadLine();
                    return 0;
                }
            }

            Console.WriteLine($"Lokalny katalog: {fullDbDir}");
            Console.WriteLine($"Docker ścieżka: /var/lib/firebird/data/database.fdb");
            Console.WriteLine($"Katalog skryptów: {fullScriptsDir}");
            Console.WriteLine();

            BuildDatabase(fullDbDir, fullScriptsDir);
            
            Console.WriteLine();
            Console.WriteLine("✓ Baza danych została zbudowana pomyślnie.");
            Console.WriteLine();
            Console.WriteLine("Plik bazy utworzony lokalnie: {0}", localDbPath);
            Console.WriteLine();
            Console.WriteLine("Connection string do użycia:");
            Console.WriteLine("Database=localhost:/var/lib/firebird/data/database.fdb;User=SYSDBA;Password=masterkey;Charset=UTF8");
            Console.WriteLine();
            Console.WriteLine("Naciśnij Enter aby kontynuować...");
            Console.ReadLine();
            return 0;
        }

        private static int ExecuteExportScriptsInteractive()
        {
            Console.WriteLine();
            Console.WriteLine("=== Export Scripts ===");
            Console.WriteLine();
            Console.WriteLine("Standardowy connection string dla Docker:");
            Console.WriteLine("Database=localhost:/var/lib/firebird/data/database.fdb;User=SYSDBA;Password=masterkey;Charset=UTF8");
            Console.WriteLine();

            Console.Write("Connection string: ");
            var connStr = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(connStr))
            {
                Console.WriteLine("Connection string jest wymagany.");
                return 1;
            }

            Console.Write("Katalog wyjściowy (np. ./output): ");
            var outputDir = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(outputDir))
            {
                Console.WriteLine("Katalog wyjściowy jest wymagany.");
                return 1;
            }

            var projectRoot = GetProjectRootDirectory();
            var fullOutputDir = Path.IsPathRooted(outputDir) ? outputDir : Path.GetFullPath(Path.Combine(projectRoot, outputDir));

            Console.WriteLine();
            Console.WriteLine($"Pełna ścieżka wyjściowa: {fullOutputDir}");
            Console.WriteLine();
            Console.WriteLine("Testowanie połączenia...");

            if (!TestConnection(connStr))
            {
                Console.WriteLine();
                Console.WriteLine("✗ Nie można połączyć się z bazą danych.");
                Console.WriteLine();
                Console.WriteLine("Możliwe przyczyny:");
                Console.WriteLine("  1. Kontener Docker nie jest uruchomiony");
                Console.WriteLine("     Rozwiązanie: cd compose && docker compose up -d");
                Console.WriteLine();
                Console.WriteLine("  2. Baza danych nie istnieje");
                Console.WriteLine("     Rozwiązanie: Użyj opcji [1] Build Database");
                Console.WriteLine();
                Console.WriteLine("  3. Nieprawidłowy connection string");
                Console.WriteLine("     Sprawdź nazwę pliku bazy i ścieżkę");
                Console.WriteLine();
                Console.WriteLine("Naciśnij Enter aby kontynuować...");
                Console.ReadLine();
                return 1;
            }

            Console.WriteLine("✓ Połączenie OK");
            Console.WriteLine();

            ExportScripts(connStr, fullOutputDir);
            Console.WriteLine();
            Console.WriteLine("✓ Skrypty zostały wyeksportowane pomyślnie.");
            Console.WriteLine($"  Zapisane w: {fullOutputDir}");
            Console.WriteLine();
            Console.WriteLine("Naciśnij Enter aby kontynuować...");
            Console.ReadLine();
            return 0;
        }

        private static int ExecuteUpdateDatabaseInteractive()
        {
            Console.WriteLine();
            Console.WriteLine("=== Update Database ===");
            Console.WriteLine();
            Console.WriteLine("Standardowy connection string dla Docker:");
            Console.WriteLine("Database=localhost:/var/lib/firebird/data/database.fdb;User=SYSDBA;Password=masterkey;Charset=UTF8");
            Console.WriteLine();

            Console.Write("Connection string: ");
            var connStr = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(connStr))
            {
                Console.WriteLine("Connection string jest wymagany.");
                return 1;
            }

            Console.Write("Katalog ze skryptami (np. ./scripts): ");
            var scriptsDir = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(scriptsDir))
            {
                Console.WriteLine("Katalog ze skryptami jest wymagany.");
                return 1;
            }

            var projectRoot = GetProjectRootDirectory();
            var fullScriptsDir = Path.IsPathRooted(scriptsDir) ? scriptsDir : Path.GetFullPath(Path.Combine(projectRoot, scriptsDir));

            Console.WriteLine();
            Console.WriteLine($"Pełna ścieżka skryptów: {fullScriptsDir}");
            Console.WriteLine();
            Console.WriteLine("Testowanie połączenia...");

            if (!TestConnection(connStr))
            {
                Console.WriteLine();
                Console.WriteLine("✗ Nie można połączyć się z bazą danych.");
                Console.WriteLine();
                Console.WriteLine("Możliwe przyczyny:");
                Console.WriteLine("  1. Kontener Docker nie jest uruchomiony");
                Console.WriteLine("     Rozwiązanie: cd compose && docker compose up -d");
                Console.WriteLine();
                Console.WriteLine("  2. Baza danych nie istnieje");
                Console.WriteLine("     Rozwiązanie: Użyj opcji [1] Build Database");
                Console.WriteLine();
                Console.WriteLine("  3. Nieprawidłowy connection string");
                Console.WriteLine("     Sprawdź nazwę pliku bazy i ścieżkę");
                Console.WriteLine();
                Console.WriteLine("Naciśnij Enter aby kontynuować...");
                Console.ReadLine();
                return 1;
            }

            Console.WriteLine("✓ Połączenie OK");
            Console.WriteLine();

            UpdateDatabase(connStr, fullScriptsDir);
            Console.WriteLine();
            Console.WriteLine("✓ Baza danych została zaktualizowana pomyślnie.");
            Console.WriteLine();
            Console.WriteLine("Naciśnij Enter aby kontynuować...");
            Console.ReadLine();
            return 0;
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Użycie:");
            Console.WriteLine("  build-db --db-dir <ścieżka> --scripts-dir <ścieżka>");
            Console.WriteLine("  export-scripts --connection-string <connStr> --output-dir <ścieżka>");
            Console.WriteLine("  update-db --connection-string <connStr> --scripts-dir <ścieżka>");
            Console.WriteLine();
            Console.WriteLine("Lub uruchom bez parametrów dla trybu interaktywnego.");
        }

        private static bool TestConnection(string connectionString)
        {
            try
            {
                var connectionFactory = new Firebird.FirebirdConnectionFactory(connectionString);
                using var connection = connectionFactory.CreateAndOpenConnection();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetArgValue(string[] args, string name)
        {
            int idx = Array.IndexOf(args, name);
            if (idx == -1 || idx + 1 >= args.Length)
                throw new ArgumentException($"Brak wymaganego parametru {name}");
            return args[idx + 1];
        }

        private static void PrintExecutionReport(List<Firebird.ExecutionResult> results)
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

        /// <summary>
        /// Buduje nową bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void BuildDatabase(string databaseDirectory, string scriptsDirectory)
        {
            Directory.CreateDirectory(databaseDirectory);

            var databaseFileName = "database.fdb";
            var localDatabasePath = Path.GetFullPath(Path.Combine(databaseDirectory, databaseFileName));
            var dockerDatabasePath = "/var/lib/firebird/data/" + databaseFileName;

            Console.WriteLine("Tworzenie bazy danych...");
            Console.WriteLine($"  Lokalna ścieżka: {localDatabasePath}");
            Console.WriteLine($"  Docker ścieżka: {dockerDatabasePath}");
            Console.WriteLine();

            var createConnectionString = Firebird.FirebirdConnectionFactory.BuildConnectionString(
                dataSource: "localhost",
                database: dockerDatabasePath,
                userId: "SYSDBA",
                password: "masterkey",
                charset: "UTF8"
            );

            try
            {
                Firebird.FirebirdConnectionFactory.CreateDatabase(createConnectionString);
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

            var useConnectionString = Firebird.FirebirdConnectionFactory.BuildConnectionString(
                dataSource: "localhost",
                database: dockerDatabasePath,
                userId: "SYSDBA",
                password: "masterkey",
                charset: "UTF8"
            );

            var connectionFactory = new Firebird.FirebirdConnectionFactory(useConnectionString);
            var scriptExecutor = new Firebird.ScriptExecutor(connectionFactory);

            var results = scriptExecutor.ExecuteScriptsFromDirectory(scriptsDirectory);

            PrintExecutionReport(results);

            var failedCount = results.Count(r => !r.Success);
            if (failedCount > 0)
            {
                throw new Exception($"Wykonanie skryptów zakończone z błędami: {failedCount} niepowodzeń.");
            }
        }

        /// <summary>
        /// Generuje skrypty metadanych z istniejącej bazy danych Firebird 5.0.
        /// </summary>
        public static void ExportScripts(string connectionString, string outputDirectory)
        {
            var connectionFactory = new Firebird.FirebirdConnectionFactory(connectionString);
            var metadataReader = new Firebird.MetadataReader(connectionFactory);
            var sqlExporter = new Exporters.SqlExporter(outputDirectory);

            var domains = metadataReader.GetDomains();
            sqlExporter.ExportDomains(domains);

            var tables = metadataReader.GetTables();
            sqlExporter.ExportTables(tables);

            var procedures = metadataReader.GetProcedures();
            sqlExporter.ExportProcedures(procedures);
        }

        /// <summary>
        /// Aktualizuje istniejącą bazę danych Firebird 5.0 na podstawie skryptów.
        /// </summary>
        public static void UpdateDatabase(string connectionString, string scriptsDirectory)
        {
            var connectionFactory = new Firebird.FirebirdConnectionFactory(connectionString);
            var databaseUpdater = new Firebird.DatabaseUpdater(connectionFactory);

            var report = databaseUpdater.UpdateFromScripts(scriptsDirectory);
            report.PrintReport();

            if (report.HasErrors())
            {
                throw new Exception("Aktualizacja zakończona z błędami.");
            }
        }
    }
}