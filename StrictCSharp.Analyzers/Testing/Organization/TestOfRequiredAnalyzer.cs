using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace StrictCSharp.Analyzers.Testing.Organization;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestOfRequiredAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SC243";
    private const string Title = "Test class must have TestOf attribute";
    private const string MessageFormat = "Test class '{0}' must have a [TestOf(typeof(ClassUnderTest))] attribute";
    private const string Description = "All test classes (classes containing XUnit Facts or Theories) must have a TestOf attribute to indicate which class they are testing.";
    private const string Category = "Testing";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Check if this is a test class (contains test methods)
        if (!IsTestClass(classDeclaration))
        {
            return;
        }

        // Check if it has a TestOf attribute
        if (!HasTestOfAttribute(classDeclaration, context.SemanticModel))
        {
            var diagnostic = Diagnostic.Create(Rule, 
                classDeclaration.Identifier.GetLocation(), 
                classDeclaration.Identifier.Text);
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

    private static bool HasTestOfAttribute(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel) =>
        classDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(attr =>
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attr);
                if (symbolInfo.Symbol is IMethodSymbol method)
                {
                    var attributeType = method.ContainingType;
                    return attributeType.Name == "TestOfAttribute" &&
                           attributeType.ContainingNamespace?.ToDisplayString() == "PauseButtonDashboard.Analyzers";
                }
                return false;
            });
}
