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
    public async Task<UpdateDatabaseResponse> HandleAsync(
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
            var existingDomains = await metadataReader.ReadDomainsAsync(sqlExecutor);
            
            var existingTables = await metadataReader.ReadTablesAsync(sqlExecutor);
            
            var existingProcedures = await metadataReader.ReadProceduresAsync(sqlExecutor);

            Console.WriteLine($"✓ Obecny stan: {existingDomains.Count} domen, {existingTables.Count} tabel, {existingProcedures.Count} procedur");
            Console.WriteLine();

            var scripts = scriptLoader.LoadScriptsInOrder(request.ScriptsDirectory);
            
            Console.WriteLine($"Wczytano {scripts.Count} skryptów");
            Console.WriteLine();

            await updateService.ProcessUpdate(
                scripts, 
                existingDomains, 
                existingTables,
                existingProcedures);

            reportGenerator.DisplayReport(updateService.GetChanges());

            Console.WriteLine("Baza danych została zaktualizowana pomyślnie.");

            return new UpdateDatabaseResponse(Success: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
            return new UpdateDatabaseResponse(Success: false, ErrorMessage: ex.Message);
        }
    }
}
