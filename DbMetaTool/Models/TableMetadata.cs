namespace DbMetaTool.Models;

public record TableMetadata(
    string Name,
    List<ColumnMetadata> Columns
);


