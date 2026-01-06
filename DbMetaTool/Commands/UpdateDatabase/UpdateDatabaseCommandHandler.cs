using DbMetaTool.Firebird;
using DbMetaTool.Models;
using DbMetaTool.Services;
using DbMetaTool.Utilities;

namespace DbMetaTool.Commands.UpdateDatabase;

public static class UpdateDatabaseCommandHandler
{
    public static void Handle(UpdateDatabaseCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        Console.WriteLine("=== Aktualizacja bazy danych Firebird ===");
        Console.WriteLine();
        Console.WriteLine($"Connection String: {command.ConnectionString}");
        Console.WriteLine($"Katalog skryptów: {command.ScriptsDirectory}");
        Console.WriteLine();

        var connectionFactory = new FirebirdConnectionFactory(command.ConnectionString);
        using var sqlExecutor = new FirebirdSqlExecutor(connectionFactory);

        Console.WriteLine("Pobieranie aktualnego stanu bazy...");
        var existingDomains = FirebirdMetadataReader.ReadDomains(sqlExecutor);
        var existingTables = FirebirdMetadataReader.ReadTables(sqlExecutor);
        var existingProcedures = FirebirdMetadataReader.ReadProcedures(sqlExecutor);

        Console.WriteLine($"✓ Obecny stan: {existingDomains.Count} domen, {existingTables.Count} tabel, {existingProcedures.Count} procedur");
        Console.WriteLine();

        var scripts = ScriptLoader.LoadScriptsInOrder(command.ScriptsDirectory);
        Console.WriteLine($"Wczytano {scripts.Count} skryptów");
        Console.WriteLine();

        var changes = new List<DatabaseChange>();

        ProcessDomains(sqlExecutor, scripts, existingDomains, changes);
        ProcessTables(sqlExecutor, scripts, existingTables, changes);
        ProcessProcedures(sqlExecutor, scripts, existingProcedures, changes);

        DisplayReport(changes);
    }

    private static void ProcessDomains(
        ISqlExecutor executor,
        List<ScriptFile> scripts,
        List<DomainMetadata> existingDomains,
        List<DatabaseChange> changes)
    {
        Console.WriteLine("=== Przetwarzanie domen ===");

        var domainScripts = scripts.Where(s => s.Type == ScriptType.Domain).ToList();

        foreach (var script in domainScripts)
        {
            var domainName = Path.GetFileNameWithoutExtension(script.FileName);
            var exists = existingDomains.Any(d => d.Name.Equals(domainName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                try
                {
                    Console.Write($"  Tworzenie domeny {domainName}... ");
                    var sql = ScriptLoader.ReadScriptContent(script);
                    var statements = SqlScriptParser.ParseScript(sql);

                    foreach (var statement in statements)
                    {
                        executor.ExecuteNonQuery(statement);
                    }

                    changes.Add(new DatabaseChange(
                        ChangeType.DomainCreated,
                        domainName,
                        null));
                    Console.WriteLine("✓");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Błąd: {ex.Message}");
                    changes.Add(new DatabaseChange(
                        ChangeType.ManualReviewRequired,
                        domainName,
                        $"Błąd tworzenia: {ex.Message}"));
                }
            }
            else
            {
                Console.WriteLine($"  Domena {domainName} już istnieje - pomijam");
            }
        }

        Console.WriteLine();
    }

    private static void ProcessTables(
        ISqlExecutor executor,
        List<ScriptFile> scripts,
        List<TableMetadata> existingTables,
        List<DatabaseChange> changes)
    {
        Console.WriteLine("=== Przetwarzanie tabel ===");

        var tableScripts = scripts.Where(s => s.Type == ScriptType.Table).ToList();

        foreach (var script in tableScripts)
        {
            var tableName = Path.GetFileNameWithoutExtension(script.FileName);
            var existingTable = existingTables.FirstOrDefault(t =>
                t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));

            if (existingTable == null)
            {
                try
                {
                    Console.Write($"  Tworzenie tabeli {tableName}... ");
                    var sql = ScriptLoader.ReadScriptContent(script);
                    var statements = SqlScriptParser.ParseScript(sql);

                    foreach (var statement in statements)
                    {
                        executor.ExecuteNonQuery(statement);
                    }

                    changes.Add(new DatabaseChange(
                        ChangeType.TableCreated,
                        tableName,
                        null));
                    Console.WriteLine("✓");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Błąd: {ex.Message}");
                    changes.Add(new DatabaseChange(
                        ChangeType.ManualReviewRequired,
                        tableName,
                        $"Błąd tworzenia: {ex.Message}"));
                }
            }
            else
            {
                Console.WriteLine($"  Tabela {tableName} istnieje - sprawdzam kolumny...");
                ProcessTableColumns(executor, script, existingTable, changes);
            }
        }

        Console.WriteLine();
    }

    private static void ProcessTableColumns(
        ISqlExecutor executor,
        ScriptFile script,
        TableMetadata existingTable,
        List<DatabaseChange> changes)
    {
        var sql = ScriptLoader.ReadScriptContent(script);
        var desiredTable = ParseTableFromScript(sql, existingTable.Name);

        if (desiredTable == null)
        {
            Console.WriteLine($"    ⚠ Nie można sparsować skryptu tabeli");
            return;
        }

        var alterStatements = DatabaseSchemaComparer.GenerateAlterStatements(existingTable, desiredTable);

        foreach (var statement in alterStatements)
        {
            if (statement.StartsWith("--"))
            {
                Console.WriteLine($"    ⚠ {statement}");
                changes.Add(new DatabaseChange(
                    ChangeType.ManualReviewRequired,
                    existingTable.Name,
                    statement));
            }
            else
            {
                try
                {
                    Console.Write($"    Dodawanie kolumny... ");
                    executor.ExecuteNonQuery(statement);

                    var columnName = ExtractColumnName(statement);
                    changes.Add(new DatabaseChange(
                        ChangeType.ColumnAdded,
                        $"{existingTable.Name}.{columnName}",
                        statement));
                    Console.WriteLine("✓");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Błąd: {ex.Message}");
                    changes.Add(new DatabaseChange(
                        ChangeType.ManualReviewRequired,
                        existingTable.Name,
                        $"Błąd ALTER: {ex.Message}"));
                }
            }
        }
    }

    private static void ProcessProcedures(
        ISqlExecutor executor,
        List<ScriptFile> scripts,
        List<ProcedureMetadata> existingProcedures,
        List<DatabaseChange> changes)
    {
        Console.WriteLine("=== Przetwarzanie procedur ===");

        var procedureScripts = scripts.Where(s => s.Type == ScriptType.Procedure).ToList();

        foreach (var script in procedureScripts)
        {
            var procedureName = Path.GetFileNameWithoutExtension(script.FileName);

            try
            {
                Console.Write($"  Procedura {procedureName}... ");
                var sql = ScriptLoader.ReadScriptContent(script);
                var statements = SqlScriptParser.ParseScript(sql);

                foreach (var statement in statements)
                {
                    if (!string.IsNullOrWhiteSpace(statement))
                    {
                        executor.ExecuteNonQuery(statement);
                    }
                }

                changes.Add(new DatabaseChange(
                    ChangeType.ProcedureModified,
                    procedureName,
                    "Wykonano skrypt"));
                Console.WriteLine("✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Błąd: {ex.Message}");
                changes.Add(new DatabaseChange(
                    ChangeType.ManualReviewRequired,
                    procedureName,
                    $"Błąd: {ex.Message}"));
            }
        }

        Console.WriteLine();
    }

    private static void DisplayReport(List<DatabaseChange> changes)
    {
        Console.WriteLine("=== Raport zmian ===");
        Console.WriteLine();

        var groupedChanges = changes.GroupBy(c => c.Type);

        foreach (var group in groupedChanges.OrderBy(g => g.Key))
        {
            Console.WriteLine($"{group.Key}:");
            foreach (var change in group)
            {
                if (string.IsNullOrWhiteSpace(change.Details))
                {
                    Console.WriteLine($"  - {change.ObjectName}");
                }
                else
                {
                    Console.WriteLine($"  - {change.ObjectName}: {change.Details}");
                }
            }
            Console.WriteLine();
        }

        var stats = new
        {
            DomainsCreated = changes.Count(c => c.Type == ChangeType.DomainCreated),
            TablesCreated = changes.Count(c => c.Type == ChangeType.TableCreated),
            ColumnsAdded = changes.Count(c => c.Type == ChangeType.ColumnAdded),
            ProceduresModified = changes.Count(c => c.Type == ChangeType.ProcedureModified),
            ManualReview = changes.Count(c => c.Type == ChangeType.ManualReviewRequired)
        };

        Console.WriteLine("Podsumowanie:");
        Console.WriteLine($"  Domeny utworzone: {stats.DomainsCreated}");
        Console.WriteLine($"  Tabele utworzone: {stats.TablesCreated}");
        Console.WriteLine($"  Kolumny dodane: {stats.ColumnsAdded}");
        Console.WriteLine($"  Procedury zmodyfikowane: {stats.ProceduresModified}");
        Console.WriteLine($"  Wymaga przeglądu manualnego: {stats.ManualReview}");
    }

    private static TableMetadata? ParseTableFromScript(string sql, string tableName)
    {
        var lines = sql.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var columns = new List<ColumnMetadata>();
        var position = 0;

        var inColumns = false;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("("))
            {
                inColumns = true;
                continue;
            }

            if (trimmed.StartsWith(")"))
            {
                break;
            }

            if (inColumns && !string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("--"))
            {
                var columnDef = ParseColumnDefinition(trimmed, position++);
                if (columnDef != null)
                {
                    columns.Add(columnDef);
                }
            }
        }

        if (columns.Count == 0)
        {
            return null;
        }

        return new TableMetadata(tableName, columns);
    }

    private static ColumnMetadata? ParseColumnDefinition(string line, int position)
    {
        line = line.TrimEnd(',').Trim();
        
        var notNullIndex = line.IndexOf("NOT NULL", StringComparison.OrdinalIgnoreCase);
        var defaultIndex = line.IndexOf("DEFAULT", StringComparison.OrdinalIgnoreCase);
        
        var endOfTypeIndex = line.Length;
        if (notNullIndex > 0)
        {
            endOfTypeIndex = Math.Min(endOfTypeIndex, notNullIndex);
        }
        if (defaultIndex > 0)
        {
            endOfTypeIndex = Math.Min(endOfTypeIndex, defaultIndex);
        }

        var columnDefinition = line.Substring(0, endOfTypeIndex).Trim();
        
        var firstSpaceIndex = columnDefinition.IndexOf(' ');
        if (firstSpaceIndex < 0)
        {
            return null;
        }

        var name = columnDefinition.Substring(0, firstSpaceIndex).Trim();
        var dataType = columnDefinition.Substring(firstSpaceIndex + 1).Trim();
        var isNullable = notNullIndex < 0;

        return new ColumnMetadata(
            Position: position,
            Name: name,
            DataType: dataType,
            Length: null,
            Precision: null,
            Scale: null,
            IsNullable: isNullable,
            DefaultValue: null,
            DomainName: null);
    }

    private static string ExtractColumnName(string alterStatement)
    {
        var parts = alterStatement.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var addIndex = Array.FindIndex(parts, p => p.Equals("ADD", StringComparison.OrdinalIgnoreCase));

        if (addIndex >= 0 && addIndex + 1 < parts.Length)
        {
            return parts[addIndex + 1];
        }

        return "UNKNOWN";
    }
}
