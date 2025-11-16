using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace StrictCSharp.Analyzers;

/// <summary>
/// Base class for all strict C# analyzers that provides common functionality and enforces consistent structure.
/// </summary>
public abstract class BaseAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Gets the static diagnostic descriptor for this analyzer.
    /// Derived classes must provide this as a static readonly field.
    /// </summary>
    protected abstract DiagnosticDescriptor Rule { get; }

    /// <summary>
    /// Gets the syntax kinds this analyzer should analyze.
    /// Override this to specify which syntax nodes to analyze.
    /// </summary>
    protected abstract SyntaxKind[] SyntaxKindsToAnalyze { get; }

    /// <summary>
    /// Gets the supported diagnostics for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <summary>
    /// Initializes the analyzer with standard configuration.
    /// Derived classes should NOT override this unless they need custom initialization.
    /// </summary>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNodeInternal, SyntaxKindsToAnalyze);
    }

    /// <summary>
    /// Internal wrapper that calls the derived class's AnalyzeNode method.
    /// </summary>
    private void AnalyzeNodeInternal(SyntaxNodeAnalysisContext context)
    {
        AnalyzeNode(context);
    }

    /// <summary>
    /// Analyzes a syntax node. Override this in derived classes.
    /// </summary>
    protected abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);
}

