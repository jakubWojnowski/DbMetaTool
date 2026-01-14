using System.CommandLine;
using DbMetaTool.Features.Commands.BuildDatabase;
using DbMetaTool.Features.Commands.ExportMetadata;
using DbMetaTool.Features.Commands.UpdateDatabase;

namespace DbMetaTool.Features.Commands;

public static class Extensions
{
    public static RootCommand MapApplicationCommands(this RootCommand rootCommand, IServiceProvider serviceProvider)
    {
        return rootCommand
            .MapBuildDatabase(serviceProvider)
            .MapExportMetadata(serviceProvider)
            .MapUpdateDatabase(serviceProvider);
    }
}
