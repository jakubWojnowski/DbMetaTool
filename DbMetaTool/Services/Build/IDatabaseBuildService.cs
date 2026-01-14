using DbMetaTool.Databases;
using DbMetaTool.Models.results;

namespace DbMetaTool.Services.Build;

public interface IDatabaseBuildService
{
    Task<BuildResult> BuildDatabaseAsync(
        DatabaseType databaseType,
        string databaseFilePath,
        string scriptsDirectory);
}
