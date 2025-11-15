using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace StrictCSharp.Analyzers.Style.UsingStatements;

/// <summary>
/// Analyzer that enforces using statement expressions instead of using blocks at the end of methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UsingBlockAtEndAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SC101";
    private const string Title = "Use using statement expression instead of using block at end of method";
    private const string MessageFormat = "Use a using statement expression instead of a using block as the last statement in a method";
    private const string Description = "Prefer using statement expressions (using var x = ...) over using blocks (using (var x = ...) { ... }) when the using is the last statement in a method.";
    private const string Category = nameof(AnalyzerCategory.Style);

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var body = methodDeclaration.Body;
        if (body == null || body.Statements.Count == 0)
        {
            return;
        }

        var lastStatement = body.Statements.Last();
        if (lastStatement is UsingStatementSyntax usingStatement)
        {
            // Check if this is a using block (old style) vs using declaration (new style)
            // Using blocks have a BlockSyntax as their statement
            // Using declarations have a declaration and their statement is not a block
            if (usingStatement.Statement is BlockSyntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, usingStatement.GetLocation()));
            }
        }
    }
}