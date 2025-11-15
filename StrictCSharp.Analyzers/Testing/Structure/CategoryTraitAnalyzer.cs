using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace StrictCSharp.Analyzers.Testing.Structure;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CategoryTraitAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SC223";
    private const string _title = "Test class must have a Category Trait";
    private const string _messageFormat = "Test class '{0}' must have a [Trait(\"Category\", ...)] attribute";
    private const string _description = "Each test class must be decorated with a [Trait(\"Category\", ...)] attribute for test categorization.";
    private const string _category = nameof(AnalyzerCategory.Testing);

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
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
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
            var diagnostic = Diagnostic.Create(_rule, classDeclaration.Identifier.GetLocation(), classDeclaration.Identifier.Text);
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