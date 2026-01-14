namespace DbMetaTool.Databases;

public interface IDatabaseStrategyService
{
    IDatabaseCreator GetDatabaseCreator(DatabaseType databaseType);
    
    ISqlExecutor GetSqlExecutor(DatabaseType databaseType, string connectionStringOrPath);
}
