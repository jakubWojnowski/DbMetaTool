namespace DbMetaTool.Firebird;

public class ScriptExecutor(FirebirdConnectionFactory connectionFactory)
{
    public List<ExecutionResult> ExecuteScriptsFromDirectory(string scriptsDirectory)
    {
        var results = new List<ExecutionResult>();

        var domainScripts = GetScriptsFromSubdirectory(scriptsDirectory, "domains");
        var tableScripts = GetScriptsFromSubdirectory(scriptsDirectory, "tables");
        var procedureScripts = GetScriptsFromSubdirectory(scriptsDirectory, "procedures");

        results.AddRange(ExecuteScripts(domainScripts, "DOMAIN"));
        results.AddRange(ExecuteScripts(tableScripts, "TABLE"));
        results.AddRange(ExecuteScripts(procedureScripts, "PROCEDURE"));

        return results;
    }

    public List<ExecutionResult> ExecuteScripts(List<string> scriptPaths, string category)
    {
        var results = new List<ExecutionResult>();

        foreach (var scriptPath in scriptPaths)
        {
            var result = ExecuteScript(scriptPath, category);
            results.Add(result);
        }

        return results;
    }

    public ExecutionResult ExecuteScript(string scriptPath, string category)
    {
        try
        {
            var scriptContent = File.ReadAllText(scriptPath);
            var statements = SqlScriptParser.SplitScriptIntoStatements(scriptContent);

            using var connection = connectionFactory.CreateAndOpenConnection();

            foreach (var statement in statements)
            {
                if (string.IsNullOrWhiteSpace(statement))
                {
                    continue;
                }

                using var command = connection.CreateCommand();
                command.CommandText = statement;
                command.ExecuteNonQuery();
            }

            return new ExecutionResult(
                ScriptPath: scriptPath,
                Category: category,
                Success: true,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            return new ExecutionResult(
                ScriptPath: scriptPath,
                Category: category,
                Success: false,
                ErrorMessage: ex.Message
            );
        }
    }

    private static List<string> GetScriptsFromSubdirectory(string baseDirectory, string subdirectory)
    {
        var path = Path.Combine(baseDirectory, subdirectory);

        if (!Directory.Exists(path))
        {
            return new List<string>();
        }

        return Directory.GetFiles(path, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();
    }
}

public record ExecutionResult(
    string ScriptPath,
    string Category,
    bool Success,
    string? ErrorMessage
);

