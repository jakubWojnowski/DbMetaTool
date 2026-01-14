using System.Data;
using DbMetaTool.Configuration;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Databases.Firebird;

public static class FirebirdConnectionFactory
{
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

    public static async Task<IDbConnection> CreateAndOpenConnectionAsync(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));

        var connection = new FbConnection(connectionString);
        
        await connection.OpenAsync();
        
        return connection;
    }
}

