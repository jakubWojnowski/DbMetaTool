using DbMetaTool.Models;
using DbMetaTool.Models.results;
using DbMetaTool.Services.Firebird;
using DbMetaTool.Services.SqlScripts;
using DbMetaTool.Services.Validation;
using DbMetaTool.Utilities;

namespace DbMetaTool.Tests.TestHelpers;

public class DatabaseBuildServiceTestWrapper
{
    private readonly ISqlExecutor _sqlExecutor;
    private readonly Action<string> _databaseCreatorStub;

    public DatabaseBuildServiceTestWrapper(
        ISqlExecutor sqlExecutor,
        Action<string> databaseCreatorStub)
    {
        _sqlExecutor = sqlExecutor ?? throw new ArgumentNullException(nameof(sqlExecutor));
        _databaseCreatorStub = databaseCreatorStub ?? throw new ArgumentNullException(nameof(databaseCreatorStub));
    }

    public BuildResult BuildDatabase(string databaseFilePath, string scriptsDirectory)
    {
        _databaseCreatorStub(databaseFilePath);
        Console.WriteLine("✓ Utworzono pustą bazę danych");

        var scripts = ScriptLoader.LoadScriptsInOrder(scriptsDirectory);
        
        if (scripts.Count == 0)
        {
            Console.WriteLine("⚠ Nie znaleziono żadnych skryptów do wykonania");
            return new BuildResult(
                ExecutedCount: 0,
                DomainScripts: 0,
                TableScripts: 0,
                ProcedureScripts: 0);
        }

        DisplayScriptsSummary(scripts);
        ExecuteScripts(_sqlExecutor, scripts);
        
        return new BuildResult(
            ExecutedCount: scripts.Count,
            DomainScripts: scripts.Count(s => s.Type == ScriptType.Domain),
            TableScripts: scripts.Count(s => s.Type == ScriptType.Table),
            ProcedureScripts: scripts.Count(s => s.Type == ScriptType.Procedure));
    }

    private static void DisplayScriptsSummary(List<ScriptFile> scripts)
    {
        Console.WriteLine($"Znaleziono {scripts.Count} skryptów do wykonania:");
        Console.WriteLine($"  - Domeny: {scripts.Count(s => s.Type == ScriptType.Domain)}");
        Console.WriteLine($"  - Tabele: {scripts.Count(s => s.Type == ScriptType.Table)}");
        Console.WriteLine($"  - Procedury: {scripts.Count(s => s.Type == ScriptType.Procedure)}");
        Console.WriteLine();
    }

    private static void ExecuteScripts(
        ISqlExecutor sqlExecutor,
        List<ScriptFile> scripts)
    {
        var allStatements = new List<string>();

        foreach (var script in scripts)
        {
            Console.Write($"Wykonywanie: {script.Type}/{script.FileName}... ");

            var sql = ScriptLoader.ReadScriptContent(script);
            
            var statements = SqlScriptParser.ParseScript(sql)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            
            allStatements.AddRange(statements);

            Console.WriteLine("✓");
        }

        if (allStatements.Count > 0)
        {
            sqlExecutor.ExecuteBatch(allStatements, ProcedureBlrValidator.ValidateProcedureIntegrity);
        }
    }
}

