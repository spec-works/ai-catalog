using SpecWorks.AiCatalog.Models;
using Xunit;

namespace SpecWorks.AiCatalog.Tests;

/// <summary>
/// Tests for A2A discovery helpers: <see cref="KnownMediaTypes"/> and <see cref="CatalogEntryExtensions"/>.
/// </summary>
public class A2ADiscoveryHelperTests
{
    [Fact]
    public void KnownMediaTypes_AiCatalog_IsCorrect()
    {
        Assert.Equal("application/ai-catalog+json", KnownMediaTypes.AiCatalog);
    }

    [Fact]
    public void KnownMediaTypes_A2AAgentCard_IsCorrect()
    {
        Assert.Equal("application/a2a-agent-card+json", KnownMediaTypes.A2AAgentCard);
    }

    [Fact]
    public void KnownMediaTypes_A2AAgentCardVendor_IsCorrect()
    {
        Assert.Equal("application/vnd.a2a.agent-card+json", KnownMediaTypes.A2AAgentCardVendor);
    }

    [Fact]
    public void IsA2AAgentCard_WithStandardMediaType_ReturnsTrue()
    {
        var entry = new CatalogEntry
        {
            Identifier = "test-agent",
            DisplayName = "Test Agent",
            MediaType = KnownMediaTypes.A2AAgentCard
        };

        Assert.True(entry.IsA2AAgentCard());
    }

    [Fact]
    public void IsA2AAgentCard_WithVendorMediaType_ReturnsTrue()
    {
        var entry = new CatalogEntry
        {
            Identifier = "test-agent",
            DisplayName = "Test Agent",
            MediaType = KnownMediaTypes.A2AAgentCardVendor
        };

        Assert.True(entry.IsA2AAgentCard());
    }

    [Fact]
    public void IsA2AAgentCard_WithStandardMediaType_CaseInsensitive_ReturnsTrue()
    {
        var testCases = new[]
        {
            "APPLICATION/A2A-AGENT-CARD+JSON",
            "application/a2a-agent-card+json",
            "Application/A2A-Agent-Card+Json",
            "APPLICATION/a2a-agent-card+JSON"
        };

        foreach (var mediaType in testCases)
        {
            var entry = new CatalogEntry
            {
                Identifier = "test",
                DisplayName = "Test",
                MediaType = mediaType
            };

            Assert.True(entry.IsA2AAgentCard(), $"Expected case-insensitive match for: {mediaType}");
        }
    }

    [Fact]
    public void IsA2AAgentCard_WithVendorMediaType_CaseInsensitive_ReturnsTrue()
    {
        var testCases = new[]
        {
            "APPLICATION/VND.A2A.AGENT-CARD+JSON",
            "application/vnd.a2a.agent-card+json",
            "Application/Vnd.A2A.Agent-Card+Json",
            "APPLICATION/vnd.a2a.agent-card+JSON"
        };

        foreach (var mediaType in testCases)
        {
            var entry = new CatalogEntry
            {
                Identifier = "test",
                DisplayName = "Test",
                MediaType = mediaType
            };

            Assert.True(entry.IsA2AAgentCard(), $"Expected case-insensitive match for: {mediaType}");
        }
    }

    [Fact]
    public void IsA2AAgentCard_WithOtherMediaType_ReturnsFalse()
    {
        var testCases = new[]
        {
            KnownMediaTypes.AiCatalog,
            "application/json",
            "text/plain",
            "application/mcp-server+json",
            "application/vnd.claude.plugin+json"
        };

        foreach (var mediaType in testCases)
        {
            var entry = new CatalogEntry
            {
                Identifier = "test",
                DisplayName = "Test",
                MediaType = mediaType
            };

            Assert.False(entry.IsA2AAgentCard(), $"Expected non-match for: {mediaType}");
        }
    }

    [Fact]
    public void IsA2AAgentCard_WithEmptyMediaType_ReturnsFalse()
    {
        var entry = new CatalogEntry
        {
            Identifier = "test",
            DisplayName = "Test",
            MediaType = string.Empty
        };

        Assert.False(entry.IsA2AAgentCard());
    }

    [Fact]
    public void IsA2AAgentCard_WithNullEntry_ThrowsArgumentNullException()
    {
        CatalogEntry? entry = null;

        Assert.Throws<ArgumentNullException>(() => entry!.IsA2AAgentCard());
    }

    [Fact]
    public void IsA2AAgentCard_WithPartialMatch_ReturnsFalse()
    {
        var testCases = new[]
        {
            "application/a2a-agent",
            "a2a-agent-card+json",
            "application/a2a",
            "agent-card+json",
            "application/a2a-agent-card"
        };

        foreach (var mediaType in testCases)
        {
            var entry = new CatalogEntry
            {
                Identifier = "test",
                DisplayName = "Test",
                MediaType = mediaType
            };

            Assert.False(entry.IsA2AAgentCard(), $"Expected no match for partial media type: {mediaType}");
        }
    }
}
