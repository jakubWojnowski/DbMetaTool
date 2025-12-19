using System.Text;

namespace DbMetaTool.Utilities;


public static class SqlFormatter
{
    public static string FormatIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        if (name.Any(c => char.IsLower(c) || !char.IsLetterOrDigit(c) && c != '_'))
        {
            return $"\"{name}\"";
        }

        return name.ToUpperInvariant();
    }
    
    public static string FormatDefaultValue(string? defaultValue)
    {
        if (string.IsNullOrWhiteSpace(defaultValue))
            return string.Empty;

        if (defaultValue.StartsWith("'") || 
            defaultValue.StartsWith("\"") ||
            char.IsDigit(defaultValue[0]) ||
            defaultValue.StartsWith("GEN_ID") ||
            defaultValue.StartsWith("CURRENT_"))
        {
            return defaultValue;
        }

        return $"'{defaultValue}'";
    }
    
    public static string FormatDataType(string dataType, int? length = null, int? scale = null)
    {
        var sb = new StringBuilder(dataType.ToUpperInvariant());

        if (length.HasValue)
        {
            sb.Append('(');
            sb.Append(length.Value);
            
            if (scale.HasValue)
            {
                sb.Append(',');
                sb.Append(scale.Value);
            }
            
            sb.Append(')');
        }

        return sb.ToString();
    }
    
    public static string CleanSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return string.Empty;

        sql = sql.Trim();

        while (sql.Contains("  "))
        {
            sql = sql.Replace("  ", " ");
        }

        sql = sql.Replace("\r\n", "\n").Replace("\r", "\n");

        return sql;
    }
}

