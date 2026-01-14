using System.Text;
using DbMetaTool.Databases;

namespace DbMetaTool.Databases.Firebird;

public static class FirebirdProcedureBlrValidator
{
    private static Task<List<string>> GetInvalidBlrProceduresAsync(ISqlExecutor executor)
    {
        var sql = new StringBuilder();
        sql.AppendLine("SELECT RDB$PROCEDURE_NAME");
        sql.AppendLine("FROM RDB$PROCEDURES");
        sql.AppendLine("WHERE RDB$VALID_BLR = 0");
        sql.AppendLine("  AND RDB$SYSTEM_FLAG = 0");
        sql.AppendLine("  AND RDB$PROCEDURE_NAME NOT STARTING WITH 'MON$'");
        sql.AppendLine("  AND RDB$PROCEDURE_NAME NOT STARTING WITH 'SEC$'");
        sql.AppendLine("ORDER BY RDB$PROCEDURE_NAME");

        return executor.ExecuteReadAsync(sql.ToString(), reader => 
            reader["RDB$PROCEDURE_NAME"].ToString()!.Trim());
    }

    public static async Task ValidateProcedureIntegrityAsync(ISqlExecutor executor)
    {
        var invalidProcedures = await GetInvalidBlrProceduresAsync(executor);

        if (invalidProcedures.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("=== Walidacja integralności procedur ===");
        Console.WriteLine($"⚠ Znaleziono {invalidProcedures.Count} procedur z nieprawidłowym BLR:");
        
        foreach (var procName in invalidProcedures)
        {
            Console.WriteLine($"  - {procName}");
        }
        Console.WriteLine();

        var errorMessage = FirebirdSqlErrorFormatter.FormatValidationError(invalidProcedures);
        throw new InvalidOperationException(errorMessage);
    }
}
