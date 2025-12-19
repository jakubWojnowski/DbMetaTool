namespace DbMetaTool.Commands.BuildDatabase;

public record BuildDatabaseCommand(
    string DatabaseDirectory,
    string ScriptsDirectory
);