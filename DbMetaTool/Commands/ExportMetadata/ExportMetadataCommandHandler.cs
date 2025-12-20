using DbMetaTool.Firebird;
using DbMetaTool.Services;

namespace DbMetaTool.Commands.ExportMetadata;

public static class ExportMetadataCommandHandler
{
    public static void Handle(ExportMetadataCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        Console.WriteLine("=== Eksport metadanych z bazy Firebird ===");
        Console.WriteLine();
        Console.WriteLine($"Connection String: {command.ConnectionString}");
        Console.WriteLine($"Katalog wyjściowy: {command.OutputDirectory}");
        Console.WriteLine();

        var absoluteOutputDirectory = Path.GetFullPath(command.OutputDirectory);
        
        if (!Directory.Exists(absoluteOutputDirectory))
        {
            Directory.CreateDirectory(absoluteOutputDirectory);
        }

        var connectionFactory = new FirebirdConnectionFactory(command.ConnectionString);
        using var sqlExecutor = new FirebirdSqlExecutor(connectionFactory);

        Console.WriteLine("Pobieranie metadanych...");

        var domains = FirebirdMetadataReader.ReadDomains(sqlExecutor);
        Console.WriteLine($"✓ Znaleziono {domains.Count} domen");

        var tables = FirebirdMetadataReader.ReadTables(sqlExecutor);
        Console.WriteLine($"✓ Znaleziono {tables.Count} tabel");

        var procedures = FirebirdMetadataReader.ReadProcedures(sqlExecutor);
        Console.WriteLine($"✓ Znaleziono {procedures.Count} procedur");

        Console.WriteLine();
        Console.WriteLine("Generowanie skryptów SQL...");

        var domainsDir = Path.Combine(absoluteOutputDirectory, "domains");
        var tablesDir = Path.Combine(absoluteOutputDirectory, "tables");
        var proceduresDir = Path.Combine(absoluteOutputDirectory, "procedures");

        Directory.CreateDirectory(domainsDir);
        Directory.CreateDirectory(tablesDir);
        Directory.CreateDirectory(proceduresDir);

        foreach (var domain in domains)
        {
            var script = SqlScriptGenerator.GenerateDomainScript(domain);
            var fileName = Path.Combine(domainsDir, $"{domain.Name}.sql");
            File.WriteAllText(fileName, script);
        }
        Console.WriteLine($"✓ Zapisano {domains.Count} skryptów domen");

        foreach (var table in tables)
        {
            var script = SqlScriptGenerator.GenerateTableScript(table);
            var fileName = Path.Combine(tablesDir, $"{table.Name}.sql");
            File.WriteAllText(fileName, script);
        }
        Console.WriteLine($"✓ Zapisano {tables.Count} skryptów tabel");

        foreach (var procedure in procedures)
        {
            var script = SqlScriptGenerator.GenerateProcedureScript(procedure);
            var fileName = Path.Combine(proceduresDir, $"{procedure.Name}.sql");
            File.WriteAllText(fileName, script);
        }
        Console.WriteLine($"✓ Zapisano {procedures.Count} skryptów procedur");

        Console.WriteLine();
        Console.WriteLine("=== Podsumowanie ===");
        Console.WriteLine($"Katalog wyjściowy: {absoluteOutputDirectory}");
        Console.WriteLine($"Łącznie plików: {domains.Count + tables.Count + procedures.Count}");
    }
}
