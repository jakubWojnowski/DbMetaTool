using DbMetaTool.Models;

namespace DbMetaTool.Services.Update;

public interface IUpdateReportGenerator
{
    void DisplayReport(List<DatabaseChange> changes);
}
