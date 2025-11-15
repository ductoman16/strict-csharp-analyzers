using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace StrictCSharp.Analyzers.Testing.Organization;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestClassMustEndWithTestsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SC241";
    private const string _title = "Test class must end with 'Tests'";
    private const string _messageFormat = "Test class '{0}' {1}";
    private const string _description = "Test classes should follow naming conventions (ending with 'Tests').";
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

        // Check if this is a test class
        if (!IsTestClass(classDeclaration))
        {
            return;
        }

        // Validate naming convention
        if (!classDeclaration.Identifier.Text.EndsWith("Tests"))
        {
            ReportDiagnostic(context, classDeclaration, "should end with 'Tests'");
        }

        // Check constructor for proper dependency setup
        var constructor = classDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault();

        if (constructor != null)
        {
            // Check if constructor parameters are properly stored
            var fields = classDeclaration.Members
                .OfType<FieldDeclarationSyntax>()
                .ToList();

            foreach (var parameter in constructor.ParameterList.Parameters)
            {
                if (parameter.Type == null)
                {
                    continue;
                }
                var parameterType = context.SemanticModel.GetTypeInfo(parameter.Type).Type;
                if (parameterType == null)
                {
                    continue;
                }

                var hasMatchingField = fields.Any(f =>
                {
                    var fieldType = context.SemanticModel.GetTypeInfo(f.Declaration.Type).Type;
                    return SymbolEqualityComparer.Default.Equals(fieldType, parameterType);
                });

                if (!hasMatchingField)
                {
                    ReportDiagnostic(context, classDeclaration,
                        $"is missing field for constructor parameter of type {parameterType.Name}");
                }
            }
        }
    }

    private static bool IsTestClass(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(m => IsTestMethod(m));

    private static bool IsTestMethod(MethodDeclarationSyntax method) =>
        method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "Fact" or "Theory");

    private void ReportDiagnostic(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, string message)
    {
        string extendedMessage = message switch
        {
            "should end with 'Tests'" => "should end with 'Tests' - Rename the class to follow naming convention (e.g., 'UserService' should be tested by 'UserServiceTests')",
            var m when m.StartsWith("is missing field for constructor parameter") => $"{m} - Add a private readonly field to store this dependency",
            _ => message
        };

        var diagnostic = Diagnostic.Create(_rule, classDeclaration.Identifier.GetLocation(),
            classDeclaration.Identifier.Text, extendedMessage);
        context.ReportDiagnostic(diagnostic);
    }
}
