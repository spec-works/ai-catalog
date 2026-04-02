using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using System.Text.Json.Nodes;
using SpecWorks.AiCatalog.Models;
using SpecWorks.AiCatalog.Parsing;

namespace SpecWorks.AiCatalog.Cli.Commands;

/// <summary>
/// The <c>install</c> command — downloads and enables an artifact from a catalog.
/// </summary>
public static class InstallCommand
{
    private const string McpConfigMediaType = "application/vnd.mcp.server+json";
    private const string SkillsDirectory = ".ai-catalog/skills";
    private const string McpConfigPath = ".ai-catalog/mcp-config.json";

    /// <summary>
    /// Creates the <c>install</c> command.
    /// </summary>
    public static Command Create()
    {
        return Create(null);
    }

    /// <summary>
    /// Creates the <c>install</c> command with an optional custom HttpClient (for testing).
    /// </summary>
    internal static Command Create(HttpClient? httpClient)
    {
        var catalogUrlArgument = new Argument<string>("catalog-url", "URL of the AI Catalog");
        var entryIdArgument = new Argument<string>("entry-id", "Identifier of the entry to install");
        var typeOption = new Option<string?>("--type", "Artifact type hint: mcp or skill (auto-detected from mediaType if omitted)");
        var outputDirOption = new Option<string?>("--output-dir", "Base directory for installation (defaults to current directory)");

        var cmd = new Command("install", "Install an artifact from an AI Catalog")
        {
            catalogUrlArgument,
            entryIdArgument,
            typeOption,
            outputDirOption
        };

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var catalogUrl = context.ParseResult.GetValueForArgument(catalogUrlArgument);
            var entryId = context.ParseResult.GetValueForArgument(entryIdArgument);
            var type = context.ParseResult.GetValueForOption(typeOption);
            var outputDir = context.ParseResult.GetValueForOption(outputDirOption) ?? ".";

            try
            {
                var client = httpClient ?? new HttpClient();
                var catalog = await FetchCatalogAsync(client, catalogUrl);
                var entry = catalog.Entries.FirstOrDefault(e =>
                    string.Equals(e.Identifier, entryId, StringComparison.Ordinal));

                if (entry == null)
                {
                    Console.Error.WriteLine($"Error: entry '{entryId}' not found in catalog");
                    context.ExitCode = 1;
                    return;
                }

                var artifactType = ResolveType(entry, type);
                await InstallEntryAsync(client, entry, artifactType, outputDir);
            }
            catch (Exception ex) when (ex is HttpRequestException or AiCatalogException or IOException)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                context.ExitCode = 1;
            }
        });

        return cmd;
    }

    private static async Task<Models.AiCatalog> FetchCatalogAsync(HttpClient client, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new AiCatalogException($"Invalid URL: {url}");
        }

        var response = await client.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return AiCatalogParser.Parse(json);
    }

    private static string ResolveType(CatalogEntry entry, string? typeHint)
    {
        if (typeHint != null)
        {
            return typeHint.ToLowerInvariant();
        }

        // Auto-detect based on mediaType
        if (entry.MediaType.Contains("mcp", StringComparison.OrdinalIgnoreCase))
        {
            return "mcp";
        }

        return "skill";
    }

    private static async Task InstallEntryAsync(HttpClient client, CatalogEntry entry, string type, string outputDir)
    {
        Console.WriteLine($"Installing: {entry.DisplayName} ({entry.Identifier})");
        Console.WriteLine($"Type: {type}");

        if (type == "mcp")
        {
            await InstallMcpEntry(client, entry, outputDir);
        }
        else
        {
            await InstallSkillEntry(client, entry, outputDir);
        }
    }

    private static async Task InstallMcpEntry(HttpClient client, CatalogEntry entry, string outputDir)
    {
        // Generate an MCP config snippet
        var configPath = Path.Combine(outputDir, McpConfigPath);
        var configDir = Path.GetDirectoryName(configPath)!;
        Directory.CreateDirectory(configDir);

        // Build the MCP server config entry
        var serverName = SanitizeName(entry.Identifier);
        var serverConfig = new JsonObject
        {
            ["url"] = entry.Url,
            ["name"] = entry.DisplayName,
            ["mediaType"] = entry.MediaType
        };

        if (entry.Version != null)
        {
            serverConfig["version"] = entry.Version;
        }

        // Read existing config or start fresh
        JsonObject mcpConfig;
        if (File.Exists(configPath))
        {
            var existing = await File.ReadAllTextAsync(configPath);
            mcpConfig = JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();
        }
        else
        {
            mcpConfig = new JsonObject();
        }

        var servers = mcpConfig["mcpServers"]?.AsObject() ?? new JsonObject();
        servers[serverName] = serverConfig;
        mcpConfig["mcpServers"] = servers;

        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(configPath, mcpConfig.ToJsonString(options));

        Console.WriteLine($"MCP server config written to {configPath}");
        Console.WriteLine($"Server name: {serverName}");

        // Also print the snippet for easy copy-paste
        Console.WriteLine();
        Console.WriteLine("Add this to your mcp-config.json:");
        Console.WriteLine(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [serverName] = serverConfig
        }, options));
    }

    private static async Task InstallSkillEntry(HttpClient client, CatalogEntry entry, string outputDir)
    {
        var skillsDir = Path.Combine(outputDir, SkillsDirectory);
        Directory.CreateDirectory(skillsDir);

        if (entry.Url != null)
        {
            // Download the artifact
            var response = await client.GetAsync(entry.Url);
            response.EnsureSuccessStatusCode();

            var fileName = SanitizeName(entry.Identifier);
            var ext = GuessExtension(entry.MediaType);
            var filePath = Path.Combine(skillsDir, $"{fileName}{ext}");

            var content = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filePath, content);
            Console.WriteLine($"Downloaded to {filePath}");
        }
        else if (entry.Inline.HasValue)
        {
            var fileName = SanitizeName(entry.Identifier);
            var ext = GuessExtension(entry.MediaType);
            var filePath = Path.Combine(skillsDir, $"{fileName}{ext}");

            var json = JsonSerializer.Serialize(entry.Inline.Value, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
            Console.WriteLine($"Inline content saved to {filePath}");
        }
        else
        {
            Console.Error.WriteLine("Warning: entry has neither url nor inline content");
        }

        Console.WriteLine($"Installed: {entry.DisplayName}");
    }

    private static string SanitizeName(string identifier)
    {
        // Strip common URI prefixes and take the last segment
        var name = identifier;

        if (name.StartsWith("urn:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = name.Split(':');
            name = parts[^1];
        }
        else if (Uri.TryCreate(name, UriKind.Absolute, out var uri))
        {
            name = uri.Segments[^1].TrimEnd('/');
        }

        // Replace non-alphanumeric characters with hyphens
        return string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-'));
    }

    private static string GuessExtension(string mediaType) => mediaType switch
    {
        _ when mediaType.Contains("json", StringComparison.OrdinalIgnoreCase) => ".json",
        _ when mediaType.Contains("yaml", StringComparison.OrdinalIgnoreCase) => ".yaml",
        _ when mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase) => ".xml",
        _ => ".bin"
    };
}
