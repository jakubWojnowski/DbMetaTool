using DbMetaTool.Models;
using DbMetaTool.Services.Firebird;

namespace DbMetaTool.Services.Metadata;

public interface IMetadataReader
{
    Task<List<DomainMetadata>> ReadDomainsAsync(ISqlExecutor executor);
    
    Task<List<TableMetadata>> ReadTablesAsync(ISqlExecutor executor);
    
    Task<List<ProcedureMetadata>> ReadProceduresAsync(ISqlExecutor executor);
}
