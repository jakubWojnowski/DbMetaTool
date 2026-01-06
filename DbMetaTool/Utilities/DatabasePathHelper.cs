namespace DbMetaTool.Utilities;

public static class DatabasePathHelper
{
    public static (string Directory, string FilePath) BuildDatabasePaths(string databaseDirectory)
    {
        if (string.IsNullOrWhiteSpace(databaseDirectory))
            throw new ArgumentException("Database directory cannot be empty", nameof(databaseDirectory));

        var isUnixPath = databaseDirectory.StartsWith("/");

        string absoluteDatabaseDirectory;
        string databaseFilePath;

        if (isUnixPath)
        {
            absoluteDatabaseDirectory = databaseDirectory.TrimEnd('/');
            var databaseName = Path.GetFileName(absoluteDatabaseDirectory);
            databaseFilePath = $"{absoluteDatabaseDirectory}/{databaseName}.fdb";
        }
        else
        {
            absoluteDatabaseDirectory = Path.GetFullPath(databaseDirectory);
            
            if (!Directory.Exists(absoluteDatabaseDirectory))
            {
                Directory.CreateDirectory(absoluteDatabaseDirectory);
            }

            var databaseName = Path.GetFileName(absoluteDatabaseDirectory);
            databaseFilePath = Path.Combine(absoluteDatabaseDirectory, $"{databaseName}.fdb");
        }

        return (absoluteDatabaseDirectory, databaseFilePath);
    }
}

