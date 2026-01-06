namespace DbMetaTool.Commands.ExportMetadata;

public record ExportMetadataCommand(string ConnectionString, string OutputDirectory);