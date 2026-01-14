using DbMetaTool.Databases;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Databases.Firebird;


public class FirebirdSqlExecutor
(
    string connectionString
) : ISqlExecutor
{
    
    public DatabaseType DatabaseType => DatabaseType.Firebird;

    private FbConnection? _connection;
    private FbTransaction? _readTransaction;
    private FbTransaction? _currentWriteTransaction;
    private bool _disposed;

    public async Task ExecuteBatchAsync(
        List<string> sqlStatements, 
        Func<ISqlExecutor, Task>? validationCallback = null)
    {
        if (sqlStatements is null)
            throw new ArgumentNullException(nameof(sqlStatements));

        if (sqlStatements.Count == 0)
            return;

        await EnsureConnectionAsync();
        
        if (_readTransaction != null)
        {
            await _readTransaction.DisposeAsync();
            
            _readTransaction = null;
        }

        var options = new FbTransactionOptions
        {
            TransactionBehavior = FbTransactionBehavior.Concurrency | 
                                  FbTransactionBehavior.Wait,
            WaitTimeout = TimeSpan.FromSeconds(10)
        };

        var transaction = await _connection!.BeginTransactionAsync(options);

        try
        {
            _currentWriteTransaction = transaction;

            var statementIndex = 0;
            foreach (var sql in sqlStatements.Where(sql => !string.IsNullOrWhiteSpace(sql)))
            {
                statementIndex++;

                await using var command = _connection.CreateCommand();
                
                command.Transaction = transaction;
                
                command.CommandText = sql;
                
                try
                {
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    var errorDetails = FirebirdSqlErrorFormatter.FormatSqlError(ex, sql, statementIndex);
                    
                    throw new InvalidOperationException(errorDetails, ex);
                }
            }
            
            if (validationCallback != null)
            {
                await validationCallback(this);
            }
            
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            _currentWriteTransaction = null;
            await transaction.DisposeAsync();
        }
    }

    public async Task<List<T>> ExecuteReadAsync<T>(string sql, Func<System.Data.IDataReader, T> mapper)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be empty", nameof(sql));

        if (mapper == null)
            throw new ArgumentNullException(nameof(mapper));

        await EnsureConnectionAsync();
        
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

                _readTransaction = await _connection!.BeginTransactionAsync(options);
            }

            transactionToUse = _readTransaction;
        }

        var results = new List<T>();

        await using var command = _connection!.CreateCommand();
        
        command.Transaction = transactionToUse;
        
        command.CommandText = sql;

        try
        {
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }
        }
        catch (Exception ex)
        {
            var errorDetails = FirebirdSqlErrorFormatter.FormatSqlError(ex, sql, 0);
            
            throw new InvalidOperationException(errorDetails, ex);
        }

        return results;
    }

    private async Task EnsureConnectionAsync()
    {
        if (_connection == null)
        {
            var connection = await FirebirdConnectionFactory.CreateAndOpenConnectionAsync(connectionString);
            _connection = connection as FbConnection 
                ?? throw new InvalidOperationException("Connection factory must return FbConnection for Firebird");
        }
        else if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync();
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