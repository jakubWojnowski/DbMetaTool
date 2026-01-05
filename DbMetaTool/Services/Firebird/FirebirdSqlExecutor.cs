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
    private FbTransaction? _currentWriteTransaction;
    private bool _disposed;

    public void ExecuteBatch(List<string> sqlStatements, Action<ISqlExecutor>? validationCallback = null)
    {
        if (sqlStatements is null)
            throw new ArgumentNullException(nameof(sqlStatements));

        if (sqlStatements.Count == 0)
            return;

        EnsureConnection();
        
        if (_readTransaction != null)
        {
            _readTransaction.Dispose();
            
            _readTransaction = null;
        }

        var options = new FbTransactionOptions
        {
            TransactionBehavior = FbTransactionBehavior.Concurrency | 
                                  FbTransactionBehavior.Wait,
            WaitTimeout = TimeSpan.FromSeconds(10)
        };

        using var transaction = _connection!.BeginTransaction(options);

        try
        {
            _currentWriteTransaction = transaction;

            var statementIndex = 0;
            foreach (var sql in sqlStatements.Where(sql => !string.IsNullOrWhiteSpace(sql)))
            {
                statementIndex++;
                
                using var command = _connection.CreateCommand();
                
                command.Transaction = transaction;
                
                command.CommandText = sql;
                
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (FbException fbEx)
                {
                    var errorDetails = SqlErrorFormatter.FormatSqlError(fbEx, sql, statementIndex);
                    
                    throw new InvalidOperationException(errorDetails, fbEx);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Błąd podczas wykonywania statement #{statementIndex}: {ex.Message}";
                    
                    if (ex.InnerException != null)
                    {
                        errorMessage += $"\nSzczegóły: {ex.InnerException.Message}";
                    }
                    throw new InvalidOperationException(errorMessage, ex);
                }
            }
            
            validationCallback?.Invoke(this);
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            _currentWriteTransaction = null;
        }
    }

    public List<T> ExecuteRead<T>(string sql, Func<System.Data.IDataReader, T> mapper)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be empty", nameof(sql));

        if (mapper == null)
            throw new ArgumentNullException(nameof(mapper));

        EnsureConnection();
        
        var transactionToUse = _currentWriteTransaction;

        if (transactionToUse == null)
        {
            if (_readTransaction == null)
            {
                var options = new FbTransactionOptions
                {
                    TransactionBehavior = FbTransactionBehavior.Concurrency | 
                                          FbTransactionBehavior.Wait | 
                                          FbTransactionBehavior.Read
                };

                _readTransaction = _connection!.BeginTransaction(options);
            }

            transactionToUse = _readTransaction;
        }

        var results = new List<T>();

        using var command = _connection!.CreateCommand();
        
        command.Transaction = transactionToUse;
        
        command.CommandText = sql;

        try
        {
            using var reader = command.ExecuteReader();
            
            while (reader.Read())
            {
                results.Add(mapper(reader));
            }
        }
        catch (FbException fbEx)
        {
            var errorDetails = SqlErrorFormatter.FormatSqlError(fbEx, sql, 0);
            
            throw new InvalidOperationException(errorDetails, fbEx);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Błąd podczas wykonywania zapytania: {ex.Message}";
            
            if (ex.InnerException != null)
            {
                errorMessage += $"\nSzczegóły: {ex.InnerException.Message}";
            }
            
            throw new InvalidOperationException(errorMessage, ex);
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