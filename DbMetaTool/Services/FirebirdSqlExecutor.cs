using DbMetaTool.Firebird;
using DbMetaTool.Utilities;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Services;

public class FirebirdSqlExecutor : ISqlExecutor, IDisposable
{
    private readonly FirebirdConnectionFactory _connectionFactory;
    private FbConnection? _connection;
    private FbTransaction? _transaction;
    private bool _disposed;

    public FirebirdSqlExecutor(FirebirdConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public void ExecuteInTransaction(Action<ISqlExecutor> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        EnsureConnection();

        using var transaction = _connection!.BeginTransaction();
        _transaction = transaction;

        try
        {
            action(this);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            _transaction = null;
        }
    }

    public void ExecuteNonQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be empty", nameof(sql));

        EnsureConnection();

        using var command = _connection!.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public void ExecuteScript(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            throw new ArgumentException("Script cannot be empty", nameof(script));

        var statements = SqlScriptParser.ParseScript(script);

        foreach (var statement in statements)
        {
            if (!string.IsNullOrWhiteSpace(statement))
            {
                ExecuteNonQuery(statement);
            }
        }
    }

    public T ExecuteScalar<T>(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be empty", nameof(sql));

        EnsureConnection();

        using var command = _connection!.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = sql;
        
        var result = command.ExecuteScalar();
        
        if (result == null || result == DBNull.Value)
            return default!;

        return (T)result;
    }

    public List<T> ExecuteQuery<T>(string sql, Func<System.Data.IDataReader, T> mapper)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be empty", nameof(sql));

        if (mapper == null)
            throw new ArgumentNullException(nameof(mapper));

        EnsureConnection();

        var results = new List<T>();

        using var command = _connection!.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(mapper(reader));
        }

        return results;
    }

    private void EnsureConnection()
    {
        if (_connection == null)
        {
            _connection = _connectionFactory.CreateAndOpenConnection();
        }
        else if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _transaction?.Dispose();
        _connection?.Dispose();

        _disposed = true;
    }
}

