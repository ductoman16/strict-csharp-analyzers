namespace StrictCSharp.Analyzers;

/// <summary>
/// Defines the categories for all analyzers in the StrictCSharp.Analyzers project.
/// These categories are used in DiagnosticDescriptor definitions to group related analyzers.
/// </summary>
public enum AnalyzerCategory
{
    /// <summary>
    /// Documentation-related rules that enforce XML documentation requirements.
    /// Example: SC021 - Public type must have XML documentation
    /// </summary>
    Documentation,

    /// <summary>
    /// Naming convention rules that enforce proper naming standards.
    /// Example: SC041 - Type name contains a weasel word
    /// </summary>
    Naming,

    /// <summary>
    /// Code style rules that enforce consistent coding patterns and best practices.
    /// Examples: SC101 - Using statement expressions, SC121 - Guard clauses, SC141 - No inline comments
    /// </summary>
    Style,

    /// <summary>
    /// Testing-related rules that enforce test organization, structure, and best practices.
    /// Examples: SC201 - FluentAssertions, SC221 - Arrange/Act/Assert, SC241 - Test class naming
    /// </summary>
    Testing
}

