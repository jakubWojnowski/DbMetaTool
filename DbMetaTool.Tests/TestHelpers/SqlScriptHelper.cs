using DbMetaTool.Models;

namespace DbMetaTool.Tests.TestHelpers;

public class SqlScriptHelper : IDisposable
{
    private readonly List<string> _createdFiles = [];
    private readonly string _baseDirectory;

    public SqlScriptHelper()
    {
        _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    }

    public ScriptFile CreateDomainScript(string domainName, string content)
    {
        var fileName = $"{domainName}.sql";
        
        var filePath = Path.Combine(_baseDirectory, fileName);
        
        File.WriteAllText(filePath, content);
        
        _createdFiles.Add(filePath);
        
        return new ScriptFile(filePath, fileName, ScriptType.Domain);
    }

    public ScriptFile CreateTableScript(string tableName, string content)
    {
        var fileName = $"{tableName}.sql";
        
        var filePath = Path.Combine(_baseDirectory, fileName);
        
        File.WriteAllText(filePath, content);
        
        _createdFiles.Add(filePath);
        
        return new ScriptFile(filePath, fileName, ScriptType.Table);
    }

    public ScriptFile CreateProcedureScript(string procedureName, string content)
    {
        var fileName = $"{procedureName}.sql";
        
        var filePath = Path.Combine(_baseDirectory, fileName);
        
        File.WriteAllText(filePath, content);
        
        _createdFiles.Add(filePath);
        
        return new ScriptFile(filePath, fileName, ScriptType.Procedure);
    }

    public void Dispose()
    {
        foreach (var filePath in _createdFiles)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        _createdFiles.Clear();
    }
}

