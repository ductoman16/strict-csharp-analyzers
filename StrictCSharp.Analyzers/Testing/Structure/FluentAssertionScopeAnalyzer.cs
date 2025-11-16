using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace StrictCSharp.Analyzers.Testing.Structure;

/// <summary>
/// Analyzer that verifies test methods with multiple assertions use AssertionScope.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FluentAssertionScopeAnalyzer : BaseAnalyzer
{
    public const string DiagnosticId = "SC222";

    private static readonly DiagnosticDescriptor RuleDescriptor = new(
        DiagnosticId,
        "Test method with multiple assertions should use AssertionScope",
        "Test method '{0}' {1}",
        "Testing",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Test methods with multiple assertions should use AssertionScope to get better error messages that show all failures.");

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

        // Find all assertions
        var fluentAssertions = FindFluentAssertions(methodBody);

        // If there are multiple assertions but no AssertionScope, report a diagnostic
        if (fluentAssertions.Count > 1 && !HasAssertionScope(methodBody))
        {
            ReportDiagnostic(context, methodDeclaration,
                "has multiple assertions without using AssertionScope - Add 'using (new AssertionScope())' before multiple assertions to get better error messages");
        }
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method) =>
        method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "Fact" or "Theory");

    private static List<InvocationExpressionSyntax> FindFluentAssertions(BlockSyntax methodBody) =>
        methodBody.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => IsFluentAssertion(i))
            .ToList();

    private static bool IsFluentAssertion(InvocationExpressionSyntax invocation)
    {
        var methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => string.Empty
        };

        return methodName.StartsWith("Should");
    }

    private static bool HasAssertionScope(BlockSyntax methodBody) =>
        methodBody.DescendantNodes()
            .OfType<UsingStatementSyntax>()
            .Any(u => u.Expression?.ToString().Contains("AssertionScope") == true) ||
        methodBody.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Any(o => o.Type.ToString().Contains("AssertionScope"));

    private void ReportDiagnostic(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method, string message)
    {
        var diagnostic = Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier.Text, message);
        context.ReportDiagnostic(diagnostic);
    }
}
