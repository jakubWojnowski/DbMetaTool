namespace DbMetaTool.Models.results;

public record BuildResult(
    string ConnectionString,
    int ExecutedCount,
    int DomainScripts,
    int TableScripts,
    int ProcedureScripts);