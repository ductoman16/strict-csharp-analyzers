using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace StrictCSharp.Analyzers.Testing.Structure;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CategoryTraitAnalyzer : BaseAnalyzer
{
    public const string DiagnosticId = "SC223";

    private static readonly DiagnosticDescriptor RuleDescriptor = new(
        DiagnosticId,
        "Test class must have a Category Trait",
        "Test class '{0}' must have a [Trait(\"Category\", ...)] attribute",
        nameof(AnalyzerCategory.Testing),
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each test class must be decorated with a [Trait(\"Category\", ...)] attribute for test categorization.");

    protected override DiagnosticDescriptor Rule => RuleDescriptor;

    protected override SyntaxKind[] SyntaxKindsToAnalyze => [SyntaxKind.ClassDeclaration];

    protected override void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Only apply to test classes
        if (!IsTestClass(classDeclaration))
        {
            return;
        }

        // Check for [Trait("Category", ...)] attribute
        if (!HasCategoryTrait(classDeclaration, context.SemanticModel))
        {
            var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), classDeclaration.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsTestClass(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(IsTestMethod);

    private static bool IsTestMethod(MethodDeclarationSyntax method) =>
        method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "Fact" or "Theory");

    private static bool HasCategoryTrait(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        foreach (var attrList in classDeclaration.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (name == "Trait" || name.EndsWith(".Trait"))
                {
                    if (attr.ArgumentList != null && attr.ArgumentList.Arguments.Count >= 2)
                    {
                        var firstArg = attr.ArgumentList.Arguments[0].Expression;
                        var constValue = semanticModel.GetConstantValue(firstArg);
                        if (constValue.HasValue && constValue.Value is string s && s == "Category")
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }
}