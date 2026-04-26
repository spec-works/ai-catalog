using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using SpecWorks.AiCatalog.Models;
using SpecWorks.AiCatalog.Parsing;
using SpecWorks.AiCatalog.Validation;

namespace SpecWorks.AiCatalog.Cli.Commands;

/// <summary>
/// The <c>explore</c> command — fetches an AI Catalog from a URL and enables interactive browsing.
/// </summary>
public static class ExploreCommand
{
    /// <summary>
    /// Creates the <c>explore</c> command.
    /// </summary>
    public static Command Create()
    {
        return Create(null);
    }

    /// <summary>
    /// Creates the <c>explore</c> command with an optional custom HttpClient (for testing).
    /// </summary>
    internal static Command Create(HttpClient? httpClient)
    {
        var urlArgument = new Argument<string>("url", "URL of an AI Catalog document");
        var filterTagOption = new Option<string?>("--filter-tag", "Filter entries by tag");
        var filterMediaTypeOption = new Option<string?>("--filter-media-type", "Filter entries by media type");
        var showEntryOption = new Option<string?>("--show", "Show details for an entry by identifier");

        var cmd = new Command("explore", "Fetch and browse an AI Catalog from a URL")
        {
            urlArgument,
            filterTagOption,
            filterMediaTypeOption,
            showEntryOption
        };

        cmd.SetHandler(async (InvocationContext context) =>
        {
            var url = context.ParseResult.GetValueForArgument(urlArgument);
            var filterTag = context.ParseResult.GetValueForOption(filterTagOption);
            var filterMediaType = context.ParseResult.GetValueForOption(filterMediaTypeOption);
            var showEntry = context.ParseResult.GetValueForOption(showEntryOption);

            try
            {
                var client = httpClient ?? new HttpClient();
                var catalog = await FetchCatalogAsync(client, url);

                if (showEntry != null)
                {
                    ShowEntryDetails(catalog, showEntry);
                }
                else
                {
                    ListEntries(catalog, filterTag, filterMediaType);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or AiCatalogException)
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

    private static void ListEntries(Models.AiCatalog catalog, string? filterTag, string? filterMediaType)
    {
        // Validate and show conformance info
        var result = AiCatalogValidator.Validate(catalog);
        Console.WriteLine($"AI Catalog (specVersion: {catalog.SpecVersion}, conformance: {result.ConformanceLevel})");

        if (catalog.Host != null)
        {
            Console.WriteLine($"Host: {catalog.Host.DisplayName}");
        }

        Console.WriteLine();

        var entries = catalog.Entries.AsEnumerable();

        if (filterTag != null)
        {
            entries = entries.Where(e => e.Tags?.Contains(filterTag, StringComparer.OrdinalIgnoreCase) == true);
        }

        if (filterMediaType != null)
        {
            entries = entries.Where(e => string.Equals(e.MediaType, filterMediaType, StringComparison.OrdinalIgnoreCase));
        }

        var entryList = entries.ToList();

        if (entryList.Count == 0)
        {
            Console.WriteLine("No entries found matching the specified filters.");
            return;
        }

        // Table header
        Console.WriteLine($"{"Identifier",-40} {"Display Name",-25} {"Media Type",-35} {"Version",-10}");
        Console.WriteLine(new string('-', 114));

        foreach (var entry in entryList)
        {
            var id = Truncate(entry.Identifier, 40);
            var name = Truncate(entry.DisplayName, 25);
            var mediaType = Truncate(entry.MediaType, 35);
            var version = Truncate(entry.Version ?? "-", 10);
            Console.WriteLine($"{id,-40} {name,-25} {mediaType,-35} {version,-10}");
        }

        Console.WriteLine();
        Console.WriteLine($"{entryList.Count} entries shown (of {catalog.Entries.Count} total)");
    }

    private static void ShowEntryDetails(Models.AiCatalog catalog, string identifier)
    {
        var entry = catalog.Entries.FirstOrDefault(e =>
            string.Equals(e.Identifier, identifier, StringComparison.Ordinal));

        if (entry == null)
        {
            Console.Error.WriteLine($"Entry not found: {identifier}");
            return;
        }

        Console.WriteLine($"Identifier:  {entry.Identifier}");
        Console.WriteLine($"Display Name: {entry.DisplayName}");
        Console.WriteLine($"Media Type:  {entry.MediaType}");

        if (entry.Description != null)
            Console.WriteLine($"Description: {entry.Description}");
        if (entry.Version != null)
            Console.WriteLine($"Version:     {entry.Version}");
        if (entry.Url != null)
            Console.WriteLine($"URL:         {entry.Url}");
        if (entry.UpdatedAt != null)
            Console.WriteLine($"Updated At:  {entry.UpdatedAt}");
        if (entry.Tags is { Count: > 0 })
            Console.WriteLine($"Tags:        {string.Join(", ", entry.Tags)}");

        if (entry.Publisher != null)
        {
            Console.WriteLine();
            Console.WriteLine("Publisher:");
            Console.WriteLine($"  Identifier:  {entry.Publisher.Identifier}");
            Console.WriteLine($"  Display Name: {entry.Publisher.DisplayName}");
        }

        if (entry.TrustManifest != null)
        {
            Console.WriteLine();
            Console.WriteLine("Trust Manifest:");
            Console.WriteLine($"  Identity: {entry.TrustManifest.Identity}");
            if (entry.TrustManifest.PrivacyPolicyUrl != null)
                Console.WriteLine($"  Privacy Policy: {entry.TrustManifest.PrivacyPolicyUrl}");
            if (entry.TrustManifest.TermsOfServiceUrl != null)
                Console.WriteLine($"  Terms of Service: {entry.TrustManifest.TermsOfServiceUrl}");
            if (entry.TrustManifest.Attestations is { Count: > 0 })
            {
                Console.WriteLine($"  Attestations: {entry.TrustManifest.Attestations.Count}");
                foreach (var att in entry.TrustManifest.Attestations)
                {
                    Console.WriteLine($"    - {att.Type}: {att.Uri}");
                }
            }
        }

        if (entry.Data.HasValue)
        {
            Console.WriteLine();
            Console.WriteLine("Embedded content:");
            Console.WriteLine(JsonSerializer.Serialize(entry.Data.Value, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength - 3), "...");
    }
}
