namespace DbMetaTool.Features.Commands.BuildDatabase;

public record BuildDatabaseCommand(
    string DatabasePath,
    string ScriptsDirectory
);
