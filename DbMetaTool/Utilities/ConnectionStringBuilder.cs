namespace DbMetaTool.Utilities;

public static class ConnectionStringBuilder
{
    public static string Build(
        string database,
        string dataSource = "localhost",
        int port = 3050,
        string user = "SYSDBA",
        string password = "masterkey",
        string charset = "UTF8",
        int dialect = 3)
    {
        var parts = new List<string>
        {
            $"DataSource={dataSource}",
            $"Port={port}",
            $"Database={database}",
            $"User={user}",
            $"Password={password}",
            $"Charset={charset}",
            $"Dialect={dialect}"
        };

        return string.Join(";", parts);
    }

    public static string BuildForDocker(
        string databaseName,
        string dataSource = "localhost",
        int port = 3050,
        string user = "SYSDBA",
        string password = "masterkey",
        string charset = "UTF8",
        int dialect = 3)
    {
        var database = $"{dataSource}/{port}:{databaseName}";
        return Build(database, dataSource, port, user, password, charset, dialect);
    }

    public static bool IsValid(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        var requiredKeys = new[] { "Database" };
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var keys = parts.Select(p => p.Split('=')[0].Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return requiredKeys.All(key => keys.Contains(key));
    }
}
