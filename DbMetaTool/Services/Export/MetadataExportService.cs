using DbMetaTool.Databases;
using DbMetaTool.Models;
using DbMetaTool.Models.results;
using DbMetaTool.Services.Metadata;

namespace DbMetaTool.Services.Export;

public class MetadataExportService(
    IMetadataReader metadataReader) : IMetadataExportService
{
    public async Task<ExportResult> ExportAll(ISqlExecutor executor, string outputDirectory,
        CancellationToken cancellationToken = default)
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

        var domains = await ReadDomainsAsync(executor);

        var tables = await ReadTablesAsync(executor);

        var procedures = await ReadProceduresAsync(executor);

        Console.WriteLine();
        Console.WriteLine("Generowanie skryptów SQL...");

        await ExportDomains(outputDirectory, domains, cancellationToken);

        await ExportTables(outputDirectory, tables, cancellationToken);

        await ExportProcedures(outputDirectory, procedures, cancellationToken);

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

    private async Task<List<DomainMetadata>> ReadDomainsAsync(ISqlExecutor executor)
    {
        var domains = await metadataReader.ReadDomainsAsync(executor);

        Console.WriteLine($"✓ Znaleziono {domains.Count} domen");

        return domains;
    }

    private async Task<List<TableMetadata>> ReadTablesAsync(ISqlExecutor executor)
    {
        var tables = await metadataReader.ReadTablesAsync(executor);

        Console.WriteLine($"✓ Znaleziono {tables.Count} tabel");

        return tables;
    }

    private async Task<List<ProcedureMetadata>> ReadProceduresAsync(ISqlExecutor executor)
    {
        var procedures = await metadataReader.ReadProceduresAsync(executor);

        Console.WriteLine($"✓ Znaleziono {procedures.Count} procedur");

        return procedures;
    }

    private static async Task ExportDomains(string outputDirectory, List<DomainMetadata> domains, CancellationToken cancellationToken)
    {
        var domainsDir = Path.Combine(Path.GetFullPath(outputDirectory), "domains");

        foreach (var domain in domains)
        {
            var script = SqlScriptGenerator.GenerateDomainScript(domain);

            var fileName = Path.Combine(domainsDir, $"{domain.Name}.sql");

            await File.WriteAllTextAsync(fileName, script, cancellationToken);
        }

        Console.WriteLine($"✓ Zapisano {domains.Count} skryptów domen");
    }

    private static async Task ExportTables(string outputDirectory, List<TableMetadata> tables, CancellationToken cancellationToken)
    {
        var tablesDir = Path.Combine(Path.GetFullPath(outputDirectory), "tables");

        foreach (var table in tables)
        {
            var script = SqlScriptGenerator.GenerateTableScript(table);

            var fileName = Path.Combine(tablesDir, $"{table.Name}.sql");

            await File.WriteAllTextAsync(fileName, script, cancellationToken);
        }

        Console.WriteLine($"✓ Zapisano {tables.Count} skryptów tabel");
    }

    private static async Task ExportProcedures(string outputDirectory, List<ProcedureMetadata> procedures, CancellationToken cancellationToken)
    {
        var proceduresDir = Path.Combine(Path.GetFullPath(outputDirectory), "procedures");

        foreach (var procedure in procedures)
        {
            var script = SqlScriptGenerator.GenerateProcedureScript(procedure);

            var fileName = Path.Combine(proceduresDir, $"{procedure.Name}.sql");

            await File.WriteAllTextAsync(fileName, script, cancellationToken);
        }

        Console.WriteLine($"✓ Zapisano {procedures.Count} skryptów procedur");
    }
}