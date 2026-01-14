using DbMetaTool.Databases;

namespace DbMetaTool.Features.Commands.UpdateDatabase;

public record UpdateDatabaseCommand(
    DatabaseType DatabaseType,
    string ConnectionString,
    string ScriptsDirectory
);