using DbMetaTool.Firebird;
using DbMetaTool.Services.Firebird;
using DbMetaTool.Services.Metadata;
using DbMetaTool.Services.SqlScripts;
using DbMetaTool.Services.Update;

namespace DbMetaTool.Features.Commands.UpdateDatabase;

public class UpdateDatabaseCommandHandler(
    IMetadataReader metadataReader,
    IScriptLoader scriptLoader,
    IUpdateReportGenerator reportGenerator) : IAsyncHandler<UpdateDatabaseCommand, UpdateDatabaseResponse>
{
    public Task<UpdateDatabaseResponse> HandleAsync(
        UpdateDatabaseCommand request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("=== Aktualizacja bazy danych Firebird ===");
            Console.WriteLine();
            Console.WriteLine($"Connection String: {request.ConnectionString}");
            Console.WriteLine($"Katalog skryptów: {request.ScriptsDirectory}");
            Console.WriteLine();

            var connectionFactory = new FirebirdConnectionFactory(request.ConnectionString);
            
            using var sqlExecutor = new FirebirdSqlExecutor(connectionFactory);

            var updateService = new DatabaseUpdateService(sqlExecutor, scriptLoader);

            Console.WriteLine("Pobieranie aktualnego stanu bazy...");
            var existingDomains = metadataReader.ReadDomains(sqlExecutor);
            
            var existingTables = metadataReader.ReadTables(sqlExecutor);
            
            var existingProcedures = metadataReader.ReadProcedures(sqlExecutor);

            Console.WriteLine($"✓ Obecny stan: {existingDomains.Count} domen, {existingTables.Count} tabel, {existingProcedures.Count} procedur");
            Console.WriteLine();

            var scripts = scriptLoader.LoadScriptsInOrder(request.ScriptsDirectory);
            
            Console.WriteLine($"Wczytano {scripts.Count} skryptów");
            Console.WriteLine();

            updateService.ProcessUpdate(
                scripts, 
                existingDomains, 
                existingTables,
                existingProcedures);

            reportGenerator.DisplayReport(updateService.GetChanges());

            Console.WriteLine("Baza danych została zaktualizowana pomyślnie.");

            return Task.FromResult(new UpdateDatabaseResponse(Success: true));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
            return Task.FromResult(new UpdateDatabaseResponse(Success: false, ErrorMessage: ex.Message));
        }
    }
}
