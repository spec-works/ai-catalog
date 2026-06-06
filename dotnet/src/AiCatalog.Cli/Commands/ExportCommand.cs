using System.CommandLine;
using System.CommandLine.Invocation;
using SpecWorks.AiCatalog.Cli.Conversion;

namespace SpecWorks.AiCatalog.Cli.Commands;

/// <summary>
/// The <c>export</c> command — projects an AI Catalog into platform-specific formats.
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
        return Create("export", "Export an AI Catalog to platform-specific plugin formats", httpClient);
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
        var githubOption = new Option<bool>("--github", "Generate GitHub Copilot CLI marketplace output");
        var codexOption = new Option<bool>("--codex", "Generate OpenAI Codex CLI marketplace output");
        var claudeOption = new Option<bool>("--claude", "Generate Claude Code plugin marketplace output");

        var cmd = new Command(name, description)
        {
            inputArgument,
            outputOption,
            sourceDirOption,
            githubOption,
            codexOption,
            claudeOption
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

            var generateGitHub = context.ParseResult.GetValueForOption(githubOption);
            var generateCodex = context.ParseResult.GetValueForOption(codexOption);
            var generateClaude = context.ParseResult.GetValueForOption(claudeOption);

            if (!inputFile.Exists)
            {
                Console.Error.WriteLine($"Error: file not found: {inputFile.FullName}");
                context.ExitCode = 1;
                return;
            }

            if (!generateGitHub && !generateCodex && !generateClaude)
            {
                generateGitHub = true;
                generateCodex = true;
                generateClaude = true;
            }

            var resolvedSourceDirectory = sourceDirectory?.FullName
                ?? inputFile.Directory?.FullName
                ?? Directory.GetCurrentDirectory();

            try
            {
                var catalog = await CatalogProjector.LoadCatalogAsync(inputFile.FullName);
                var client = httpClient ?? new HttpClient();
                var projectors = new List<CatalogProjector>();

                if (generateGitHub)
                {
                    projectors.Add(new GitHubProjector(catalog, inputFile.FullName, outputDirectory.FullName, resolvedSourceDirectory, client));
                }

                if (generateCodex)
                {
                    projectors.Add(new CodexProjector(catalog, inputFile.FullName, outputDirectory.FullName, resolvedSourceDirectory, client));
                }

                if (generateClaude)
                {
                    projectors.Add(new ClaudeProjector(catalog, inputFile.FullName, outputDirectory.FullName, resolvedSourceDirectory, client));
                }

                foreach (var projector in projectors)
                {
                    await projector.ProjectAsync();
                }

                Console.WriteLine($"Generated {string.Join(", ", projectors.Select(p => p.PlatformName))} output in {outputDirectory.FullName}");
            }
            catch (Exception ex) when (ex is AiCatalogException or HttpRequestException or IOException)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });

        return cmd;
    }
}
