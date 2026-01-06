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
            var statements = SplitScriptIntoStatements(scriptContent);

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

    private static List<string> SplitScriptIntoStatements(string scriptContent)
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
                if (!statement.EndsWith(";") && terminator == ";")
                {
                    statement += ";";
                }
                statements.Add(statement);
            }
        }

        return statements;
    }
}

public record ExecutionResult(
    string ScriptPath,
    string Category,
    bool Success,
    string? ErrorMessage
);

