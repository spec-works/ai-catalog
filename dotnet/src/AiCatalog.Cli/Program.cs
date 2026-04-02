using System.CommandLine;
using SpecWorks.AiCatalog.Cli.Commands;

var rootCommand = new RootCommand("AI Catalog CLI — convert, explore, and install AI artifacts");
rootCommand.AddCommand(ConvertCommand.Create());
rootCommand.AddCommand(ExploreCommand.Create());
rootCommand.AddCommand(InstallCommand.Create());

return await rootCommand.InvokeAsync(args);
