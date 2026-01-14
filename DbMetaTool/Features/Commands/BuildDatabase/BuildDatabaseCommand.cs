using DbMetaTool.Databases;

namespace DbMetaTool.Features.Commands.BuildDatabase;

public record BuildDatabaseCommand(
    DatabaseType DatabaseType,
    string DatabasePath,
    string ScriptsDirectory
);
