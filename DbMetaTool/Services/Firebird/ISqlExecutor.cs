namespace DbMetaTool.Services.Firebird;

public interface ISqlExecutor
{
    Task ExecuteBatchAsync(List<string> sqlStatements, Func<ISqlExecutor, Task>? validationCallback = null);
    
    Task<List<T>> ExecuteReadAsync<T>(string sql, Func<System.Data.IDataReader, T> mapper);
}

