using DbMetaTool.Models.results;
using DbMetaTool.Services.Firebird;

namespace DbMetaTool.Services.Export;

public interface IMetadataExportService
{
    ExportResult ExportAll(ISqlExecutor executor, string outputDirectory);
}
