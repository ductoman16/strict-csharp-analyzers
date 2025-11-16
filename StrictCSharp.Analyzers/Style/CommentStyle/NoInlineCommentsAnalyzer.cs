using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Linq;

namespace StrictCSharp.Analyzers.Style.CommentStyle;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoInlineCommentsAnalyzer : BaseAnalyzer
{
    public const string DiagnosticId = "SC141";

    private static readonly DiagnosticDescriptor RuleDescriptor = new(
        DiagnosticId,
        "Inline comments are not allowed; code should be self-explanatory or use XML documentation comments",
        "Inline comments are not allowed. Code should be self-explanatory (e.g., extract a method with a descriptive name). If that's not possible, use XML documentation comments (///).",
        nameof(AnalyzerCategory.Style),
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Inline comments (// or /* ... */) are not allowed. Code should be self-explanatory, for example by extracting a method with a descriptive name. If that's not possible, use XML documentation comments (///) for documentation.");

    protected override DiagnosticDescriptor Rule => RuleDescriptor;

    protected override SyntaxKind[] SyntaxKindsToAnalyze => [];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    protected override void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        // Not used - this analyzer uses RegisterSyntaxTreeAction instead
    }

    private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        foreach (var trivia in root.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                var commentText = trivia.ToString().TrimStart();
                // Allow TODO comments (case-insensitive)
                if (commentText.StartsWith("///"))
                {
                    continue;
                }
                if (commentText.StartsWith("// TODO", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // Allow null-forgiving operator justification comments (required by Nullable.Extended.Analyzer)
                if (commentText.StartsWith("// !"))
                {
                    continue;
                }
                // Allow Arrange/Act/Assert comments (case-insensitive, allow trailing whitespace)
                var allowedTestComments = new[] { "// Arrange", "// Act", "// Assert" };
                if (allowedTestComments.Any(allowed => commentText.TrimEnd().Equals(allowed, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                context.ReportDiagnostic(Diagnostic.Create(Rule, trivia.GetLocation()));
            }
            else if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, trivia.GetLocation()));
            }
        }
    }
} 