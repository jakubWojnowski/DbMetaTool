using System.Text;
using DbMetaTool.Services.Firebird;

namespace DbMetaTool.Services.Validation;

public static class ProcedureDependencyValidator
{
    public static Task<List<string>> GetCallingProceduresAsync(ISqlExecutor executor, string procedureName)
    {
        var sql = new StringBuilder();
        
        sql.AppendLine("SELECT DISTINCT RDB$DEPENDENT_NAME");
        sql.AppendLine("FROM RDB$DEPENDENCIES");
        sql.AppendLine($"WHERE RDB$DEPENDED_ON_NAME = '{procedureName}'");
        sql.AppendLine("  AND RDB$DEPENDED_ON_TYPE = 5");
        sql.AppendLine("  AND RDB$DEPENDENT_TYPE = 5");
        sql.AppendLine("ORDER BY RDB$DEPENDENT_NAME");

        return executor.ExecuteReadAsync(sql.ToString(), reader => 
            reader["RDB$DEPENDENT_NAME"].ToString()!.Trim());
    }
}

