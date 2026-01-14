using DbMetaTool.Models.results;

namespace DbMetaTool.Services.Build;

public interface IDatabaseBuildService
{
    BuildResult BuildDatabase(string databaseFilePath, string scriptsDirectory);
}
