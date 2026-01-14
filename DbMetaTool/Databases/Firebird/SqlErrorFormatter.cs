using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Databases.Firebird;

public static class FirebirdSqlErrorFormatter
{
    public static string FormatSqlError(Exception ex, string sql, int statementIndex)
    {
        if (ex is not FbException fbEx)
        {
            return FormatGenericError(ex, sql, statementIndex);
        }

        return FormatFirebirdError(fbEx, sql, statementIndex);
    }

    private static string FormatFirebirdError(FbException ex, string sql, int statementIndex)
    {
        var sqlPreview = GetSqlPreview(sql, 200);
        var statementType = DetectStatementType(sql);
        
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(statementType))
        {
            sb.AppendLine($"Błąd wykonania {statementType} (statement #{statementIndex}):");
        }
        else
        {
            sb.AppendLine($"Błąd wykonania statement #{statementIndex}:");
        }
        sb.AppendLine();
        
        if (!string.IsNullOrWhiteSpace(ex.Message))
        {
            sb.AppendLine(ex.Message);
        }
        
        if (ex.Errors is { Count: > 0 })
        {
            var uniqueMessages = ex.Errors
                .Where(e => !string.IsNullOrWhiteSpace(e.Message))
                .Select(e => e.Message.Trim())
                .Distinct()
                .Where(msg => string.IsNullOrWhiteSpace(ex.Message) || !ex.Message.Contains(msg, StringComparison.OrdinalIgnoreCase))
                .ToList();
                
            if (uniqueMessages.Count > 0)
            {
                sb.AppendLine();
                foreach (var msg in uniqueMessages)
                {
                    sb.AppendLine($"  {msg}");
                }
            }
        }
        
        if (sql.Length <= 300)
        {
            sb.AppendLine();
            sb.AppendLine("SQL:");
            sb.AppendLine(sqlPreview);
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine($"SQL: {sqlPreview}... (pełny SQL ma {sql.Length} znaków)");
        }
        
        return sb.ToString();
    }

    public static string FormatValidationError(List<string> invalidProcedures)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Błąd walidacji integralności procedur:");
        sb.AppendLine();
        sb.AppendLine($"Znaleziono {invalidProcedures.Count} procedur z nieprawidłowym BLR:");
        
        foreach (var procName in invalidProcedures)
        {
            sb.AppendLine($"  - {procName}");
        }
        
        sb.AppendLine();
        sb.AppendLine("Prawdopodobna przyczyna: Niezgodność sygnatur wywołań procedur.");
        sb.AppendLine("Sprawdź skrypty SQL i upewnij się, że wszystkie wywołania procedur mają poprawne sygnatury.");
        
        return sb.ToString();
    }

    private static string FormatGenericError(Exception ex, string sql, int statementIndex)
    {
        var sqlPreview = GetSqlPreview(sql, 200);
        var statementType = DetectStatementType(sql);
        
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(statementType))
        {
            sb.AppendLine($"Błąd wykonania {statementType} (statement #{statementIndex}):");
        }
        else
        {
            sb.AppendLine($"Błąd wykonania statement #{statementIndex}:");
        }
        sb.AppendLine();
        
        if (!string.IsNullOrWhiteSpace(ex.Message))
        {
            sb.AppendLine(ex.Message);
        }
        
        if (ex.InnerException != null)
        {
            sb.AppendLine();
            sb.AppendLine($"Szczegóły: {ex.InnerException.Message}");
        }
        
        if (sql.Length <= 300)
        {
            sb.AppendLine();
            sb.AppendLine("SQL:");
            sb.AppendLine(sqlPreview);
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine($"SQL: {sqlPreview}... (pełny SQL ma {sql.Length} znaków)");
        }
        
        return sb.ToString();
    }

    private static string GetSqlPreview(string sql, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return "(pusty SQL)";
        
        if (sql.Length <= maxLength)
            return sql;
        
        var preview = sql[..maxLength];
        var remaining = sql.Length - maxLength;
        return $"{preview}\n... (pominięto {remaining} znaków)";
    }

    private static readonly string[] StatementPrefixes = 
    [
        "CREATE",
        "ALTER",
        "DROP",
        "INSERT",
        "UPDATE",
        "DELETE",
        "SELECT",
        "SET TERM"
    ];

    private static string DetectStatementType(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return string.Empty;
        
        var upperSql = sql.TrimStart().ToUpperInvariant();
        
        return StatementPrefixes
            .FirstOrDefault(prefix => upperSql.StartsWith(prefix, StringComparison.Ordinal)) 
            ?? string.Empty;
    }
}

