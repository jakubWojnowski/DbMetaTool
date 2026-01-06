namespace DbMetaTool.Tests.TestHelpers;

public static class FirebirdDatabaseCreatorStub
{
    private static bool _shouldThrowOnCreate;
    private static string? _existingDatabasePath;

    public static void Reset()
    {
        _shouldThrowOnCreate = false;
        _existingDatabasePath = null;
    }

    public static void SetExistingDatabase(string databasePath)
    {
        _existingDatabasePath = databasePath;
    }

    public static void CreateDatabaseStub(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path cannot be empty", nameof(databasePath));
        }

        if (_existingDatabasePath != null && 
            databasePath.Equals(_existingDatabasePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Baza danych '{databasePath}' już istnieje.");
        }

        if (_shouldThrowOnCreate)
        {
            throw new InvalidOperationException("Database creation failed");
        }

        Console.WriteLine($"[STUB] Utworzono bazę danych: {databasePath}");
    }
}

