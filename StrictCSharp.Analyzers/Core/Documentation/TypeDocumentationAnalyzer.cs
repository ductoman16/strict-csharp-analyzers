using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrictCSharp.Analyzers.Core.Documentation;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TypeDocumentationAnalyzer : BaseAnalyzer
{
    public const string DiagnosticId = "SC021";

    private static readonly DiagnosticDescriptor RuleDescriptor = new(
        DiagnosticId,
        "Public type must have XML documentation",
        "Public type '{0}' must have XML documentation",
        "Documentation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All public types must have XML documentation.");

    protected override DiagnosticDescriptor Rule => RuleDescriptor;

    protected override SyntaxKind[] SyntaxKindsToAnalyze =>
        [SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.EnumDeclaration, SyntaxKind.RecordDeclaration];

    protected override void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (BaseTypeDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);

        if (symbol == null || !symbol.DeclaredAccessibility.HasFlag(Accessibility.Public))
        {
            return;
        }

        var documentation = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(documentation))
        {
            var diagnostic = Diagnostic.Create(Rule, typeDeclaration.Identifier.GetLocation(), symbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
