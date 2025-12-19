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
                var scriptColumnDefinitions = ParseColumnDefinitionsFromCreateTableScript(scriptContent);

                CompareAndUpdateTableColumns(tableName, existingTable, scriptColumnDefinitions, report);
            }
        }
    }

    private void CompareAndUpdateTableColumns(
        string tableName,
        TableMetadata existingTable,
        List<ColumnDefinition> scriptColumnDefinitions,
        UpdateReport report)
    {
        var existingColumnNames = existingTable.Columns.Select(c => c.Name).ToHashSet();

        foreach (var scriptColumn in scriptColumnDefinitions)
        {
            if (!existingColumnNames.Contains(scriptColumn.Name))
            {
                try
                {
                    // Automatycznie dodaj brakującą kolumnę
                    var alterTableSql = BuildAlterTableAddColumnSql(tableName, scriptColumn);
                    
                    using var connection = _connectionFactory.CreateAndOpenConnection();
                    using var command = connection.CreateCommand();
                    command.CommandText = alterTableSql;
                    command.ExecuteNonQuery();

                    report.AddedColumns.Add($"{tableName}.{scriptColumn.Name}");
                }
                catch (Exception ex)
                {
                    report.Errors.Add($"Błąd podczas dodawania kolumny {scriptColumn.Name} do tabeli {tableName}: {ex.Message}");
                }
            }
        }
    }

    private static string BuildAlterTableAddColumnSql(string tableName, ColumnDefinition column)
    {
        var sql = $"ALTER TABLE {tableName} ADD {column.Name} {column.DataType}";
        
        if (!column.IsNullable)
        {
            sql += " NOT NULL";
        }
        
        if (!string.IsNullOrEmpty(column.DefaultValue))
        {
            sql += $" DEFAULT {column.DefaultValue}";
        }
        
        sql += ";";
        
        return sql;
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

    private static List<ColumnDefinition> ParseColumnDefinitionsFromCreateTableScript(string scriptContent)
    {
        var columns = new List<ColumnDefinition>();
        var lines = scriptContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
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

            if (inTableDefinition && !string.IsNullOrWhiteSpace(trimmedLine))
            {
                var columnDef = ParseColumnDefinition(trimmedLine.TrimEnd(','));
                if (columnDef != null)
                {
                    columns.Add(columnDef);
                }
            }
        }

        return columns;
    }

    private static ColumnDefinition? ParseColumnDefinition(string columnLine)
    {
        // Przykład: "ID INTEGER NOT NULL" lub "NAME VARCHAR(255)" lub "EMAIL D_EMAIL" lub "CREATED_AT D_TIMESTAMP"
        // Format: NAME TYPE [NOT NULL] [DEFAULT value]
        
        var trimmed = columnLine.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        // Znajdź pierwszą spację - to jest koniec nazwy kolumny
        var firstSpaceIndex = trimmed.IndexOf(' ');
        if (firstSpaceIndex <= 0)
        {
            return null;
        }

        var name = trimmed.Substring(0, firstSpaceIndex).Trim();
        var rest = trimmed.Substring(firstSpaceIndex).Trim();

        // Sprawdź czy jest NOT NULL
        var isNullable = true;
        if (rest.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase))
        {
            isNullable = false;
            rest = rest.Replace("NOT NULL", "", StringComparison.OrdinalIgnoreCase).Trim();
        }
        else if (rest.EndsWith("NOT NULL", StringComparison.OrdinalIgnoreCase))
        {
            isNullable = false;
            rest = rest.Substring(0, rest.Length - 8).Trim();
        }

        // Sprawdź czy jest DEFAULT
        string? defaultValue = null;
        var defaultIndex = rest.IndexOf("DEFAULT", StringComparison.OrdinalIgnoreCase);
        if (defaultIndex >= 0)
        {
            var defaultPart = rest.Substring(defaultIndex + 7).Trim();
            // Pobierz wartość DEFAULT (może być po spacji)
            var defaultValueEnd = defaultPart.IndexOf(' ');
            if (defaultValueEnd > 0)
            {
                defaultValue = defaultPart.Substring(0, defaultValueEnd).Trim();
            }
            else
            {
                defaultValue = defaultPart.Trim();
            }
            rest = rest.Substring(0, defaultIndex).Trim();
        }

        // Reszta to typ danych (może zawierać nawiasy, np. VARCHAR(255) lub domenę D_EMAIL)
        var dataType = rest.Trim();

        return new ColumnDefinition(
            Name: name,
            DataType: dataType,
            IsNullable: isNullable,
            DefaultValue: defaultValue
        );
    }
}

public record ColumnDefinition(
    string Name,
    string DataType,
    bool IsNullable,
    string? DefaultValue
);

public class UpdateReport
{
    public List<string> AddedDomains { get; } = new List<string>();
    public List<string> AddedTables { get; } = new List<string>();
    public List<string> AddedColumns { get; } = new List<string>();
    public List<string> UpdatedProcedures { get; } = new List<string>();
    public List<string> ManualReviewRequired { get; } = new List<string>();
    public List<string> Errors { get; } = new List<string>();

    public bool HasChanges()
    {
        return AddedDomains.Count > 0 ||
               AddedTables.Count > 0 ||
               AddedColumns.Count > 0 ||
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

        if (AddedColumns.Count > 0)
        {
            Console.WriteLine("Dodane kolumny:");
            foreach (var column in AddedColumns)
            {
                Console.WriteLine($"  + {column}");
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

