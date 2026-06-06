using System.CommandLine;
using SpecWorks.AiCatalog.Cli.Commands;

var rootCommand = new RootCommand("AI Catalog CLI — migrate, export, publish, explore, and install AI artifacts");
rootCommand.AddCommand(MigrateCommand.Create());
rootCommand.AddCommand(ExportCommand.Create());
rootCommand.AddCommand(PublishCommand.Create());
rootCommand.AddCommand(ExploreCommand.Create());
rootCommand.AddCommand(InstallCommand.Create());

return await rootCommand.InvokeAsync(args);
