using DbMetaTool.Models;
using DbMetaTool.Services.Firebird;
using DbMetaTool.Services.SqlScripts;
using DbMetaTool.Services.Validation;
using DbMetaTool.Utilities;

namespace DbMetaTool.Services.Update;

public class DatabaseUpdateService(ISqlExecutor mainExecutor)
{
    private readonly List<DatabaseChange> _changes = [];
    private List<DomainMetadata> _existingDomains = [];
    private readonly List<string> _allStatements = [];

    public List<DatabaseChange> GetChanges() => _changes;

    public void ProcessUpdate(
        List<ScriptFile> scripts,
        List<DomainMetadata> existingDomains,
        List<TableMetadata> existingTables,
        List<ProcedureMetadata> existingProcedures)
    {
        _existingDomains = existingDomains;
        
        ProcessDomains(scripts, existingDomains);
        
        ProcessTables(scripts, existingTables);
        
        ProcessProcedures(scripts, existingProcedures);
        
        if (_allStatements.Count > 0)
        {
            mainExecutor.ExecuteBatch(_allStatements, ProcedureBlrValidator.ValidateProcedureIntegrity);
        }
    }

    private void ProcessDomains(
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
                TryCreateDomain(script, domainName);
            }
            else
            {
                Console.WriteLine($"  Domena {domainName} już istnieje - pomijam");
            }
        }

        Console.WriteLine();
    }

    private void ProcessTables(
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
                TryCreateTable(script, tableName);
            }
            else
            {
                var sql = ScriptLoader.ReadScriptContent(script);
                if (HasCreateStatement(sql))
                {
                    Console.WriteLine($"  Tabela {tableName} już istnieje - pomijam skrypt CREATE");
                }
                else
                {
                    Console.WriteLine($"  Tabela {tableName} istnieje - sprawdzam kolumny...");
                    
                    ProcessTableColumns(script, existingTable);
                }
            }
        }

        Console.WriteLine();
    }

    private void ProcessProcedures(
        List<ScriptFile> scripts,
        List<ProcedureMetadata> existingProcedures)
    {
        Console.WriteLine("=== Przetwarzanie procedur ===");

        var procedureScripts = scripts.Where(s => s.Type == ScriptType.Procedure).ToList();

        foreach (var script in procedureScripts)
        {
            var procedureName = Path.GetFileNameWithoutExtension(script.FileName);
            
            var existingProcedure = existingProcedures.FirstOrDefault(p =>
                p.Name.Equals(procedureName, StringComparison.OrdinalIgnoreCase));

            if (existingProcedure != null)
            {
                var sql = ScriptLoader.ReadScriptContent(script);
                if (HasCreateStatement(sql))
                {
                    Console.WriteLine($"  Procedura {procedureName} już istnieje - pomijam skrypt CREATE");
                    continue;
                }
            }
            
            CollectProcedureStatements(script, procedureName);
        }

        Console.WriteLine();
    }

    private void TryCreateDomain(ScriptFile script, string domainName)
    {
        Console.Write($"  Tworzenie domeny {domainName}... ");
        
        var sql = ScriptLoader.ReadScriptContent(script);
        
        var statements = SqlScriptParser.ParseScript(sql)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        _allStatements.AddRange(statements);

        _changes.Add(new DatabaseChange(
            ChangeType.DomainCreated,
            domainName,
            null));
        Console.WriteLine("✓");
    }

    private void TryCreateTable(ScriptFile script, string tableName)
    {
        Console.Write($"  Tworzenie tabeli {tableName}... ");
        
        var sql = ScriptLoader.ReadScriptContent(script);
        
        var statements = SqlScriptParser.ParseScript(sql)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        _allStatements.AddRange(statements);

        _changes.Add(new DatabaseChange(
            ChangeType.TableCreated,
            tableName,
            null));
        
        Console.WriteLine("✓");
    }

    private void ProcessTableColumns(ScriptFile script, TableMetadata existingTable)
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
            desiredTable,
            _existingDomains);

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
                TryAddColumn(statement, existingTable.Name);
            }
        }
    }

    private void TryAddColumn(string statement, string tableName)
    {
        Console.Write($"    Dodawanie kolumny... ");
        
        _allStatements.Add(statement);

        var columnName = ScriptDefinitionParser.ExtractColumnName(statement);
        
        _changes.Add(new DatabaseChange(
            ChangeType.ColumnAdded,
            $"{tableName}.{columnName}",
            statement));
        Console.WriteLine("✓");
    }

    private void CollectProcedureStatements(ScriptFile script, string procedureName)
    {
        Console.Write($"  Procedura {procedureName}... ");
        
        var callingProcedures = ProcedureDependencyValidator.GetCallingProcedures(mainExecutor, procedureName);
        
        if (callingProcedures.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"    ⚠ Procedura jest wywoływana przez: {string.Join(", ", callingProcedures)}");
        }
        
        var sql = ScriptLoader.ReadScriptContent(script);
        var statements = SqlScriptParser.ParseScript(sql)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        
        _allStatements.AddRange(statements);
   
        
        _changes.Add(new DatabaseChange(
            ChangeType.ProcedureModified,
            procedureName,
            "Wykonano skrypt"));
        Console.WriteLine("✓");
    }

    private static bool HasCreateStatement(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return false;
        }

        var upperSql = sql.ToUpperInvariant();
        
        return upperSql.Contains("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase) ||
               upperSql.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase) ||
               upperSql.Contains("CREATE DOMAIN", StringComparison.OrdinalIgnoreCase);
    }

}

