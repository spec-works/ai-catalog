namespace SpecWorks.AiCatalog.Validation;

/// <summary>
/// Severity of a validation diagnostic.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>An error that prevents conformance at the current level.</summary>
    Error,

    /// <summary>A warning that does not prevent conformance but indicates a potential issue.</summary>
    Warning
}

/// <summary>
/// A single validation diagnostic (error or warning).
/// </summary>
/// <param name="Severity">Error or warning.</param>
/// <param name="Message">Human-readable description of the issue.</param>
/// <param name="Path">JSON pointer-style path to the element (e.g., "entries[0].trustManifest.identity").</param>
public sealed record ValidationDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    string? Path = null);

/// <summary>
/// The result of validating an AI Catalog document.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>Whether the catalog is valid (has no errors at the evaluated level).</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>The highest conformance level the catalog satisfies.</summary>
    public ConformanceLevel ConformanceLevel { get; init; }

    /// <summary>Validation errors.</summary>
    public IReadOnlyList<ValidationDiagnostic> Errors { get; init; } = [];

    /// <summary>Validation warnings.</summary>
    public IReadOnlyList<ValidationDiagnostic> Warnings { get; init; } = [];
}
