using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace StrictCSharp.Analyzers.Testing.Framework;

/// <summary>
/// Analyzer that detects use of xUnit Assert methods instead of FluentAssertions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoXUnitAssertionsAnalyzer : BaseAnalyzer
{
    public const string DiagnosticId = "SC201";

    private static readonly DiagnosticDescriptor RuleDescriptor = new(
        DiagnosticId,
        "Use FluentAssertions instead of xUnit assertions",
        "Test method '{0}' {1}",
        nameof(AnalyzerCategory.Testing),
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Test methods should use FluentAssertions syntax instead of xUnit Assert methods for more readable and detailed assertions.");

    protected override DiagnosticDescriptor Rule => RuleDescriptor;

    protected override SyntaxKind[] SyntaxKindsToAnalyze => [SyntaxKind.MethodDeclaration];

    protected override void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Check if this is a test method
        if (!IsTestMethod(methodDeclaration))
        {
            return;
        }

        var methodBody = methodDeclaration.Body;
        if (methodBody == null)
        {
            return;
        }

        // Find all xUnit assertions
        var xunitAssertions = FindXUnitAssertions(methodBody);

        // Report each xUnit assertion
        foreach (var assertion in xunitAssertions)
        {
            ReportDiagnostic(context, assertion, methodDeclaration.Identifier.Text,
                "uses xUnit assertions instead of FluentAssertions - Replace Assert.X with FluentAssertions syntax (e.g., use value.Should().Be(expected) instead of Assert.Equal(expected, value))");
        }
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method) =>
        method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "Fact" or "Theory");

    private static List<InvocationExpressionSyntax> FindXUnitAssertions(BlockSyntax methodBody) =>
        methodBody.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => IsXUnitAssertion(i))
            .ToList();

    private static bool IsXUnitAssertion(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var expression = memberAccess.Expression.ToString();
            return expression == "Assert";
        }
        return false;
    }

    private void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxNode node, string methodName, string message)
    {
        var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), methodName, message);
        context.ReportDiagnostic(diagnostic);
    }
}
