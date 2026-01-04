using DbMetaTool.Models;
using DbMetaTool.Models.results;
using DbMetaTool.Services.Firebird;
using DbMetaTool.Services.Metadata;

namespace DbMetaTool.Services.Export;

public static class MetadataExportService
{
    public static ExportResult ExportAll(ISqlExecutor executor, string outputDirectory)
    {
        if (executor == null)
        {
            throw new ArgumentNullException(nameof(executor));
        }
        if (outputDirectory == null)
        {
            throw new ArgumentNullException(nameof(outputDirectory));
        }

        PrepareOutputDirectory(outputDirectory);

        Console.WriteLine("Pobieranie metadanych (spójny snapshot)...");
        
        var domains = ReadDomains(executor);
        
        var tables = ReadTables(executor);
        
        var procedures = ReadProcedures(executor);

        Console.WriteLine();
        Console.WriteLine("Generowanie skryptów SQL...");

        ExportDomains(outputDirectory, domains);
        
        ExportTables(outputDirectory, tables);
        
        ExportProcedures(outputDirectory, procedures);

        return new ExportResult(
            OutputDirectory: Path.GetFullPath(outputDirectory),
            DomainsCount: domains.Count,
            TablesCount: tables.Count,
            ProceduresCount: procedures.Count);
    }

    private static void PrepareOutputDirectory(string outputDirectory)
    {
        var absolutePath = Path.GetFullPath(outputDirectory);

        if (!Directory.Exists(absolutePath))
        {
            Directory.CreateDirectory(absolutePath);
        }

        var domainsDir = Path.Combine(absolutePath, "domains");
        
        var tablesDir = Path.Combine(absolutePath, "tables");
        
        var proceduresDir = Path.Combine(absolutePath, "procedures");

        Directory.CreateDirectory(domainsDir);
        
        Directory.CreateDirectory(tablesDir);
        
        Directory.CreateDirectory(proceduresDir);
    }

    private static List<DomainMetadata> ReadDomains(ISqlExecutor executor)
    {
        var domains = FirebirdMetadataReader.ReadDomains(executor);
        
        Console.WriteLine($"✓ Znaleziono {domains.Count} domen");
        
        return domains;
    }

    private static List<TableMetadata> ReadTables(ISqlExecutor executor)
    {
        var tables = FirebirdMetadataReader.ReadTables(executor);
        
        Console.WriteLine($"✓ Znaleziono {tables.Count} tabel");
        
        return tables;
    }

    private static List<ProcedureMetadata> ReadProcedures(ISqlExecutor executor)
    {
        var procedures = FirebirdMetadataReader.ReadProcedures(executor);
        
        Console.WriteLine($"✓ Znaleziono {procedures.Count} procedur");
        
        return procedures;
    }

    private static void ExportDomains(string outputDirectory, List<DomainMetadata> domains)
    {
        var domainsDir = Path.Combine(Path.GetFullPath(outputDirectory), "domains");

        foreach (var domain in domains)
        {
            var script = SqlScriptGenerator.GenerateDomainScript(domain);
            
            var fileName = Path.Combine(domainsDir, $"{domain.Name}.sql");
            
            File.WriteAllText(fileName, script);
        }

        Console.WriteLine($"✓ Zapisano {domains.Count} skryptów domen");
    }

    private static void ExportTables(string outputDirectory, List<TableMetadata> tables)
    {
        var tablesDir = Path.Combine(Path.GetFullPath(outputDirectory), "tables");

        foreach (var table in tables)
        {
            var script = SqlScriptGenerator.GenerateTableScript(table);
            
            var fileName = Path.Combine(tablesDir, $"{table.Name}.sql");
            
            File.WriteAllText(fileName, script);
        }

        Console.WriteLine($"✓ Zapisano {tables.Count} skryptów tabel");
    }

    private static void ExportProcedures(string outputDirectory, List<ProcedureMetadata> procedures)
    {
        var proceduresDir = Path.Combine(Path.GetFullPath(outputDirectory), "procedures");

        foreach (var procedure in procedures)
        {
            var script = SqlScriptGenerator.GenerateProcedureScript(procedure);
            
            var fileName = Path.Combine(proceduresDir, $"{procedure.Name}.sql");
            
            File.WriteAllText(fileName, script);
        }

        Console.WriteLine($"✓ Zapisano {procedures.Count} skryptów procedur");
    }
}