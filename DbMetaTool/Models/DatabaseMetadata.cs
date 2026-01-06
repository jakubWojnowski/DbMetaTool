namespace DbMetaTool.Models;

public class DatabaseMetadata
{
    public List<Domain> Domains { get; set; } = new();
    
    public List<Table> Tables { get; set; } = new();
    
    public List<StoredProcedure> Procedures { get; set; } = new();
}

