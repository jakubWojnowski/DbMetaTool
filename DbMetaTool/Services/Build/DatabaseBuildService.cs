using DbMetaTool.Firebird;
using DbMetaTool.Models;
using DbMetaTool.Models.results;
using DbMetaTool.Services.Firebird;
using DbMetaTool.Services.SqlScripts;
using DbMetaTool.Services.Validation;
using DbMetaTool.Utilities;

namespace DbMetaTool.Services.Build;

public class DatabaseBuildService(
    IDatabaseCreator databaseCreator,
    IScriptLoader scriptLoader) : IDatabaseBuildService
{
    public async Task<BuildResult> BuildDatabaseAsync(string databaseFilePath, string scriptsDirectory)
    {
        CreateEmptyDatabase(databaseFilePath);

        var scripts = scriptLoader.LoadScriptsInOrder(scriptsDirectory);
        
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

        await ExecuteScriptsAsync(sqlExecutor, scripts);
        
        return new BuildResult(
            ExecutedCount: scripts.Count,
            DomainScripts: scripts.Count(s => s.Type == ScriptType.Domain),
            TableScripts: scripts.Count(s => s.Type == ScriptType.Table),
            ProcedureScripts: scripts.Count(s => s.Type == ScriptType.Procedure));
    }

    private void CreateEmptyDatabase(string databaseFilePath)
    {
        databaseCreator.CreateDatabase(databaseFilePath);
        
        Console.WriteLine("✓ Utworzono pustą bazę danych");
    }

    private static void DisplayScriptsSummary(List<ScriptFile> scripts)
    {
        Console.WriteLine($"Znaleziono {scripts.Count} skryptów do wykonania:");
        Console.WriteLine($"  - Domeny: {scripts.Count(s => s.Type == ScriptType.Domain)}");
        Console.WriteLine($"  - Tabele: {scripts.Count(s => s.Type == ScriptType.Table)}");
        Console.WriteLine($"  - Procedury: {scripts.Count(s => s.Type == ScriptType.Procedure)}");
        Console.WriteLine();
    }

    private async Task ExecuteScriptsAsync(
        ISqlExecutor sqlExecutor,
        List<ScriptFile> scripts)
    {
        var allStatements = new List<string>();

        foreach (var script in scripts)
        {
            Console.Write($"Wykonywanie: {script.Type}/{script.FileName}... ");

            var sql = scriptLoader.ReadScriptContent(script);
            
            var statements = SqlScriptParser.ParseScript(sql)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            
            allStatements.AddRange(statements);

            Console.WriteLine("✓");
        }

        if (allStatements.Count > 0)
        {
            await sqlExecutor.ExecuteBatchAsync(allStatements, ProcedureBlrValidator.ValidateProcedureIntegrityAsync);
        }
    }
}