using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Firebird;

public class FirebirdConnectionFactory
{
    private readonly string _connectionString;

    public FirebirdConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public FbConnection CreateAndOpenConnection()
    {
        var connection = new FbConnection(_connectionString);
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

    public static void CreateDatabase(string connectionString, bool overwrite = false)
    {
        FbConnection.CreateDatabase(connectionString, overwrite: overwrite);
    }
}

