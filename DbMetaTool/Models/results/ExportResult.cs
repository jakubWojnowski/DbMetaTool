namespace DbMetaTool.Models.results;

public record ExportResult(
    string OutputDirectory,
    int DomainsCount,
    int TablesCount,
    int ProceduresCount)
{
    public int TotalFiles => DomainsCount + TablesCount + ProceduresCount;
}