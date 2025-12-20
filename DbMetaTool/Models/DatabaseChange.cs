namespace DbMetaTool.Models;

public enum ChangeType
{
    TableCreated,
    ColumnAdded,
    ProcedureModified,
    DomainCreated,
    ManualReviewRequired
}

public record DatabaseChange(
    ChangeType Type,
    string ObjectName,
    string? Details
);
