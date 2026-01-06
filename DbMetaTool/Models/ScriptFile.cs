namespace DbMetaTool.Models;

public enum ScriptType
{
    Domain,
    Table,
    Procedure
}

public record ScriptFile(
    string FullPath,
    string FileName,
    ScriptType Type
);

