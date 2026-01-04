using DbMetaTool.Firebird;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Services.Firebird;

public class FirebirdSqlExecutor
(
    FirebirdConnectionFactory connectionFactory
) : ISqlExecutor, IDisposable
{
    private FbConnection? _connection;
    private FbTransaction? _readTransaction;
    private bool _disposed;

    public void ExecuteBatch(List<string> sqlStatements)
    {
        if (sqlStatements == null)
            throw new ArgumentNullException(nameof(sqlStatements));

        if (sqlStatements.Count == 0)
            return;

        EnsureConnection();
        
        if (_readTransaction != null)
        {
            _readTransaction.Dispose();
            _readTransaction = null;
        }

        var options = new FbTransactionOptions()
        {
            TransactionBehavior = FbTransactionBehavior.Concurrency | 
                                  FbTransactionBehavior.Wait,
            WaitTimeout = TimeSpan.FromSeconds(10)
        };

        using var transaction = _connection!.BeginTransaction(options);

        try
        {
            foreach (var sql in sqlStatements)
            {
                if (string.IsNullOrWhiteSpace(sql))
                    continue;

                using var command = _connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<T> ExecuteRead<T>(string sql, Func<System.Data.IDataReader, T> mapper)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be empty", nameof(sql));

        if (mapper == null)
            throw new ArgumentNullException(nameof(mapper));

        EnsureConnection();

        if (_readTransaction == null)
        {
            var options = new FbTransactionOptions()
            {
                TransactionBehavior = FbTransactionBehavior.Concurrency | 
                                      FbTransactionBehavior.Wait | 
                                      FbTransactionBehavior.Read
            };

            _readTransaction = _connection!.BeginTransaction(options);
        }

        var results = new List<T>();

        using var command = _connection!.CreateCommand();
        command.Transaction = _readTransaction;
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
            _connection = connectionFactory.CreateAndOpenConnection();
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
        
        _readTransaction?.Dispose();
        _connection?.Dispose();

        _disposed = true;
    }
}