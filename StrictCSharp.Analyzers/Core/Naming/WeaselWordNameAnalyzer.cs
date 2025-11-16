using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrictCSharp.Analyzers.Core.Naming;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WeaselWordNameAnalyzer : BaseAnalyzer
{
    public const string DiagnosticId = "SC041";

    private static readonly DiagnosticDescriptor RuleDescriptor = new(
        DiagnosticId,
        "Type name contains a weasel word",
        "Type name '{0}' ends with a weasel word ('{1}'). Use a more descriptive name.",
        "Naming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Type names should not end with generic words like 'Service', 'Manager', 'Helper', or 'Util'.");

    protected override DiagnosticDescriptor Rule => RuleDescriptor;

    protected override SyntaxKind[] SyntaxKindsToAnalyze =>
        [SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.RecordDeclaration];

    private static readonly string[] _forbiddenSuffixes = [
        "Service",
        "Manager",
        "Helper",
        "Helpers",
        "Util",
        "Utils"
    ];

    protected override void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        var name = typeDeclaration.Identifier.Text;
        foreach (var suffix in _forbiddenSuffixes)
        {
            if (name.EndsWith(suffix))
            {
                var diagnostic = Diagnostic.Create(Rule, typeDeclaration.Identifier.GetLocation(), name, suffix);
                context.ReportDiagnostic(diagnostic);
                break;
            }
        }
    }
}