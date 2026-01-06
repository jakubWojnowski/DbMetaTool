using DbMetaTool.Firebird;
using DbMetaTool.Services;
using DbMetaTool.Services.Firebird;
using DbMetaTool.Services.Metadata;
using DbMetaTool.Services.SqlScripts;
using DbMetaTool.Services.Update;

namespace DbMetaTool.Commands.UpdateDatabase;

public static class UpdateDatabaseCommandHandler
{
    public static void Handle(UpdateDatabaseCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        Console.WriteLine("=== Aktualizacja bazy danych Firebird ===");
        Console.WriteLine();
        Console.WriteLine($"Connection String: {command.ConnectionString}");
        Console.WriteLine($"Katalog skryptów: {command.ScriptsDirectory}");
        Console.WriteLine();

        var connectionFactory = new FirebirdConnectionFactory(command.ConnectionString);
        
        using var sqlExecutor = new FirebirdSqlExecutor(connectionFactory);

        Console.WriteLine("Pobieranie aktualnego stanu bazy...");
        var existingDomains = FirebirdMetadataReader.ReadDomains(sqlExecutor);
        
        var existingTables = FirebirdMetadataReader.ReadTables(sqlExecutor);
        
        var existingProcedures = FirebirdMetadataReader.ReadProcedures(sqlExecutor);

        Console.WriteLine($"✓ Obecny stan: {existingDomains.Count} domen, {existingTables.Count} tabel, {existingProcedures.Count} procedur");
        Console.WriteLine();

        var scripts = ScriptLoader.LoadScriptsInOrder(command.ScriptsDirectory);
        
        Console.WriteLine($"Wczytano {scripts.Count} skryptów");
        Console.WriteLine();

        var updateService = new DatabaseUpdateService(sqlExecutor);
        
        updateService.ProcessUpdate(
            scripts, 
            existingDomains, 
            existingTables,
            existingProcedures);

        UpdateReportGenerator.DisplayReport(updateService.GetChanges());
    }
}
