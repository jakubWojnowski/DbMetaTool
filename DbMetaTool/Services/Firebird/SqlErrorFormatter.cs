using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Services.Firebird;

public static class SqlErrorFormatter
{
    public static string FormatFirebirdError(FbException ex, string sql, int statementIndex)
    {
        var sqlPreview = GetSqlPreview(sql);
        var statementType = DetectStatementType(sql);
        
        var errorMessage = new System.Text.StringBuilder();
        errorMessage.AppendLine($"=== Błąd wykonania statement #{statementIndex} ===");
        
        if (!string.IsNullOrEmpty(statementType))
        {
            errorMessage.AppendLine($"Typ: {statementType}");
        }
        
        errorMessage.AppendLine();
        errorMessage.AppendLine($"Kod błędu Firebird: {ex.ErrorCode}");
        
        if (!string.IsNullOrWhiteSpace(ex.Message))
        {
            errorMessage.AppendLine($"Komunikat: {ex.Message}");
        }
        
        if (ex.Errors is { Count: > 0 })
        {
            errorMessage.AppendLine();
            errorMessage.AppendLine("Szczegóły błędów Firebird:");
            foreach (var error in ex.Errors)
            {
                errorMessage.AppendLine($"  Kod: {error.Number}");
                if (!string.IsNullOrWhiteSpace(error.Message))
                {
                    errorMessage.AppendLine($"  Komunikat: {error.Message}");
                }
            }
        }
        
        errorMessage.AppendLine();
        errorMessage.AppendLine($"SQL ({sql.Length} znaków):");
        errorMessage.AppendLine(sqlPreview);
        
        return errorMessage.ToString();
    }

    public static string FormatGenericError(Exception ex, string sql, int statementIndex)
    {
        var sqlPreview = GetSqlPreview(sql);
        var statementType = DetectStatementType(sql);
        
        var errorMessage = new System.Text.StringBuilder();
        errorMessage.AppendLine($"=== Błąd wykonania statement #{statementIndex} ===");
        
        if (!string.IsNullOrEmpty(statementType))
        {
            errorMessage.AppendLine($"Typ: {statementType}");
        }
        
        errorMessage.AppendLine($"Typ błędu: {ex.GetType().Name}");
        errorMessage.AppendLine($"Komunikat: {ex.Message}");
        
        if (ex.InnerException != null)
        {
            errorMessage.AppendLine($"Błąd wewnętrzny: {ex.InnerException.Message}");
        }
        
        errorMessage.AppendLine($"\nSQL ({sql.Length} znaków):");
        errorMessage.AppendLine(sqlPreview);
        
        return errorMessage.ToString();
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

