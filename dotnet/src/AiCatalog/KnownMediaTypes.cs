namespace SpecWorks.AiCatalog;

/// <summary>
/// Well-known media type constants for AI catalog and related standards.
/// </summary>
public static class KnownMediaTypes
{
    /// <summary>
    /// Media type for AI Catalog documents: <c>application/ai-catalog+json</c>
    /// </summary>
    public const string AiCatalog = "application/ai-catalog+json";

    /// <summary>
    /// Media type for A2A Agent Card documents: <c>application/a2a-agent-card+json</c>
    /// </summary>
    public const string A2AAgentCard = "application/a2a-agent-card+json";

    /// <summary>
    /// Vendor-prefixed variant of A2A Agent Card media type: <c>application/vnd.a2a.agent-card+json</c>
    /// Used by some implementations; treated as equivalent to <see cref="A2AAgentCard"/>.
    /// </summary>
    public const string A2AAgentCardVendor = "application/vnd.a2a.agent-card+json";
}
