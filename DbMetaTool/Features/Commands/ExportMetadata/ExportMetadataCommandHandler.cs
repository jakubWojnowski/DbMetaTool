using DbMetaTool.Databases;
using DbMetaTool.Services.Export;

namespace DbMetaTool.Features.Commands.ExportMetadata;

public class ExportMetadataCommandHandler(
    IDatabaseStrategyService strategyService,
    IMetadataExportService exportService,
    IExportReportGenerator reportGenerator) : IAsyncHandler<ExportMetadataCommand, ExportMetadataResponse>
{
    public async Task<ExportMetadataResponse> HandleAsync(
        ExportMetadataCommand request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"=== Eksport metadanych z bazy {request.DatabaseType} ===");
            Console.WriteLine();
            Console.WriteLine($"Connection String: {request.ConnectionString}");
            Console.WriteLine($"Katalog wyjściowy: {request.OutputDirectory}");
            Console.WriteLine();

            using var sqlExecutor = strategyService.GetSqlExecutor(request.DatabaseType, request.ConnectionString);

            var result = await exportService.ExportAll(sqlExecutor, request.OutputDirectory, cancellationToken);

            reportGenerator.DisplayReport(result);

            Console.WriteLine("Skrypty zostały wyeksportowane pomyślnie.");

            return new ExportMetadataResponse(Success: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
            return new ExportMetadataResponse(Success: false, ErrorMessage: ex.Message);
        }
    }
}
