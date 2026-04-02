namespace SpecWorks.AiCatalog.Validation;

/// <summary>
/// Conformance levels as defined by the AI Card specification.
/// </summary>
public enum ConformanceLevel
{
    /// <summary>Level 1: specVersion + entries with required fields.</summary>
    Minimal = 1,

    /// <summary>Level 2: Minimal + host with displayName.</summary>
    Discoverable = 2,

    /// <summary>Level 3: Discoverable + trustManifest on entries and/or host.</summary>
    Trusted = 3
}
