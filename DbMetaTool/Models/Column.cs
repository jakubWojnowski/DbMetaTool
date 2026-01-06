namespace DbMetaTool.Models;

public class Column
{
    public string Name { get; set; } = string.Empty;
    
    public string? DomainName { get; set; }
    
    public string? DataType { get; set; }
    
    public int? Length { get; set; }
    
    public int? Scale { get; set; }

    public bool IsNullable { get; set; } = true;
    
    public string? DefaultValue { get; set; }
    
    public int Position { get; set; }
    
    public string? CharacterSet { get; set; }
    
    public string? Collation { get; set; }
}

