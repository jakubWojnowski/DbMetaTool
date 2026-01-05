using DbMetaTool.Models;

namespace DbMetaTool.Tests.TestHelpers;

public static class TestDataBuilder
{
    public static DomainMetadata CreateDomain(
        string name,
        string dataType = "VARCHAR",
        int? length = 255,
        bool isNullable = true)
    {
        return new DomainMetadata(
            name,
            dataType,
            length,
            null,
            null,
            isNullable,
            null,
            null);
    }

    public static TableMetadata CreateTable(string tableName, params ColumnMetadata[] columns)
    {
        return new TableMetadata(tableName, columns.ToList());
    }

    private static ColumnMetadata CreateColumn(
        string name,
        string dataType,
        int position = 0,
        bool isNullable = true,
        int? length = null,
        string? defaultValue = null)
    {
        return new ColumnMetadata(
            position,
            name,
            dataType,
            length,
            null,
            null,
            isNullable,
            defaultValue,
            null);
    }

    public static ColumnMetadata CreateIntegerColumn(string name, int position, bool isNullable = false)
    {
        return CreateColumn(name, "INTEGER", position, isNullable);
    }

    public static ColumnMetadata CreateVarcharColumn(string name, int position, int length = 100, bool isNullable = true)
    {
        return CreateColumn(name, $"VARCHAR({length})", position, isNullable, length);
    }

    public static ProcedureMetadata CreateProcedure(string name, string? source = null)
    {
        return new ProcedureMetadata(name, source);
    }
}
