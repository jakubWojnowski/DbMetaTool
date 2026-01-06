namespace DbMetaTool.Commands.UpdateDatabase;

public record UpdateDatabaseCommand(string ConnectionString, string ScriptsDirectory);