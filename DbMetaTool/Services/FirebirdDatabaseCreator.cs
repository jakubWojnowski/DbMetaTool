using FirebirdSql.Data.FirebirdClient;
using DbMetaTool.Configuration;

namespace DbMetaTool.Services;

public static class FirebirdDatabaseCreator
{
    public static void CreateDatabase(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path cannot be empty", nameof(databasePath));
        }

        // Sprawdź czy baza już istnieje
        if (File.Exists(databasePath))
        {
            throw new InvalidOperationException(
                $"Baza danych już istnieje: {databasePath}\n" +
                "Ze względów bezpieczeństwa nie można nadpisać istniejącej bazy.\n" +
                "Jeśli chcesz utworzyć nową bazę:\n" +
                "  1. Usuń istniejącą bazę ręcznie, lub\n" +
                "  2. Użyj innej nazwy/lokalizacji");
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

