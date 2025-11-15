using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace StrictCSharp.Analyzers.Testing.Organization;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestClassNamespaceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SC244";
    private const string _title = "Test class with TestOf attribute must follow correct namespace structure";
    private const string _messageFormat = "Test class '{0}' should be in namespace '{1}' but is in '{2}'";
    private const string _description = "Test classes with TestOf attribute must follow the namespace structure: [ClassUnderTestProjectName].Tests.[ClassUnderTestNamespace].[ClassUnderTest]Tests.";
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

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Check if this class has a TestOf attribute
        var testOfAttribute = GetTestOfAttribute(classDeclaration, context.SemanticModel);
        if (testOfAttribute == null)
        {
            return;
        }

        // Get the class under test type
        var classUnderTestType = GetClassUnderTestType(testOfAttribute, context.SemanticModel);
        if (classUnderTestType == null)
        {
            return;
        }

        // Get current namespace
        var currentNamespace = GetNamespace(classDeclaration);
        if (currentNamespace == null)
        {
            return;
        }

        // Calculate expected namespace
        var expectedNamespace = CalculateExpectedNamespace(classUnderTestType);
        
        // Check if current namespace matches expected
        if (currentNamespace != expectedNamespace)
        {
            var diagnostic = Diagnostic.Create(_rule, 
                classDeclaration.Identifier.GetLocation(), 
                classDeclaration.Identifier.Text, 
                expectedNamespace, 
                currentNamespace);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static AttributeSyntax? GetTestOfAttribute(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel) =>
        classDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(attr =>
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

    private static INamedTypeSymbol? GetClassUnderTestType(AttributeSyntax testOfAttribute, SemanticModel semanticModel)
    {
        if (testOfAttribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is TypeOfExpressionSyntax typeOfExpression)
        {
            var typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type);
            return typeInfo.Type as INamedTypeSymbol;
        }
        return null;
    }

    private static string? GetNamespace(ClassDeclarationSyntax classDeclaration)
    {
        var namespaceDeclaration = classDeclaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceDeclaration?.Name.ToString();
    }

    private static string CalculateExpectedNamespace(INamedTypeSymbol classUnderTestType)
    {
        var classUnderTestNamespace = classUnderTestType.ContainingNamespace.ToDisplayString();
        var classUnderTestName = classUnderTestType.Name;

        // Extract namespace parts
        var namespaceParts = classUnderTestNamespace.Split('.');
        if (namespaceParts.Length == 0)
        {
            return $"Tests.{classUnderTestName}Tests";
        }

        // For PauseButtonDashboard.Core.Publishing.PublishingStatusOperations -> PauseButtonDashboard.Core.Tests.Publishing.PublishingStatusOperationsTests
        // Insert ".Tests" after the project name (first two parts for this codebase) and append ".[ClassUnderTest]Tests"
        if (namespaceParts.Length >= 2)
        {
            // Build the expected namespace manually without LINQ
            var result = namespaceParts[0] + "." + namespaceParts[1] + ".Tests";
            for (int i = 2; i < namespaceParts.Length; i++)
            {
                result += "." + namespaceParts[i];
            }
            result += "." + classUnderTestName + "Tests";
            return result;
        }

        // Fallback for simpler cases
        var projectName = namespaceParts[0];
        if (namespaceParts.Length == 1)
        {
            return $"{projectName}.Tests.{classUnderTestName}Tests";
        }

        var restOfNamespace = string.Join(".", namespaceParts, 1, namespaceParts.Length - 1);
        return $"{projectName}.Tests.{restOfNamespace}.{classUnderTestName}Tests";
    }
}
