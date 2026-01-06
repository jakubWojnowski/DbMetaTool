using DbMetaTool.Configuration;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Firebird;

public class FirebirdConnectionFactory
{
    private readonly string _connectionString;

    public FirebirdConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public static string BuildConnectionString(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path cannot be empty", nameof(databasePath));

        var builder = new FbConnectionStringBuilder
        {
            DataSource = DatabaseConfiguration.DefaultDataSource,
            Port = DatabaseConfiguration.DefaultPort,
            Database = databasePath,
            UserID = DatabaseConfiguration.DefaultUserId,
            Password = DatabaseConfiguration.DefaultPassword,
            Charset = DatabaseConfiguration.DefaultCharset,
            ServerType = FbServerType.Default,
            Dialect = 3
        };

        return builder.ToString();
    }

    public FbConnection CreateAndOpenConnection()
    {
        var connection = new FbConnection(_connectionString);
        
        connection.Open();
        
        return connection;
    }
}

