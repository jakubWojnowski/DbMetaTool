using System;
using DbMetaTool.Firebird;

namespace DbMetaTool.Commands
{
    public class UpdateDatabaseCommand
    {
        public static void Execute(string connectionString, string scriptsDirectory)
        {
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
}

