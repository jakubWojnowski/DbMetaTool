using DbMetaTool.Exceptions;
using DbMetaTool.Utilities;

namespace DbMetaTool.Services;

public class SqlScriptParser
{
    public class ParsedScript
    {
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public ScriptType Type { get; set; }
        public string ObjectName { get; set; } = string.Empty;
    }

    public enum ScriptType
    {
        Unknown,
        Domain,
        Table,
        Procedure
    }

    public List<ParsedScript> ParseDirectory(string scriptsDirectory)
    {
        if (!Directory.Exists(scriptsDirectory))
        {
            throw new DirectoryNotFoundException($"Katalog skryptów nie istnieje: {scriptsDirectory}");
        }

        var sqlFiles = FileSystemHelper.GetSqlFiles(scriptsDirectory);
        var parsedScripts = new List<ParsedScript>();

        foreach (var filePath in sqlFiles)
        {
            try
            {
                var content = FileSystemHelper.ReadFromFile(filePath);
                var parsed = ParseScript(filePath, content);
                parsedScripts.Add(parsed);
            }
            catch (Exception ex)
            {
                throw new ScriptParsingException(
                    $"Błąd parsowania pliku: {ex.Message}", 
                    filePath, 
                    ex);
            }
        }

        // Sortuj skrypty według kolejności: domeny -> tabele -> procedury
        return SortScripts(parsedScripts);
    }

    private ParsedScript ParseScript(string filePath, string content)
    {
        var script = new ParsedScript
        {
            FilePath = filePath,
            Content = content.Trim()
        };

        // Określ typ skryptu na podstawie zawartości
        var upperContent = content.ToUpperInvariant();
        
        if (upperContent.Contains("CREATE DOMAIN") || upperContent.Contains("CREATE OR ALTER DOMAIN"))
        {
            script.Type = ScriptType.Domain;
            script.ObjectName = ExtractObjectName(content, "DOMAIN");
        }
        else if (upperContent.Contains("CREATE TABLE") || upperContent.Contains("CREATE OR ALTER TABLE"))
        {
            script.Type = ScriptType.Table;
            script.ObjectName = ExtractObjectName(content, "TABLE");
        }
        else if (upperContent.Contains("CREATE PROCEDURE") || 
                 upperContent.Contains("CREATE OR ALTER PROCEDURE") ||
                 upperContent.Contains("RECREATE PROCEDURE"))
        {
            script.Type = ScriptType.Procedure;
            script.ObjectName = ExtractObjectName(content, "PROCEDURE");
        }
        else
        {
            script.Type = ScriptType.Unknown;
            // Spróbuj wywnioskować z nazwy pliku lub katalogu
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var directory = Path.GetDirectoryName(filePath) ?? "";
            
            if (directory.Contains("domain", StringComparison.OrdinalIgnoreCase))
                script.Type = ScriptType.Domain;
            else if (directory.Contains("table", StringComparison.OrdinalIgnoreCase))
                script.Type = ScriptType.Table;
            else if (directory.Contains("procedure", StringComparison.OrdinalIgnoreCase))
                script.Type = ScriptType.Procedure;
        }

        return script;
    }

    private string ExtractObjectName(string content, string keyword)
    {
        var upperContent = content.ToUpperInvariant();
        var keywordIndex = upperContent.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        
        if (keywordIndex == -1)
            return string.Empty;

        var startIndex = keywordIndex + keyword.Length;
        
        // Pomiń białe znaki
        while (startIndex < content.Length && char.IsWhiteSpace(content[startIndex]))
        {
            startIndex++;
        }

        // Znajdź koniec nazwy (biały znak, nawias, średnik)
        var endIndex = startIndex;
        var inQuotes = false;
        var quoteChar = '\0';

        while (endIndex < content.Length)
        {
            var ch = content[endIndex];
            
            if ((ch == '"' || ch == '\'') && (endIndex == startIndex || content[endIndex - 1] != '\\'))
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = ch;
                }
                else if (ch == quoteChar)
                {
                    inQuotes = false;
                }
            }
            else if (!inQuotes && (char.IsWhiteSpace(ch) || ch == '(' || ch == ';'))
            {
                break;
            }
            
            endIndex++;
        }

        var name = content.Substring(startIndex, endIndex - startIndex).Trim();
        
        // Usuń cudzysłowy jeśli istnieją
        if (name.StartsWith('"') && name.EndsWith('"'))
            name = name.Substring(1, name.Length - 2);
        else if (name.StartsWith('\'') && name.EndsWith('\''))
            name = name.Substring(1, name.Length - 2);

        return name;
    }

    private List<ParsedScript> SortScripts(List<ParsedScript> scripts)
    {
        var typeOrder = new Dictionary<ScriptType, int>
        {
            { ScriptType.Domain, 1 },
            { ScriptType.Table, 2 },
            { ScriptType.Procedure, 3 },
            { ScriptType.Unknown, 99 }
        };

        return scripts
            .OrderBy(s => typeOrder.GetValueOrDefault(s.Type, 99))
            .ThenBy(s => s.ObjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}