using System.CommandLine;
using System.CommandLine.Invocation;
using SpecWorks.AiCatalog.Cli.Conversion;
using SpecWorks.AiCatalog.Serialization;

namespace SpecWorks.AiCatalog.Cli.Commands;

/// <summary>
/// The <c>convert</c> command group with subcommands for converting between formats.
/// </summary>
public static class ConvertCommand
{
    /// <summary>
    /// Creates the <c>convert</c> command with its subcommands.
    /// </summary>
    public static Command Create()
    {
        var convertCommand = new Command("convert", "Convert between AI artifact catalog formats");
        convertCommand.AddCommand(CreateMarketplaceCommand());
        return convertCommand;
    }

    private static Command CreateMarketplaceCommand()
    {
        var inputArgument = new Argument<FileInfo>("input-file", "Path to marketplace.json file");
        var outputOption = new Option<FileInfo?>("--output", "Output file path (defaults to stdout)");
        outputOption.AddAlias("-o");

        var cmd = new Command("marketplace", "Convert a Claude marketplace.json to ai-catalog.json")
        {
            inputArgument,
            outputOption
        };

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var inputFile = context.ParseResult.GetValueForArgument(inputArgument);
            var outputFile = context.ParseResult.GetValueForOption(outputOption);

            if (!inputFile.Exists)
            {
                Console.Error.WriteLine($"Error: file not found: {inputFile.FullName}");
                context.ExitCode = 1;
                return;
            }

            try
            {
                using var stream = inputFile.OpenRead();
                var catalog = MarketplaceConverter.Convert(stream);
                var json = AiCatalogSerializer.Serialize(catalog);

                if (outputFile != null)
                {
                    await File.WriteAllTextAsync(outputFile.FullName, json);
                    Console.WriteLine($"Converted {catalog.Entries.Count} entries to {outputFile.FullName}");
                }
                else
                {
                    Console.WriteLine(json);
                }
            }
            catch (AiCatalogException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });

        return cmd;
    }
}
