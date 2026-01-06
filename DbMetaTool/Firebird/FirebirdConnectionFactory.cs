using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Firebird;

public class FirebirdConnectionFactory
{
    private readonly string _connectionString;

    public FirebirdConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public FbConnection CreateConnection()
    {
        var connection = new FbConnection(_connectionString);
        return connection;
    }

    public async Task<FbConnection> CreateAndOpenConnectionAsync()
    {
        var connection = CreateConnection();
        await connection.OpenAsync();
        return connection;
    }

    public FbConnection CreateAndOpenConnection()
    {
        var connection = CreateConnection();
        connection.Open();
        return connection;
    }

    public static string BuildConnectionString(
        string dataSource,
        string database,
        string userId = "SYSDBA",
        string password = "masterkey",
        string charset = "UTF8",
        int serverType = 0)
    {
        var builder = new FbConnectionStringBuilder
        {
            DataSource = dataSource,
            Database = database,
            UserID = userId,
            Password = password,
            Charset = charset,
            ServerType = (FbServerType)serverType
        };

        return builder.ConnectionString;
    }

    public static string BuildConnectionStringForDocker(
        string databasePath,
        string userId = "SYSDBA",
        string password = "masterkey")
    {
        return BuildConnectionString(
            dataSource: "localhost",
            database: databasePath,
            userId: userId,
            password: password
        );
    }

    public static string BuildEmbeddedConnectionString(
        string databasePath,
        string userId = "SYSDBA",
        string password = "masterkey")
    {
        var builder = new FbConnectionStringBuilder
        {
            Database = databasePath,
            UserID = userId,
            Password = password,
            Charset = "UTF8",
            ServerType = FbServerType.Embedded
        };

        return builder.ConnectionString;
    }

    public static void CreateDatabase(string connectionString)
    {
        FbConnection.CreateDatabase(connectionString, overwrite: false);
    }

    public static void CreateDatabaseIfNotExists(string connectionString)
    {
        try
        {
            using var connection = new FbConnection(connectionString);
            connection.Open();
        }
        catch (FbException)
        {
            FbConnection.CreateDatabase(connectionString, overwrite: false);
        }
    }
}

