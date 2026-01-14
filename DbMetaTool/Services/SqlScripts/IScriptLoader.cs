using DbMetaTool.Models;

namespace DbMetaTool.Services.SqlScripts;

public interface IScriptLoader
{
    List<ScriptFile> LoadScriptsInOrder(string scriptsDirectory);
    
    string ReadScriptContent(ScriptFile script);
}
