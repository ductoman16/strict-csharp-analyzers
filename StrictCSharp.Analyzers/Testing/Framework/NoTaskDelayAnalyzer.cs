using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace StrictCSharp.Analyzers.Testing.Framework;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoTaskDelayAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SC204";
    private const string Title = "Do not use await Task.Delay() in unit tests";
    private const string MessageFormat = "Unit test method '{0}' should not use await Task.Delay(). Use deterministic timing patterns instead.";
    private const string Description = "Unit tests should not use await Task.Delay() as it introduces non-deterministic timing and slows down tests. Use test doubles or deterministic timing patterns instead.";
    private const string Category = "Testing";

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
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AwaitExpression);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var awaitExpression = (AwaitExpressionSyntax)context.Node;

        // Check if we're in a test method
        if (!IsInTestMethod(awaitExpression))
        {
            return;
        }

        // Check if the awaited expression is Task.Delay
        if (IsTaskDelayExpression(awaitExpression.Expression, context.SemanticModel))
        {
            var testMethod = GetContainingTestMethod(awaitExpression);
            if (testMethod != null)
            {
                var diagnostic = Diagnostic.Create(Rule, awaitExpression.GetLocation(), testMethod.Identifier.Text);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsInTestMethod(SyntaxNode node)
    {
        var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        return method != null && IsTestMethod(method);
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method) =>
        method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "Fact" or "Theory");

    private static MethodDeclarationSyntax? GetContainingTestMethod(SyntaxNode node)
    {
        var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        return method != null && IsTestMethod(method) ? method : null;
    }

    private static bool IsTaskDelayExpression(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        // Handle both Task.Delay() and Task.Delay(TimeSpan)
        if (expression is InvocationExpressionSyntax invocation)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess?.Name.Identifier.ValueText == "Delay")
            {
                var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
                if (symbolInfo.Symbol is INamedTypeSymbol namedType)
                {
                    return namedType.Name == "Task" && namedType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks";
                }
            }
        }

        return false;
    }
}
