namespace DbMetaTool.Tests.TestHelpers;

public class TestDirectoryHelper : IDisposable
{
    private readonly string _baseDirectory;
    private readonly List<string> _createdDirectories = [];
    private readonly List<string> _createdFiles = [];

    public TestDirectoryHelper()
    {
        _baseDirectory = Path.Combine(Path.GetTempPath(), "DbMetaToolTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_baseDirectory);
    }

    public string CreateScriptsDirectory()
    {
        var scriptsDir = Path.Combine(_baseDirectory, "scripts");
        Directory.CreateDirectory(scriptsDir);
        _createdDirectories.Add(scriptsDir);
        
        var domainsDir = Path.Combine(scriptsDir, "domains");
        var tablesDir = Path.Combine(scriptsDir, "tables");
        var proceduresDir = Path.Combine(scriptsDir, "procedures");
        
        Directory.CreateDirectory(domainsDir);
        Directory.CreateDirectory(tablesDir);
        Directory.CreateDirectory(proceduresDir);
        
        return scriptsDir;
    }

    public string CreateDatabaseDirectory()
    {
        var dbDir = Path.Combine(_baseDirectory, "database");
        Directory.CreateDirectory(dbDir);
        _createdDirectories.Add(dbDir);
        return dbDir;
    }

    public void CreateScriptFile(string scriptsDirectory, string subdirectory, string fileName, string content)
    {
        var fullPath = Path.Combine(scriptsDirectory, subdirectory, fileName);
        File.WriteAllText(fullPath, content);
        _createdFiles.Add(fullPath);
    }

    public void Dispose()
    {
        foreach (var file in _createdFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        foreach (var dir in _createdDirectories.OrderByDescending(d => d.Length))
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        try
        {
            if (Directory.Exists(_baseDirectory))
            {
                Directory.Delete(_baseDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

