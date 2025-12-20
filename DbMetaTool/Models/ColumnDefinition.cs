namespace DbMetaTool.Models;

public record ColumnDefinition(
    string Name,
    string DataType,
    bool IsNullable,
    string? DefaultValue
);