using DbMetaTool.Models.results;

namespace DbMetaTool.Services.Build;

public static class BuildReportGenerator
{
    public static void DisplayReport(BuildResult result)
    {
        Console.WriteLine();
        Console.WriteLine("=== Podsumowanie ===");
        Console.WriteLine($"Wykonano pomyślnie: {result.ExecutedCount}");
    }
}

