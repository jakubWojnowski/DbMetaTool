namespace DbMetaTool.Utilities;

public static class SqlScriptParser
{
    public static List<string> ParseScript(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            return [];

        var statements = new List<string>();
        
        var lines = script.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        
        var currentTerminator = ";";
        
        var currentStatement = new List<string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            if (trimmedLine.StartsWith("--"))
                continue;

            if (trimmedLine.StartsWith("SET TERM", StringComparison.InvariantCultureIgnoreCase))
            {
                if (currentStatement.Count > 0)
                {
                    var statement = string.Join(Environment.NewLine, currentStatement);
                    
                    statements.Add(statement);
                    
                    currentStatement.Clear();
                }

                var parts = trimmedLine.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var newTerminator = parts[2];
                    
                    newTerminator = newTerminator.TrimEnd(currentTerminator.ToCharArray());
                    
                    newTerminator = newTerminator.TrimEnd(';');
                    
                    if (!string.IsNullOrWhiteSpace(newTerminator))
                    {
                        currentTerminator = newTerminator;
                    }
                    else
                    {
                        currentTerminator = ";";
                    }
                }
                continue;
            }

            if (trimmedLine.EndsWith(currentTerminator))
            {
                var lineWithoutTerminator = trimmedLine[..^currentTerminator.Length].Trim();
                
                if (!string.IsNullOrWhiteSpace(lineWithoutTerminator))
                {
                    currentStatement.Add(lineWithoutTerminator);
                }

                if (currentStatement.Count > 0)
                {
                    var statement = string.Join(Environment.NewLine, currentStatement);
                    
                    statements.Add(statement);
                    
                    currentStatement.Clear();
                }
            }
            else
            {
                currentStatement.Add(line);
            }
        }

        if (currentStatement.Count > 0)
        {
            var statement = string.Join(Environment.NewLine, currentStatement);
            
            statements.Add(statement);
        }

        return statements;
    }
}

