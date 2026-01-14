using DbMetaTool.Models.results;

namespace DbMetaTool.Services.Build;

public class BuildReportGenerator : IBuildReportGenerator
{
    public void DisplayReport(BuildResult result)
    {
        Console.WriteLine();
        Console.WriteLine("=== Podsumowanie ===");
        Console.WriteLine($"Wykonano pomyślnie: {result.ExecutedCount}");
    }
}

