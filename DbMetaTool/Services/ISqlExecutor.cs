namespace DbMetaTool.Services;

public interface ISqlExecutor
{
    void ExecuteInTransaction(Action<ISqlExecutor> action);
    
    void ExecuteNonQuery(string sql);
    
    void ExecuteScript(string script);
    
    T ExecuteScalar<T>(string sql);
    
    List<T> ExecuteQuery<T>(string sql, Func<System.Data.IDataReader, T> mapper);
}

