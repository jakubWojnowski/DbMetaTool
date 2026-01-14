using DbMetaTool.Models.results;

namespace DbMetaTool.Services.Build;

public interface IBuildReportGenerator
{
    void DisplayReport(BuildResult result);
}
