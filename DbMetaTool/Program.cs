using System.CommandLine;
using DbMetaTool.Services;
using Microsoft.Extensions.Hosting;

// Przykładowe wywołania:
// DbMetaTool build-db --db-dir "C:\db\fb5" --scripts-dir "C:\scripts"
// DbMetaTool export-scripts --connection-string "..." --output-dir "C:\out"
// DbMetaTool update-db --connection-string "..." --scripts-dir "C:\scripts"

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbMetaToolServices();

var host = builder.Build();

var serviceProvider = host.Services;

var rootCommand = new RootCommand("Narzędzie do zarządzania metadanymi bazy danych Firebird");

rootCommand.RegisterCommands(serviceProvider);

return rootCommand.Parse(args).Invoke();
