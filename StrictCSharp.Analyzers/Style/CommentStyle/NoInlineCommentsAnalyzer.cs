using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace StrictCSharp.Analyzers.Style.CommentStyle;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoInlineCommentsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SC141";
    private const string _title = "Inline comments are not allowed; code should be self-explanatory or use XML documentation comments";
    private const string _messageFormat = "Inline comments are not allowed. Code should be self-explanatory (e.g., extract a method with a descriptive name). If that's not possible, use XML documentation comments (///).";
    private const string _description = "Inline comments (// or /* ... */) are not allowed. Code should be self-explanatory, for example by extracting a method with a descriptive name. If that's not possible, use XML documentation comments (///) for documentation.";
    private const string _category = nameof(AnalyzerCategory.Style);

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: _description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
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
                context.ReportDiagnostic(Diagnostic.Create(_rule, trivia.GetLocation()));
            }
            else if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                context.ReportDiagnostic(Diagnostic.Create(_rule, trivia.GetLocation()));
            }
        }
    }
} 