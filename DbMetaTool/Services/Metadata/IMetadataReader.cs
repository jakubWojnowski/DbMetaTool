using DbMetaTool.Models;
using DbMetaTool.Services.Firebird;

namespace DbMetaTool.Services.Metadata;

public interface IMetadataReader
{
    List<DomainMetadata> ReadDomains(ISqlExecutor executor);
    
    List<TableMetadata> ReadTables(ISqlExecutor executor);
    
    List<ProcedureMetadata> ReadProcedures(ISqlExecutor executor);
}
