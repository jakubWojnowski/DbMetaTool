using DbMetaTool.Firebird;
using DbMetaTool.Models;
using DbMetaTool.Models.results;
using DbMetaTool.Services.Firebird;
using DbMetaTool.Services.SqlScripts;
using DbMetaTool.Services.Validation;
using DbMetaTool.Utilities;

namespace DbMetaTool.Services.Build;

public static class DatabaseBuildService
{
    public static BuildResult BuildDatabase(string databaseFilePath, string scriptsDirectory)
    {
        CreateEmptyDatabase(databaseFilePath);

        var scripts = LoadScripts(scriptsDirectory);
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

        var connectionString = FirebirdConnectionFactory.BuildConnectionString(databaseFilePath);
        var connectionFactory = new FirebirdConnectionFactory(connectionString);

        using var sqlExecutor = new FirebirdSqlExecutor(connectionFactory);

        ExecuteScripts(sqlExecutor, scripts);
        
        return new BuildResult(
            ExecutedCount: scripts.Count,
            DomainScripts: scripts.Count(s => s.Type == ScriptType.Domain),
            TableScripts: scripts.Count(s => s.Type == ScriptType.Table),
            ProcedureScripts: scripts.Count(s => s.Type == ScriptType.Procedure));
    }

    private static void CreateEmptyDatabase(string databaseFilePath)
    {
        FirebirdDatabaseCreator.CreateDatabase(databaseFilePath);
        
        Console.WriteLine("✓ Utworzono pustą bazę danych");
    }

    private static List<ScriptFile> LoadScripts(string scriptsDirectory)
    {
        return ScriptLoader.LoadScriptsInOrder(scriptsDirectory);
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