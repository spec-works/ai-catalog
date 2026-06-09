using System.CommandLine;
using System.CommandLine.Invocation;
using SpecWorks.AiCatalog.Cli.Conversion;

namespace SpecWorks.AiCatalog.Cli.Commands;

/// <summary>
/// The <c>export</c> command — projects an AI Catalog into the unified plugin layout.
/// </summary>
public static class ExportCommand
{
    /// <summary>
    /// Creates the <c>export</c> command.
    /// </summary>
    public static Command Create()
    {
        return Create(null);
    }

    /// <summary>
    /// Creates the <c>export</c> command with an optional custom <see cref="HttpClient"/>.
    /// </summary>
    internal static Command Create(HttpClient? httpClient)
    {
        return Create("export", "Export an AI Catalog to the unified plugin marketplace layout", httpClient);
    }

    private static Command Create(string name, string description, HttpClient? httpClient)
    {
        var inputArgument = new Argument<FileInfo>("input", "Path to ai-catalog.json file");
        var outputOption = new Option<DirectoryInfo>("--output", "Output directory for generated plugin marketplace files")
        {
            IsRequired = true
        };
        outputOption.AddAlias("-o");

        var sourceDirOption = new Option<DirectoryInfo?>("--source-dir", "Base directory for resolving relative plugin source paths (defaults to the input file directory)");

        var cmd = new Command(name, description)
        {
            inputArgument,
            outputOption,
            sourceDirOption,
        };

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var inputFile = context.ParseResult.GetValueForArgument(inputArgument);
            var outputDirectory = context.ParseResult.GetValueForOption(outputOption);
            var sourceDirectory = context.ParseResult.GetValueForOption(sourceDirOption);

            if (outputDirectory == null)
            {
                Console.Error.WriteLine("Error: output directory is required");
                context.ExitCode = 1;
                return;
            }

            if (!inputFile.Exists)
            {
                Console.Error.WriteLine($"Error: file not found: {inputFile.FullName}");
                context.ExitCode = 1;
                return;
            }

            var resolvedSourceDirectory = sourceDirectory?.FullName
                ?? inputFile.Directory?.FullName
                ?? Directory.GetCurrentDirectory();

            HttpClient? createdClient = null;

            try
            {
                var catalog = await CatalogProjector.LoadCatalogAsync(inputFile.FullName);
                var client = httpClient ?? (createdClient = new HttpClient());
                var projector = new UnifiedProjector(catalog, inputFile.FullName, outputDirectory.FullName, resolvedSourceDirectory, client);

                await projector.ProjectAsync();

                Console.WriteLine($"Generated unified plugin output in {outputDirectory.FullName}");
            }
            catch (Exception ex) when (ex is AiCatalogException or HttpRequestException or IOException)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
            finally
            {
                createdClient?.Dispose();
            }
        });

        return cmd;
    }
}
