using DbMetaTool.Databases;
using DbMetaTool.Models;

namespace DbMetaTool.Services.Metadata;

public interface IMetadataReader
{
    Task<List<DomainMetadata>> ReadDomainsAsync(ISqlExecutor executor);
    
    Task<List<TableMetadata>> ReadTablesAsync(ISqlExecutor executor);
    
    Task<List<ProcedureMetadata>> ReadProceduresAsync(ISqlExecutor executor);
}
