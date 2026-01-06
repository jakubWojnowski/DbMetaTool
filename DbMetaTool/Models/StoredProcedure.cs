namespace DbMetaTool.Models;

public class StoredProcedure
{
    public string Name { get; set; } = string.Empty;
    
    public string Source { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public List<ProcedureParameter> InputParameters { get; set; } = new();
    
    public List<ProcedureParameter> OutputParameters { get; set; } = new();
}

public class ProcedureParameter
{
    public string Name { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;
    
    public int? Length { get; set; }
    
    public int? Scale { get; set; }
    
    public int Position { get; set; }
}

