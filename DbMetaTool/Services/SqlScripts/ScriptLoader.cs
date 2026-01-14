using DbMetaTool.Models;

namespace DbMetaTool.Services.SqlScripts;

public class ScriptLoader : IScriptLoader
{
    public List<ScriptFile> LoadScriptsInOrder(string scriptsDirectory)
    {
        if (string.IsNullOrWhiteSpace(scriptsDirectory))
            throw new ArgumentException("Scripts directory cannot be empty", nameof(scriptsDirectory));

        if (!Directory.Exists(scriptsDirectory))
            throw new DirectoryNotFoundException($"Scripts directory not found: {scriptsDirectory}");

        var scripts = new List<ScriptFile>();

        LoadScriptsOfType(scripts, scriptsDirectory, "domains", ScriptType.Domain);
        
        LoadScriptsOfType(scripts, scriptsDirectory, "tables", ScriptType.Table);
        
        LoadScriptsOfType(scripts, scriptsDirectory, "procedures", ScriptType.Procedure);

        return scripts;
    }

    private static void LoadScriptsOfType(
        List<ScriptFile> scripts, 
        string baseDirectory, 
        string subdirectory, 
        ScriptType type)
    {
        var path = Path.Combine(baseDirectory, subdirectory);

        if (!Directory.Exists(path))
            return;

        var sqlFiles = Directory.GetFiles(path, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f)
            .ToList();

        foreach (var file in sqlFiles)
        {
            scripts.Add(new ScriptFile(
                FullPath: file,
                FileName: Path.GetFileName(file),
                Type: type
            ));
        }
    }

    public string ReadScriptContent(ScriptFile script)
    {
        if (script == null)
            throw new ArgumentNullException(nameof(script));

        if (!File.Exists(script.FullPath))
            throw new FileNotFoundException($"Script file not found: {script.FullPath}");

        return File.ReadAllText(script.FullPath);
    }
}

