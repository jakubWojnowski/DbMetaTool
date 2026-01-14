using DbMetaTool.Models.results;

namespace DbMetaTool.Services.Build;

public interface IDatabaseBuildService
{
    Task<BuildResult> BuildDatabaseAsync(string databaseFilePath, string scriptsDirectory);
}
