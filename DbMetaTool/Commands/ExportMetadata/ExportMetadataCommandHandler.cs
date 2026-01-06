using DbMetaTool.Firebird;
using DbMetaTool.Services;

namespace DbMetaTool.Commands.ExportMetadata;

public static class ExportMetadataCommandHandler
{
    public static void Handle(ExportMetadataCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        Console.WriteLine("=== Eksport metadanych z bazy Firebird ===");
        Console.WriteLine();
        Console.WriteLine($"Connection String: {command.ConnectionString}");
        Console.WriteLine($"Katalog wyjściowy: {command.OutputDirectory}");
        Console.WriteLine();

        var connectionFactory = new FirebirdConnectionFactory(command.ConnectionString);
        using var sqlExecutor = new FirebirdSqlExecutor(connectionFactory);

        var result = MetadataExportService.ExportAll(sqlExecutor, command.OutputDirectory);

        ExportReportGenerator.DisplayReport(result);
    }
}
