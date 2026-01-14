using DbMetaTool.Firebird;
using DbMetaTool.Services.Export;
using DbMetaTool.Services.Firebird;

namespace DbMetaTool.Features.Commands.ExportMetadata;

public class ExportMetadataCommandHandler(
    IMetadataExportService exportService,
    IExportReportGenerator reportGenerator) : IAsyncHandler<ExportMetadataCommand, ExportMetadataResponse>
{
    public async Task<ExportMetadataResponse> HandleAsync(
        ExportMetadataCommand request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("=== Eksport metadanych z bazy Firebird ===");
            Console.WriteLine();
            Console.WriteLine($"Connection String: {request.ConnectionString}");
            Console.WriteLine($"Katalog wyjściowy: {request.OutputDirectory}");
            Console.WriteLine();

            var connectionFactory = new FirebirdConnectionFactory(request.ConnectionString);
            
            using var sqlExecutor = new FirebirdSqlExecutor(connectionFactory);

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
