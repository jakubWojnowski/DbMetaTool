using DbMetaTool.Models;
using System.Text;

namespace DbMetaTool.Services;

public static class DatabaseSchemaComparer
{
    public static List<string> GenerateAlterStatements(
        TableMetadata existingTable,
        TableMetadata desiredTable)
    {
        var statements = new List<string>();

        foreach (var desiredColumn in desiredTable.Columns)
        {
            var existingColumn = existingTable.Columns
                .FirstOrDefault(c => c.Name.Equals(desiredColumn.Name, StringComparison.OrdinalIgnoreCase));

            if (existingColumn == null)
            {
                var addColumnStatement = GenerateAddColumnStatement(existingTable.Name, desiredColumn);
                statements.Add(addColumnStatement);
            }
            else if (!AreColumnsEqual(existingColumn, desiredColumn))
            {
                var comment = $"-- MANUAL REVIEW REQUIRED: Column {desiredColumn.Name} exists but has different definition";
                statements.Add(comment);
            }
        }

        return statements;
    }

    public static string GenerateAddColumnStatement(string tableName, ColumnMetadata column)
    {
        var sb = new StringBuilder();
        sb.Append($"ALTER TABLE {tableName} ADD {column.Name} {column.DataType}");

        if (!column.IsNullable)
        {
            sb.Append(" NOT NULL");
        }

        if (!string.IsNullOrWhiteSpace(column.DefaultValue))
        {
            sb.Append($" {column.DefaultValue.Trim()}");
        }

        return sb.ToString();
    }

    private static bool AreColumnsEqual(ColumnMetadata col1, ColumnMetadata col2)
    {
        var dataType1 = NormalizeDataType(col1.DataType);
        var dataType2 = NormalizeDataType(col2.DataType);
        
        return dataType1.Equals(dataType2, StringComparison.OrdinalIgnoreCase) &&
               col1.IsNullable == col2.IsNullable;
    }

    private static string NormalizeDataType(string dataType)
    {
        return dataType.Replace(" ", "").ToUpperInvariant();
    }
}

