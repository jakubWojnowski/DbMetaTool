using DbMetaTool.Models.results;

namespace DbMetaTool.Services.Export;

public static class ExportReportGenerator
{
    public static void DisplayReport(ExportResult result)
    {
        Console.WriteLine();
        Console.WriteLine("=== Podsumowanie ===");
        Console.WriteLine($"Katalog wyjściowy: {result.OutputDirectory}");
        Console.WriteLine($"Łącznie plików: {result.TotalFiles}");
        Console.WriteLine();
        Console.WriteLine("Skrypty zostały wyeksportowane pomyślnie.");
    }
}

