using DbMetaTool.Databases.Firebird;

namespace DbMetaTool.Databases;

public class DatabaseStrategyService : IDatabaseStrategyService
{
    private readonly Dictionary<DatabaseType, IDatabaseCreator> _databaseCreators;

    public DatabaseStrategyService(IEnumerable<IDatabaseCreator> databaseCreators)
    {
        _databaseCreators = databaseCreators.ToDictionary(dc => dc.DatabaseType);
    }

    public IDatabaseCreator GetDatabaseCreator(DatabaseType databaseType)
    {
        if (!_databaseCreators.TryGetValue(databaseType, out var creator))
        {
            throw new NotSupportedException($"Database type {databaseType} is not supported");
        }

        return creator;
    }

    public ISqlExecutor GetSqlExecutor(DatabaseType databaseType, string connectionStringOrPath)
    {
        string connectionString;

        if (IsConnectionString(connectionStringOrPath))
        {
            connectionString = connectionStringOrPath;
        }
        else
        {
            connectionString = BuildConnectionStringInternal(databaseType, connectionStringOrPath);
        }

        return CreateSqlExecutorInternal(databaseType, connectionString);
    }

    private static bool IsConnectionString(string connectionStringOrPath)
    {
        if (string.IsNullOrWhiteSpace(connectionStringOrPath))
        {
            return false;
        }

        return connectionStringOrPath.Contains('=') && 
               (connectionStringOrPath.Contains("DataSource=", StringComparison.OrdinalIgnoreCase) ||
                connectionStringOrPath.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                connectionStringOrPath.Contains("Host=", StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildConnectionStringInternal(DatabaseType databaseType, string databasePath)
    {
        return databaseType switch
        {
            DatabaseType.Firebird => FirebirdConnectionFactory.BuildConnectionString(databasePath),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported")
        };
    }

    private static ISqlExecutor CreateSqlExecutorInternal(
        DatabaseType databaseType,
        string connectionString)
    {
        return databaseType switch
        {
            DatabaseType.Firebird => new FirebirdSqlExecutor(connectionString),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported")
        };
    }
}
//TO DO: Add support for other databases
// trzeba zmienic implementacje tego serwisu nie switch a first or default 
// blr validator ma implementacje stricte firebird. tez trzeba nalezy strategie utworzyc i implmentacje dla wielu baz