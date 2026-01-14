namespace DbMetaTool.Features.Commands.ExportMetadata;

public record ExportMetadataCommand(string ConnectionString, string OutputDirectory);