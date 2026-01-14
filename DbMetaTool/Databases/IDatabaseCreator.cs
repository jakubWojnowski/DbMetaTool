namespace DbMetaTool.Databases;

public interface IDatabaseCreator
{
    DatabaseType DatabaseType { get; }
    
    void CreateDatabase(string databasePath);
}
