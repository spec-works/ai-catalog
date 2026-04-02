namespace SpecWorks.AiCatalog;

/// <summary>
/// Base exception for all AI Catalog operations.
/// </summary>
public class AiCatalogException : Exception
{
    /// <summary>Initializes a new instance of <see cref="AiCatalogException"/>.</summary>
    public AiCatalogException() { }

    /// <summary>Initializes a new instance with the specified message.</summary>
    public AiCatalogException(string message) : base(message) { }

    /// <summary>Initializes a new instance with the specified message and inner exception.</summary>
    public AiCatalogException(string message, Exception innerException) : base(message, innerException) { }
}
