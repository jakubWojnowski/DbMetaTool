namespace DbMetaTool.Models.results;

public record BuildResult(
    int ExecutedCount,
    int DomainScripts,
    int TableScripts,
    int ProcedureScripts);