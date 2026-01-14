using DbMetaTool.Services.Build;
using DbMetaTool.Utilities;

namespace DbMetaTool.Features.Commands.BuildDatabase;

public class BuildDatabaseCommandHandler(
    IDatabaseBuildService buildService,
    IBuildReportGenerator reportGenerator) : IAsyncHandler<BuildDatabaseCommand, BuildDatabaseResponse>
{
    public Task<BuildDatabaseResponse> HandleAsync(
        BuildDatabaseCommand request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("=== Budowanie bazy danych Firebird ===");
            Console.WriteLine();

            var (databaseDirectory, databaseFilePath) = DatabasePathHelper.BuildDatabasePaths(request.DatabasePath);

            Console.WriteLine($"Katalog bazy: {databaseDirectory}");
            Console.WriteLine($"Plik bazy: {databaseFilePath}");
            Console.WriteLine($"Katalog skryptów: {request.ScriptsDirectory}");
            Console.WriteLine();

            var result = buildService.BuildDatabase(databaseFilePath, request.ScriptsDirectory);

            reportGenerator.DisplayReport(result);

            Console.WriteLine("Baza danych została zbudowana pomyślnie.");

            return Task.FromResult(new BuildDatabaseResponse(Success: true));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
            return Task.FromResult(new BuildDatabaseResponse(Success: false, ErrorMessage: ex.Message));
        }
    }
}
