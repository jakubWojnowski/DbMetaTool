using DbMetaTool.Models;

namespace DbMetaTool.Utilities;

public static class ScriptDefinitionParser
{
    public static TableMetadata? ParseTableFromScript(string sql, string tableName)
    {
        var lines = sql.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var columns = new List<ColumnMetadata>();
        var position = 0;

        var inColumns = false;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("("))
            {
                inColumns = true;
                continue;
            }

            if (trimmed.StartsWith(")"))
            {
                break;
            }

            if (inColumns && !string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("--"))
            {
                var columnDef = ParseColumnDefinition(trimmed, position++);
                if (columnDef != null)
                {
                    columns.Add(columnDef);
                }
            }
        }

        if (columns.Count == 0)
        {
            return null;
        }

        return new TableMetadata(tableName, columns);
    }

    public static ColumnMetadata? ParseColumnDefinition(string line, int position)
    {
        line = line.TrimEnd(',').Trim();
        
        var notNullIndex = line.IndexOf("NOT NULL", StringComparison.OrdinalIgnoreCase);
        var defaultIndex = line.IndexOf("DEFAULT", StringComparison.OrdinalIgnoreCase);
        
        var endOfTypeIndex = line.Length;
        if (notNullIndex > 0)
        {
            endOfTypeIndex = Math.Min(endOfTypeIndex, notNullIndex);
        }
        if (defaultIndex > 0)
        {
            endOfTypeIndex = Math.Min(endOfTypeIndex, defaultIndex);
        }

        var columnDefinition = line[..endOfTypeIndex].Trim();
        
        var firstSpaceIndex = columnDefinition.IndexOf(' ');
        if (firstSpaceIndex < 0)
        {
            return null;
        }

        var name = columnDefinition[..firstSpaceIndex].Trim();
        var dataType = columnDefinition[(firstSpaceIndex + 1)..].Trim();
        var isNullable = notNullIndex < 0;

        return new ColumnMetadata(
            Position: position,
            Name: name,
            DataType: dataType,
            Length: null,
            Precision: null,
            Scale: null,
            IsNullable: isNullable,
            DefaultValue: null,
            DomainName: null);
    }

    public static string ExtractColumnName(string alterStatement)
    {
        var parts = alterStatement.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        var addIndex = Array.FindIndex(parts, p => p.Equals("ADD", StringComparison.OrdinalIgnoreCase));

        if (addIndex >= 0 && addIndex + 1 < parts.Length)
        {
            return parts[addIndex + 1];
        }

        return "UNKNOWN";
    }
}

