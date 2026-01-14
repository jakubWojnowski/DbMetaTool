using System.Data;

namespace DbMetaTool.Databases;

public interface ISqlExecutor : IDisposable
{
    DatabaseType DatabaseType { get; }

    Task ExecuteBatchAsync(
        List<string> sqlStatements, 
        Func<ISqlExecutor, Task>? validationCallback = null);
    
    Task<List<T>> ExecuteReadAsync<T>(string sql, Func<IDataReader, T> mapper);
}
