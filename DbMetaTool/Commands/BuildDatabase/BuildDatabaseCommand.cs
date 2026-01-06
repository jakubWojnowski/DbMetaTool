namespace DbMetaTool.Commands.BuildDatabase;

public record BuildDatabaseCommand(
    string DatabasePath,
    string ScriptsDirectory
);