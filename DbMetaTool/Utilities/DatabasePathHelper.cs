namespace DbMetaTool.Utilities;

public static class DatabasePathHelper
{
    public static (string Directory, string FilePath) BuildDatabasePaths(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new ArgumentException("Database name cannot be empty", nameof(databaseName));
        }

        databaseName = databaseName.Replace(".fdb", "").Trim();

        var isUnixPath = databaseName.StartsWith("/");

        string databaseDirectory;
        
        string databaseFilePath;

        if (isUnixPath)
        {
            var lastSlashIndex = databaseName.LastIndexOf('/');
            if (lastSlashIndex > 0)
            {
                databaseDirectory = databaseName[..lastSlashIndex];
                
                var fileName = databaseName[(lastSlashIndex + 1)..];
                
                databaseFilePath = $"{databaseDirectory}/{fileName}.fdb";
            }
            else
            {
                databaseDirectory = "/var/lib/firebird/data";
                
                databaseFilePath = $"{databaseDirectory}/{databaseName}.fdb";
            }
        }
        else
        {
            var isAbsolutePath = Path.IsPathRooted(databaseName);
            
            if (isAbsolutePath)
            {
                databaseDirectory = Path.GetDirectoryName(databaseName) ?? ".";
                
                var fileName = Path.GetFileName(databaseName);
                
                databaseFilePath = Path.Combine(databaseDirectory, $"{fileName}.fdb");
            }
            else
            {
                databaseDirectory = Path.GetFullPath("./databases");
                
                databaseFilePath = Path.Combine(databaseDirectory, $"{databaseName}.fdb");
            }
            
            if (!Directory.Exists(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }
        }

        return (databaseDirectory, databaseFilePath);
    }
}

