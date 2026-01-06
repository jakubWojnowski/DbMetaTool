using DbMetaTool.Models.results;
using DbMetaTool.Services;

namespace DbMetaTool.Services;

public static class BuildReportGenerator
{
    public static void DisplayReport(BuildResult result)
    {
        Console.WriteLine();
        Console.WriteLine("=== Podsumowanie ===");
        Console.WriteLine($"Wykonano pomyślnie: {result.ExecutedCount}");
        Console.WriteLine();
        Console.WriteLine("Connection String:");
        Console.WriteLine(result.ConnectionString);
    }
}

