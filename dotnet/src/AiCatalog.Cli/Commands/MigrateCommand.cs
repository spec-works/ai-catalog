using System.CommandLine;
using System.CommandLine.Invocation;
using SpecWorks.AiCatalog.Cli.Conversion;
using SpecWorks.AiCatalog.Serialization;

namespace SpecWorks.AiCatalog.Cli.Commands;

/// <summary>
/// The <c>migrate</c> command — converts marketplace.json into AI Catalog format.
/// </summary>
public static class MigrateCommand
{
    /// <summary>
    /// Creates the <c>migrate</c> command.
    /// </summary>
    public static Command Create()
    {
        return Create("migrate", "Migrate a marketplace.json to AI Catalog format");
    }

    private static Command Create(string name, string description)
    {
        var inputArgument = new Argument<FileInfo>("input", "Path to marketplace.json file");
        var outputOption = new Option<FileInfo?>("--output", "Output file path (defaults to stdout)");
        outputOption.AddAlias("-o");

        var cmd = new Command(name, description)
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

                var catalog = MarketplaceConverter.Convert(stream, packaging: null);
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
