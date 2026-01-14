using DbMetaTool.Models;

namespace DbMetaTool.Services.Update;

public class UpdateReportGenerator : IUpdateReportGenerator
{
    public void DisplayReport(List<DatabaseChange> changes)
    {
        Console.WriteLine("=== Raport zmian ===");
        Console.WriteLine();

        var groupedChanges = changes.GroupBy(c => c.Type);

        foreach (var group in groupedChanges.OrderBy(g => g.Key))
        {
            Console.WriteLine($"{group.Key}:");
            foreach (var change in group)
            {
                if (string.IsNullOrWhiteSpace(change.Details))
                {
                    Console.WriteLine($"  - {change.ObjectName}");
                }
                else
                {
                    Console.WriteLine($"  - {change.ObjectName}: {change.Details}");
                }
            }
            Console.WriteLine();
        }

        var stats = new
        {
            DomainsCreated = changes.Count(c => c.Type == ChangeType.DomainCreated),
            TablesCreated = changes.Count(c => c.Type == ChangeType.TableCreated),
            ColumnsAdded = changes.Count(c => c.Type == ChangeType.ColumnAdded),
            ProceduresModified = changes.Count(c => c.Type == ChangeType.ProcedureModified),
            ManualReview = changes.Count(c => c.Type == ChangeType.ManualReviewRequired)
        };

        Console.WriteLine("Podsumowanie:");
        Console.WriteLine($"  Domeny utworzone: {stats.DomainsCreated}");
        Console.WriteLine($"  Tabele utworzone: {stats.TablesCreated}");
        Console.WriteLine($"  Kolumny dodane: {stats.ColumnsAdded}");
        Console.WriteLine($"  Procedury zmodyfikowane: {stats.ProceduresModified}");
        Console.WriteLine($"  Wymaga przeglądu manualnego: {stats.ManualReview}");
    }
}

