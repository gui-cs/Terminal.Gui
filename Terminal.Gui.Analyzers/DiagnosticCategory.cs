namespace Terminal.Gui.Analyzers;

/// <summary>
/// Categories commonly used for diagnostic analyzers, inspired by FxCop and .NET analyzers conventions.
/// </summary>
internal enum DiagnosticCategory
{
    /// <summary>
    /// Issues related to naming conventions and identifiers.
    /// </summary>
    Naming,

    /// <summary>
    /// API design, class structure, inheritance, etc.
    /// </summary>
    Design,

    /// <summary>
    /// How code uses APIs or language features incorrectly or suboptimally.
    /// </summary>
    Usage,

    /// <summary>
    /// Patterns that cause poor runtime performance.
    /// </summary>
    Performance,

    /// <summary>
    /// Vulnerabilities or insecure coding patterns.
    /// </summary>
    Security,

    /// <summary>
    /// Code patterns that can cause bugs, crashes, or unpredictable behavior.
    /// </summary>
    Reliability,

    /// <summary>
    /// Code readability, complexity, or future-proofing concerns.
    /// </summary>
    Maintainability,

    /// <summary>
    /// Code patterns that may not work on all platforms or frameworks.
    /// </summary>
    Portability,

    /// <summary>
    /// Issues with culture, localization, or globalization support.
    /// </summary>
    Globalization,

    /// <summary>
    /// Problems when working with COM, P/Invoke, or other interop scenarios.
    /// </summary>
    Interoperability,

    /// <summary>
    /// Issues with missing or incorrect XML doc comments.
    /// </summary>
    Documentation,

    /// <summary>
    /// Purely stylistic issues not affecting semantics (e.g., whitespace, order).
    /// </summary>
    Style
}
