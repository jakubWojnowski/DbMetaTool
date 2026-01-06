namespace DbMetaTool.Models;

public record DomainMetadata(
    string Name,
    string DataType,
    int? Length,
    int? Precision,
    int? Scale,
    bool IsNullable,
    string? DefaultValue,
    string? CheckConstraint
);
