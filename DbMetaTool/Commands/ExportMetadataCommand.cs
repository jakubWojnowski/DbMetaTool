using System;
using DbMetaTool.Firebird;
using DbMetaTool.Exporters;

namespace DbMetaTool.Commands
{
    public class ExportMetadataCommand
    {
        public static void Execute(string connectionString, string outputDirectory)
        {
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
}

