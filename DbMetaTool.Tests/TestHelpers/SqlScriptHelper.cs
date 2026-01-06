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

    private ScriptFile CreateDomainScript(string domainName, string content)
    {
        var fileName = $"{domainName}.sql";
        
        var filePath = Path.Combine(_baseDirectory, fileName);
        
        File.WriteAllText(filePath, content);
        
        _createdFiles.Add(filePath);
        
        return new ScriptFile(filePath, fileName, ScriptType.Domain);
    }

    private ScriptFile CreateTableScript(string tableName, string content)
    {
        var fileName = $"{tableName}.sql";
        
        var filePath = Path.Combine(_baseDirectory, fileName);
        
        File.WriteAllText(filePath, content);
        
        _createdFiles.Add(filePath);
        
        return new ScriptFile(filePath, fileName, ScriptType.Table);
    }

    private ScriptFile CreateProcedureScript(string procedureName, string content)
    {
        var fileName = $"{procedureName}.sql";
        
        var filePath = Path.Combine(_baseDirectory, fileName);
        
        File.WriteAllText(filePath, content);
        
        _createdFiles.Add(filePath);
        
        return new ScriptFile(filePath, fileName, ScriptType.Procedure);
    }

    public ScriptFile CreateDomainScriptFromTemplate(string domainName, string dataType)
    {
        var sql = SqlTemplates.CreateDomain(domainName, dataType);
        return CreateDomainScript(domainName, sql);
    }

    public ScriptFile CreateTableScriptFromTemplate(string tableName, params string[] columnDefinitions)
    {
        var sql = SqlTemplates.CreateSimpleTable(tableName, columnDefinitions);
        return CreateTableScript(tableName, sql);
    }

    public ScriptFile CreateTableColumnsOnlyScript(string tableName, params string[] columnDefinitions)
    {
        var sql = SqlTemplates.CreateTableColumnsOnly(columnDefinitions);
        return CreateTableScript(tableName, sql);
    }

    public ScriptFile CreateSimpleProcedureScript(string procedureName, string body = "BEGIN END")
    {
        var sql = SqlTemplates.CreateSimpleProcedure(procedureName, body);
        return CreateProcedureScript(procedureName, sql);
    }

    public ScriptFile CreateFirebirdProcedureScript(
        string procedureName,
        string parameters,
        string returns,
        string body)
    {
        var sql = SqlTemplates.CreateFirebirdProcedure(procedureName, parameters, returns, body);
        return CreateProcedureScript(procedureName, sql);
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

