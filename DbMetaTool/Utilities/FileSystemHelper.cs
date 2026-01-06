using System.Text;

namespace DbMetaTool.Utilities;

public static class FileSystemHelper
{
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (Exception ex)
            {
                throw new DirectoryNotFoundException(
                    $"Nie można utworzyć katalogu: {directoryPath}. {ex.Message}", ex);
            }
        }
    }

    public static void WriteToFile(string filePath, string content, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            EnsureDirectoryExists(directory);
        }

        File.WriteAllText(filePath, content, encoding);
    }

    public static string ReadFromFile(string filePath, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return File.ReadAllText(filePath, encoding);
    }

    public static List<string> GetSqlFiles(string directoryPath, string searchPattern = "*.sql")
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Katalog nie istnieje: {directoryPath}");
        }

        return Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();
    }

    public static List<string> GetFilesByExtension(
        string directoryPath, 
        string extension, 
        bool recursive = true)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Katalog nie istnieje: {directoryPath}");
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var pattern = $"*{extension}";

        return Directory.GetFiles(directoryPath, pattern, searchOption)
            .OrderBy(f => f)
            .ToList();
    }
}
