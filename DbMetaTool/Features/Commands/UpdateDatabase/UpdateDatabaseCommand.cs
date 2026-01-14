namespace DbMetaTool.Features.Commands.UpdateDatabase;

public record UpdateDatabaseCommand(string ConnectionString, string ScriptsDirectory);