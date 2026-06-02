using SpecWorks.AiCatalog.Models;

namespace SpecWorks.AiCatalog;

/// <summary>
/// Extension methods for <see cref="CatalogEntry"/>.
/// </summary>
public static class CatalogEntryExtensions
{
    /// <summary>
    /// Determines whether this catalog entry represents an A2A Agent Card.
    /// Checks against both standard and vendor-prefixed A2A agent card media types.
    /// Media type comparison is case-insensitive per RFC 2045.
    /// </summary>
    /// <param name="entry">The catalog entry to check.</param>
    /// <returns>
    /// <c>true</c> if the entry's <see cref="CatalogEntry.MediaType"/> matches either
    /// <see cref="KnownMediaTypes.A2AAgentCard"/> or <see cref="KnownMediaTypes.A2AAgentCardVendor"/>
    /// (case-insensitive); otherwise <c>false</c>.
    /// </returns>
    public static bool IsA2AAgentCard(this CatalogEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        var mediaType = entry.MediaType ?? string.Empty;

        return mediaType.Equals(KnownMediaTypes.A2AAgentCard, StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals(KnownMediaTypes.A2AAgentCardVendor, StringComparison.OrdinalIgnoreCase);
    }
}
