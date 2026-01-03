using DbMetaTool.Models;
using DbMetaTool.Services.Firebird;
using DbMetaTool.Services.Metadata;
using DbMetaTool.Services.SqlScripts;
using DbMetaTool.Utilities;

namespace DbMetaTool.Services.Update;

public class DatabaseUpdateService(ISqlExecutor mainExecutor)
{
    private readonly List<DatabaseChange> _changes = [];

    public List<DatabaseChange> GetChanges() => _changes;

    public void ProcessUpdate(
        List<ScriptFile> scripts,
        List<DomainMetadata> existingDomains,
        List<TableMetadata> existingTables)
    {
        mainExecutor.ExecuteInTransaction(executor =>
        {
            ProcessDomains(executor, scripts, existingDomains);
            
            ProcessTables(executor, scripts, existingTables);
            
            ProcessProcedures(executor, scripts);
        });
    }

    private void ProcessDomains(
        ISqlExecutor executor,
        List<ScriptFile> scripts,
        List<DomainMetadata> existingDomains)
    {
        Console.WriteLine("=== Przetwarzanie domen ===");

        var domainScripts = scripts.Where(s => s.Type == ScriptType.Domain).ToList();

        foreach (var script in domainScripts)
        {
            var domainName = Path.GetFileNameWithoutExtension(script.FileName);
            
            var exists = existingDomains.Any(d => 
                d.Name.Equals(domainName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                TryCreateDomain(executor, script, domainName);
            }
            else
            {
                Console.WriteLine($"  Domena {domainName} już istnieje - pomijam");
            }
        }

        Console.WriteLine();
    }

    private void ProcessTables(
        ISqlExecutor executor,
        List<ScriptFile> scripts,
        List<TableMetadata> existingTables)
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
                TryCreateTable(executor, script, tableName);
            }
            else
            {
                Console.WriteLine($"  Tabela {tableName} istnieje - sprawdzam kolumny...");
                
                ProcessTableColumns(executor, script, existingTable);
            }
        }

        Console.WriteLine();
    }

    private void ProcessProcedures(
        ISqlExecutor executor,
        List<ScriptFile> scripts)
    {
        Console.WriteLine("=== Przetwarzanie procedur ===");

        var procedureScripts = scripts.Where(s => s.Type == ScriptType.Procedure).ToList();

        foreach (var script in procedureScripts)
        {
            var procedureName = Path.GetFileNameWithoutExtension(script.FileName);
            
            TryExecuteProcedureScript(executor, script, procedureName);
        }

        Console.WriteLine();
    }

    private void TryCreateDomain(ISqlExecutor executor, ScriptFile script, string domainName)
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

            _changes.Add(new DatabaseChange(
                ChangeType.DomainCreated,
                domainName,
                null));
            Console.WriteLine("✓");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd: {ex.Message}");
            
            _changes.Add(new DatabaseChange(
                ChangeType.ManualReviewRequired,
                domainName,
                $"Błąd tworzenia: {ex.Message}"));
            throw;
        }
    }

    private void TryCreateTable(ISqlExecutor executor, ScriptFile script, string tableName)
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

            _changes.Add(new DatabaseChange(
                ChangeType.TableCreated,
                tableName,
                null));
            
            Console.WriteLine("✓");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd: {ex.Message}");
            
            _changes.Add(new DatabaseChange(
                ChangeType.ManualReviewRequired,
                tableName,
                $"Błąd tworzenia: {ex.Message}"));
            
            throw;
        }
    }

    private void ProcessTableColumns(ISqlExecutor executor, ScriptFile script, TableMetadata existingTable)
    {
        var sql = ScriptLoader.ReadScriptContent(script);
        
        var desiredTable = ScriptDefinitionParser.ParseTableFromScript(sql, existingTable.Name);

        if (desiredTable == null)
        {
            Console.WriteLine($"    ⚠ Nie można sparsować skryptu tabeli");
            return;
        }

        var alterStatements = DatabaseSchemaComparer.GenerateAlterStatements(
            existingTable,
            desiredTable);

        foreach (var statement in alterStatements)
        {
            if (statement.StartsWith("--"))
            {
                Console.WriteLine($"    ⚠ {statement}");
                
                _changes.Add(new DatabaseChange(
                    ChangeType.ManualReviewRequired,
                    existingTable.Name,
                    statement));
            }
            else
            {
                TryAddColumn(executor, statement, existingTable.Name);
            }
        }
    }

    private void TryAddColumn(ISqlExecutor executor, string statement, string tableName)
    {
        try
        {
            Console.Write($"    Dodawanie kolumny... ");
            
            executor.ExecuteNonQuery(statement);

            var columnName = ScriptDefinitionParser.ExtractColumnName(statement);
            
            _changes.Add(new DatabaseChange(
                ChangeType.ColumnAdded,
                $"{tableName}.{columnName}",
                statement));
            Console.WriteLine("✓");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd: {ex.Message}");
            
            _changes.Add(new DatabaseChange(
                ChangeType.ManualReviewRequired,
                tableName,
                $"Błąd ALTER: {ex.Message}"));
            
            throw;
        }
    }

    private void TryExecuteProcedureScript(ISqlExecutor executor, ScriptFile script, string procedureName)
    {
        try
        {
            Console.Write($"  Procedura {procedureName}... ");
            
            var callingProcedures = ProcedureDependencyValidator.GetCallingProcedures(executor, procedureName);
            
            if (callingProcedures.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"    ⚠ Procedura jest wywoływana przez: {string.Join(", ", callingProcedures)}");
            }
            
            var sql = ScriptLoader.ReadScriptContent(script);
            
            var statements = SqlScriptParser.ParseScript(sql);

            foreach (var statement in statements)
            {
                if (!string.IsNullOrWhiteSpace(statement))
                {
                    executor.ExecuteNonQuery(statement);
                }
            }

            ValidateProcedureIntegrity(executor, procedureName, callingProcedures);

            _changes.Add(new DatabaseChange(
                ChangeType.ProcedureModified,
                procedureName,
                "Wykonano skrypt"));
            Console.WriteLine("✓");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Błąd: {ex.Message}");
            
            _changes.Add(new DatabaseChange(
                ChangeType.ManualReviewRequired,
                procedureName,
                $"Błąd: {ex.Message}"));
            
            throw;
        }
    }

    private void ValidateProcedureIntegrity(
        ISqlExecutor executor,
        string modifiedProcedureName,
        List<string> callingProcedures)
    {
        if (callingProcedures.Count == 0)
        {
            Console.WriteLine($"    Brak procedur wywołujących {modifiedProcedureName} - pomijam walidację");
            return;
        }

        Console.WriteLine($"    Sprawdzanie integralności BLR i rekompilacja {callingProcedures.Count} procedur wywołujących...");
        
        foreach (var procedureName in callingProcedures)
        {
            try
            {
                Console.Write($"      Próba rekompilacji {procedureName}... ");
                ProcedureDependencyValidator.RecompileProcedure(executor, procedureName);
                Console.WriteLine("✓");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Procedura {procedureName} nie może być automatycznie rekompilowana po zmianie {modifiedProcedureName}: {ex.Message}";
                
                Console.WriteLine($"✗ Błąd: {ex.Message}");
                
                _changes.Add(new DatabaseChange(
                    ChangeType.ManualReviewRequired,
                    procedureName,
                    errorMessage));
                
                throw new InvalidOperationException(
                    $"Zmiana sygnatury procedury {modifiedProcedureName} spowodowała niespójność. " +
                    $"Procedura {procedureName} wymaga ręcznej aktualizacji (prawdopodobnie wywołuje {modifiedProcedureName} z nieprawidłową liczbą parametrów). " +
                    $"Transakcja została wycofana.",
                    ex);
            }
        }
        
        var invalidProcedures = ProcedureDependencyValidator.GetInvalidProcedures(executor);
        
        if (invalidProcedures.Count > 0)
        {
            var stillInvalid = invalidProcedures
                .Where(p => callingProcedures.Contains(p, StringComparer.OrdinalIgnoreCase) || 
                           p.Equals(modifiedProcedureName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            if (stillInvalid.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Po rekompilacji nadal istnieją nieprawidłowe procedury: {string.Join(", ", stillInvalid)}");
            }
        }
        
        Console.WriteLine($"    Wszystkie procedury wywołujące zostały pomyślnie zrekompilowane");
    }
}

