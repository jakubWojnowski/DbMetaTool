using DbMetaTool.Databases;
using DbMetaTool.Models.results;

namespace DbMetaTool.Services.Export;

public interface IMetadataExportService
{
    Task<ExportResult> ExportAll(ISqlExecutor executor, string outputDirectory, CancellationToken cancellationToken = default);
}
