using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace StrictCSharp.Analyzers.Core.Documentation;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TypeDocumentationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SC021";

    private static readonly LocalizableString Title = "Public type must have XML documentation";
    private static readonly LocalizableString MessageFormat = "Public type '{0}' must have XML documentation";
    private static readonly LocalizableString Description = "All public types must have XML documentation.";
    private const string Category = nameof(AnalyzerCategory.Documentation);

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
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.EnumDeclaration, SyntaxKind.RecordDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
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
