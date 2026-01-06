using FirebirdSql.Data.FirebirdClient;
using DbMetaTool.Models;

namespace DbMetaTool.Firebird;

public class DatabaseUpdater
{
    private readonly FirebirdConnectionFactory _connectionFactory;
    private readonly MetadataReader _metadataReader;
    private readonly ScriptExecutor _scriptExecutor;

    public DatabaseUpdater(FirebirdConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _metadataReader = new MetadataReader(connectionFactory);
        _scriptExecutor = new ScriptExecutor(connectionFactory);
    }

    public UpdateReport UpdateFromScripts(string scriptsDirectory)
    {
        var report = new UpdateReport();

        UpdateDomains(scriptsDirectory, report);
        UpdateTables(scriptsDirectory, report);
        UpdateProcedures(scriptsDirectory, report);

        return report;
    }

    private void UpdateDomains(string scriptsDirectory, UpdateReport report)
    {
        var domainsDir = Path.Combine(scriptsDirectory, "domains");
        if (!Directory.Exists(domainsDir))
        {
            return;
        }

        var existingDomains = _metadataReader.GetDomains();
        var existingDomainNames = existingDomains.Select(d => d.Name).ToHashSet();

        var scriptFiles = Directory.GetFiles(domainsDir, "*.sql", SearchOption.TopDirectoryOnly);

        foreach (var scriptFile in scriptFiles)
        {
            var domainName = Path.GetFileNameWithoutExtension(scriptFile);

            if (!existingDomainNames.Contains(domainName))
            {
                var result = _scriptExecutor.ExecuteScript(scriptFile, "DOMAIN");

                if (result.Success)
                {
                    report.AddedDomains.Add(domainName);
                }
                else
                {
                    report.Errors.Add($"Błąd podczas tworzenia domeny {domainName}: {result.ErrorMessage}");
                }
            }
        }
    }

    private void UpdateTables(string scriptsDirectory, UpdateReport report)
    {
        var tablesDir = Path.Combine(scriptsDirectory, "tables");
        if (!Directory.Exists(tablesDir))
        {
            return;
        }

        var existingTables = _metadataReader.GetTables();
        var existingTableDict = existingTables.ToDictionary(t => t.Name, t => t);

        var scriptFiles = Directory.GetFiles(tablesDir, "*.sql", SearchOption.TopDirectoryOnly);

        foreach (var scriptFile in scriptFiles)
        {
            var tableName = Path.GetFileNameWithoutExtension(scriptFile);

            if (!existingTableDict.ContainsKey(tableName))
            {
                var result = _scriptExecutor.ExecuteScript(scriptFile, "TABLE");

                if (result.Success)
                {
                    report.AddedTables.Add(tableName);
                }
                else
                {
                    report.Errors.Add($"Błąd podczas tworzenia tabeli {tableName}: {result.ErrorMessage}");
                }
            }
            else
            {
                var existingTable = existingTableDict[tableName];
                var scriptContent = File.ReadAllText(scriptFile);
                var scriptColumns = ParseColumnsFromCreateTableScript(scriptContent);

                CompareAndUpdateTableColumns(tableName, existingTable, scriptColumns, report);
            }
        }
    }

    private void CompareAndUpdateTableColumns(
        string tableName,
        TableMetadata existingTable,
        List<string> scriptColumnNames,
        UpdateReport report)
    {
        var existingColumnNames = existingTable.Columns.Select(c => c.Name).ToHashSet();

        foreach (var scriptColumnName in scriptColumnNames)
        {
            if (!existingColumnNames.Contains(scriptColumnName))
            {
                report.ManualReviewRequired.Add(
                    $"Tabela {tableName}: brakuje kolumny {scriptColumnName}. Wymagana ręczna weryfikacja i ALTER TABLE."
                );
            }
        }
    }

    private void UpdateProcedures(string scriptsDirectory, UpdateReport report)
    {
        var proceduresDir = Path.Combine(scriptsDirectory, "procedures");
        if (!Directory.Exists(proceduresDir))
        {
            return;
        }

        var scriptFiles = Directory.GetFiles(proceduresDir, "*.sql", SearchOption.TopDirectoryOnly);

        foreach (var scriptFile in scriptFiles)
        {
            var procedureName = Path.GetFileNameWithoutExtension(scriptFile);
            var scriptContent = File.ReadAllText(scriptFile);

            var createOrAlterScript = ConvertToCreateOrAlter(scriptContent, procedureName);

            try
            {
                using var connection = _connectionFactory.CreateAndOpenConnection();
                using var command = connection.CreateCommand();

                var statements = SplitProcedureScript(createOrAlterScript);
                foreach (var statement in statements)
                {
                    if (string.IsNullOrWhiteSpace(statement))
                    {
                        continue;
                    }

                    command.CommandText = statement;
                    command.ExecuteNonQuery();
                }

                report.UpdatedProcedures.Add(procedureName);
            }
            catch (Exception ex)
            {
                report.Errors.Add($"Błąd podczas aktualizacji procedury {procedureName}: {ex.Message}");
            }
        }
    }

    private static string ConvertToCreateOrAlter(string scriptContent, string procedureName)
    {
        var lines = scriptContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var result = new List<string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase))
            {
                var replaced = line.Replace("CREATE PROCEDURE", "CREATE OR ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase);
                result.Add(replaced);
            }
            else
            {
                result.Add(line);
            }
        }

        return string.Join(Environment.NewLine, result);
    }

    private static List<string> SplitProcedureScript(string scriptContent)
    {
        var statements = new List<string>();
        var lines = scriptContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var currentStatement = new List<string>();
        var terminator = ";";
        var isProcedure = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                if (currentStatement.Count > 0)
                {
                    currentStatement.Add(line);
                }
                continue;
            }

            if (trimmedLine.StartsWith("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith("CREATE OR ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase))
            {
                isProcedure = true;
                terminator = "^";
            }

            if (trimmedLine == "^")
            {
                if (currentStatement.Count > 0)
                {
                    var statement = string.Join(Environment.NewLine, currentStatement).Trim();
                    if (!string.IsNullOrWhiteSpace(statement))
                    {
                        statements.Add(statement);
                    }
                    currentStatement.Clear();
                }
                isProcedure = false;
                terminator = ";";
                continue;
            }

            if (isProcedure)
            {
                currentStatement.Add(line);
            }
            else if (trimmedLine.EndsWith(";"))
            {
                currentStatement.Add(line.TrimEnd(';'));
                var statement = string.Join(Environment.NewLine, currentStatement).Trim();
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    statements.Add(statement + ";");
                }
                currentStatement.Clear();
            }
            else
            {
                currentStatement.Add(line);
            }
        }

        if (currentStatement.Count > 0)
        {
            var statement = string.Join(Environment.NewLine, currentStatement).Trim();
            if (!string.IsNullOrWhiteSpace(statement))
            {
                if (isProcedure && !statement.EndsWith("^"))
                {
                    // Jeśli to procedura i nie ma terminatora, dodaj go
                    statements.Add(statement);
                }
                else if (!statement.EndsWith(";") && terminator == ";")
                {
                    statement += ";";
                    statements.Add(statement);
                }
                else
                {
                    statements.Add(statement);
                }
            }
        }

        return statements;
    }

    private static List<string> ParseColumnsFromCreateTableScript(string scriptContent)
    {
        var columns = new List<string>();
        var lines = scriptContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var inTableDefinition = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (trimmedLine == "(")
            {
                inTableDefinition = true;
                continue;
            }

            if (trimmedLine.StartsWith(");") || trimmedLine == ")")
            {
                break;
            }

            if (inTableDefinition)
            {
                var columnLine = trimmedLine.TrimEnd(',');
                var parts = columnLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 0)
                {
                    columns.Add(parts[0]);
                }
            }
        }

        return columns;
    }
}

public class UpdateReport
{
    public List<string> AddedDomains { get; } = new List<string>();
    public List<string> AddedTables { get; } = new List<string>();
    public List<string> UpdatedProcedures { get; } = new List<string>();
    public List<string> ManualReviewRequired { get; } = new List<string>();
    public List<string> Errors { get; } = new List<string>();

    public bool HasChanges()
    {
        return AddedDomains.Count > 0 ||
               AddedTables.Count > 0 ||
               UpdatedProcedures.Count > 0;
    }

    public bool HasErrors()
    {
        return Errors.Count > 0;
    }

    public void PrintReport()
    {
        Console.WriteLine();
        Console.WriteLine("=== Raport aktualizacji bazy danych ===");
        Console.WriteLine();

        if (AddedDomains.Count > 0)
        {
            Console.WriteLine("Dodane domeny:");
            foreach (var domain in AddedDomains)
            {
                Console.WriteLine($"  + {domain}");
            }
            Console.WriteLine();
        }

        if (AddedTables.Count > 0)
        {
            Console.WriteLine("Dodane tabele:");
            foreach (var table in AddedTables)
            {
                Console.WriteLine($"  + {table}");
            }
            Console.WriteLine();
        }

        if (UpdatedProcedures.Count > 0)
        {
            Console.WriteLine("Zaktualizowane procedury:");
            foreach (var procedure in UpdatedProcedures)
            {
                Console.WriteLine($"  ~ {procedure}");
            }
            Console.WriteLine();
        }

        if (ManualReviewRequired.Count > 0)
        {
            Console.WriteLine("Wymagana ręczna weryfikacja:");
            foreach (var item in ManualReviewRequired)
            {
                Console.WriteLine($"  ! {item}");
            }
            Console.WriteLine();
        }

        if (Errors.Count > 0)
        {
            Console.WriteLine("Błędy:");
            foreach (var error in Errors)
            {
                Console.WriteLine($"  ✗ {error}");
            }
            Console.WriteLine();
        }

        if (!HasChanges() && !HasErrors() && ManualReviewRequired.Count == 0)
        {
            Console.WriteLine("Brak zmian do zastosowania.");
            Console.WriteLine();
        }
    }
}

