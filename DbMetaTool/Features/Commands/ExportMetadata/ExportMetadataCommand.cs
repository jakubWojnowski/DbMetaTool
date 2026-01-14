using DbMetaTool.Databases;

namespace DbMetaTool.Features.Commands.ExportMetadata;

public record ExportMetadataCommand(
    DatabaseType DatabaseType,
    string ConnectionString,
    string OutputDirectory
);