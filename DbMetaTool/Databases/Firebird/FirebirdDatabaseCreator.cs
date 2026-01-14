using DbMetaTool.Configuration;
using DbMetaTool.Databases;
using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Databases.Firebird;

public class FirebirdDatabaseCreator : IDatabaseCreator
{
    
    public DatabaseType DatabaseType => DatabaseType.Firebird;

    public void CreateDatabase(string databasePath)
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

        var connectionString = connectionStringBuilder.ToString();

        if (DatabaseExists(connectionString))
        {
            throw new InvalidOperationException($"Baza danych '{databasePath}' już istnieje.");
        }

        FbConnection.CreateDatabase(connectionString, overwrite: false);
    }

    private static bool DatabaseExists(string connectionString)
    {
        try
        {
            using var connection = new FbConnection(connectionString);
            
            connection.Open();
            
            return true;
        }
        catch (FbException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }
}

