using DbMetaTool.Models.results;

namespace DbMetaTool.Services.Export;

public interface IExportReportGenerator
{
    void DisplayReport(ExportResult result);
}
