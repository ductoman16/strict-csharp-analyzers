using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace StrictCSharp.Analyzers.Testing.Organization;

// TODO: Update for 2-segment names.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestMethodNamingAnalyzer : BaseAnalyzer
{
    public const string DiagnosticId = "SC242";

    private static readonly DiagnosticDescriptor RuleDescriptor = new(
        DiagnosticId,
        "Test method name must follow 3-segment structure",
        "Test method '{0}' must follow the naming pattern 'MethodName_Scenario_ExpectedResult' with exactly 3 segments separated by underscores",
        nameof(AnalyzerCategory.Testing),
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Test method names should have exactly 3 segments separated by underscores: MethodName_Scenario_ExpectedResult. This ensures consistent and readable test naming across the codebase.");

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

        var methodName = methodDeclaration.Identifier.Text;

        // Check if the method name follows the 3-segment pattern
        if (!HasThreeSegments(methodName))
        {
            var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method) =>
        method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "Fact" or "Theory");

    private static bool HasThreeSegments(string methodName)
    {
        // Split by underscore and check if we have exactly 3 segments
        var segments = methodName.Split('_');
        return segments.Length == 3 && segments.All(segment => !string.IsNullOrWhiteSpace(segment));
    }
}
