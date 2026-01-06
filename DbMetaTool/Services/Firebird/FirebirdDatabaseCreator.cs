using DbMetaTool.Configuration;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Services.Firebird;

public static class FirebirdDatabaseCreator
{
    public static void CreateDatabase(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path cannot be empty", nameof(databasePath));
        }

        var connectionStringBuilder = new FbConnectionStringBuilder
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

       FbConnection.CreateDatabase(connectionStringBuilder.ToString(), overwrite: false);
    }
}

