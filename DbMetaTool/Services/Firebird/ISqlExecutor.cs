namespace DbMetaTool.Services.Firebird;

public interface ISqlExecutor
{
    void ExecuteBatch(List<string> sqlStatements);
    
    List<T> ExecuteRead<T>(string sql, Func<System.Data.IDataReader, T> mapper);
}

