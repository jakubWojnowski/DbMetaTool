using DbMetaTool.Firebird;

namespace DbMetaTool.Commands.UpdateDatabase;

public static class UpdateDatabaseCommandHandler
{
    public static void Handle(UpdateDatabaseCommand command)
    {
        var (connectionString, scriptsDirectory) = command;
        
        var connectionFactory = new FirebirdConnectionFactory(connectionString);
        var databaseUpdater = new DatabaseUpdater(connectionFactory);

        var report = databaseUpdater.UpdateFromScripts(scriptsDirectory);
        report.PrintReport();

        if (report.HasErrors())
        {
            throw new Exception("Aktualizacja zakończona z błędami.");
        }
    }
}