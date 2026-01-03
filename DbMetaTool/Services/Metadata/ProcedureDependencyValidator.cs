using System.Text;
using DbMetaTool.Services.Firebird;

namespace DbMetaTool.Services.Metadata;

public static class ProcedureDependencyValidator
{
    public static List<string> GetDependentProcedures(ISqlExecutor executor, string procedureName)
    {
        var sql = new StringBuilder();
        
        sql.AppendLine("SELECT DISTINCT RDB$DEPENDENT_NAME");
        sql.AppendLine("FROM RDB$DEPENDENCIES");
        sql.AppendLine($"WHERE RDB$DEPENDED_ON_NAME = '{procedureName}'");
        sql.AppendLine("  AND RDB$DEPENDED_ON_TYPE = 5");
        sql.AppendLine("ORDER BY RDB$DEPENDENT_NAME");

        return executor.ExecuteQuery(sql.ToString(), reader => 
            reader["RDB$DEPENDENT_NAME"].ToString()!.Trim());
    }

    public static List<string> GetCallingProcedures(ISqlExecutor executor, string procedureName)
    {
        var sql = new StringBuilder();
        
        sql.AppendLine("SELECT DISTINCT RDB$DEPENDENT_NAME");
        sql.AppendLine("FROM RDB$DEPENDENCIES");
        sql.AppendLine($"WHERE RDB$DEPENDED_ON_NAME = '{procedureName}'");
        sql.AppendLine("  AND RDB$DEPENDED_ON_TYPE = 5");
        sql.AppendLine("  AND RDB$DEPENDENT_TYPE = 5");
        sql.AppendLine("ORDER BY RDB$DEPENDENT_NAME");

        return executor.ExecuteQuery(sql.ToString(), reader => 
            reader["RDB$DEPENDENT_NAME"].ToString()!.Trim());
    }

    public static List<string> GetInvalidProcedures(ISqlExecutor executor)
    {
        var sql = new StringBuilder();
        
        sql.AppendLine("SELECT RDB$PROCEDURE_NAME");
        sql.AppendLine("FROM RDB$PROCEDURES");
        sql.AppendLine("WHERE RDB$VALID_BLR = 0");
        sql.AppendLine("  AND RDB$SYSTEM_FLAG = 0");
        sql.AppendLine("ORDER BY RDB$PROCEDURE_NAME");

        return executor.ExecuteQuery(sql.ToString(), reader => 
            reader["RDB$PROCEDURE_NAME"].ToString()!.Trim());
    }

    public static void RecompileProcedure(ISqlExecutor executor, string procedureName)
    {
        var sql = $"ALTER PROCEDURE {procedureName} RECOMPILE";
        executor.ExecuteNonQuery(sql);
    }
}

