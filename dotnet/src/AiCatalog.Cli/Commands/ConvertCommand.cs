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
        return Create(null);
    }

    /// <summary>
    /// Creates the <c>convert</c> command with an optional custom <see cref="HttpClient"/>.
    /// </summary>
    internal static Command Create(HttpClient? httpClient)
    {
        var convertCommand = new Command("convert", "Convert between AI artifact catalog formats");
        convertCommand.AddCommand(CreateMarketplaceCommand());
        convertCommand.AddCommand(CreateCatalogCommand(httpClient));
        return convertCommand;
    }

    private static Command CreateMarketplaceCommand()
    {
        var inputArgument = new Argument<FileInfo>("input-file", "Path to marketplace.json file");
        var outputOption = new Option<FileInfo?>("--output", "Output file path (defaults to stdout)");
        outputOption.AddAlias("-o");

        var cmd = new Command("marketplace", "Convert a marketplace.json to ai-catalog.json, packaging skills as zip files")
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

                MarketplaceConverter.PackagingOptions? packaging = null;
                if (outputFile != null)
                {
                    var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputFile.FullName))
                        ?? Directory.GetCurrentDirectory();
                    packaging = new MarketplaceConverter.PackagingOptions
                    {
                        SourceDir = inputFile.Directory?.FullName,
                        OutputDir = outputDir
                    };
                }

                var catalog = MarketplaceConverter.Convert(stream, packaging);
                var json = AiCatalogSerializer.Serialize(catalog);

                if (outputFile != null)
                {
                    await File.WriteAllTextAsync(outputFile.FullName, json);
                    Console.WriteLine($"Converted {catalog.Entries.Count} entries to {outputFile.FullName}");

                    var skillsDir = Path.Combine(
                        Path.GetDirectoryName(Path.GetFullPath(outputFile.FullName)) ?? ".",
                        "skills");
                    if (Directory.Exists(skillsDir))
                    {
                        var zips = Directory.GetFiles(skillsDir, "*.zip");
                        if (zips.Length > 0)
                        {
                            Console.WriteLine($"Packaged {zips.Length} skill(s) to {skillsDir}/");
                        }
                    }
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

    private static Command CreateCatalogCommand(HttpClient? httpClient)
    {
        var inputArgument = new Argument<FileInfo>("input-file", "Path to ai-catalog.json file");
        var outputOption = new Option<DirectoryInfo>("--output", "Output directory for generated plugin marketplace files")
        {
            IsRequired = true
        };
        outputOption.AddAlias("-o");

        var sourceDirOption = new Option<DirectoryInfo?>("--source-dir", "Base directory for resolving relative plugin source paths (defaults to the input file directory)");
        var githubOption = new Option<bool>("--github", "Generate GitHub Copilot CLI marketplace output");
        var codexOption = new Option<bool>("--codex", "Generate OpenAI Codex CLI marketplace output");
        var claudeOption = new Option<bool>("--claude", "Generate Claude Code plugin marketplace output");

        var cmd = new Command("catalog", "Convert an ai-catalog.json file into platform-specific plugin marketplace formats")
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
