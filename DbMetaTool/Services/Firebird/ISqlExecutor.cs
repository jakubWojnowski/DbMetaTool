namespace DbMetaTool.Services.Firebird;

public interface ISqlExecutor
{
    void ExecuteInTransaction(Action<ISqlExecutor> action);
    
    void ExecuteNonQuery(string sql);
    
    void ExecuteScript(string script);

    List<T> ExecuteQuery<T>(string sql, Func<System.Data.IDataReader, T> mapper);
}

