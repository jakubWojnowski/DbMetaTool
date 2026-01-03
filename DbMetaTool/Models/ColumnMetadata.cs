namespace DbMetaTool.Models;

public record ColumnMetadata(
    int Position,
    string Name,
    string DataType,
    int? Length,
    int? Precision,
    int? Scale,
    bool IsNullable,
    string? DefaultValue,
    string? DomainName
);
