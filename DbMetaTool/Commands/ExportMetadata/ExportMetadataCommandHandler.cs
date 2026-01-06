using DbMetaTool.Firebird;
using DbMetaTool.Utilities;

namespace DbMetaTool.Commands.ExportMetadata;

public static class ExportMetadataCommandHandler
{
    public static void Handle(ExportMetadataCommand command)
    {
        var (connectionString, outputDirectory) = command;
        
        var connectionFactory = new FirebirdConnectionFactory(connectionString);
        var metadataReader = new MetadataReader(connectionFactory);
        var sqlExporter = new SqlExporter(outputDirectory);

        var domains = metadataReader.GetDomains();
        sqlExporter.ExportDomains(domains);

        var tables = metadataReader.GetTables();
        sqlExporter.ExportTables(tables);

        var procedures = metadataReader.GetProcedures();
        sqlExporter.ExportProcedures(procedures);
    }
}