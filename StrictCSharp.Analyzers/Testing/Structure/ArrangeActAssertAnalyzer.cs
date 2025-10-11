using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace StrictCSharp.Analyzers.Testing.Structure;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ArrangeActAssertAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SC221";
    private const string Title = "Test method must follow Arrange/Act/Assert pattern";
    private const string MessageFormat = "Test method '{0}' {1}";
    private const string Description = "Test methods should have clear Arrange, Act, and Assert sections separated by comments. Each section should be preceded by a '// Arrange', '// Act', or '// Assert' comment.";
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
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
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

        var statements = methodBody.Statements;
        if (!statements.Any())
        {
            return;
        }

        // Look for comments or regions indicating sections
        var sections = GetTestSections(methodBody);

        // Validate sections
        if (!sections.Contains("Arrange"))
        {
            ReportDiagnostic(context, methodDeclaration, "is missing Arrange section");
        }
        if (!sections.Contains("Act"))
        {
            ReportDiagnostic(context, methodDeclaration, "is missing Act section");
        }
        if (!sections.Contains("Assert"))
        {
            ReportDiagnostic(context, methodDeclaration, "is missing Assert section");
        }

        // Check section order
        if (sections.Count >= 2)
        {
            var arrangeIndex = sections.IndexOf("Arrange");
            var actIndex = sections.IndexOf("Act");
            var assertIndex = sections.IndexOf("Assert");

            if (arrangeIndex > actIndex && actIndex != -1 ||
                actIndex > assertIndex && assertIndex != -1 ||
                arrangeIndex > assertIndex && assertIndex != -1)
            {
                ReportDiagnostic(context, methodDeclaration, "has sections in incorrect order");
            }
        }
    }

    private static bool IsTestMethod(MethodDeclarationSyntax method) =>
        method.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "Fact" or "Theory");

    private static List<string> GetTestSections(BlockSyntax methodBody)
    {
        var sections = new List<string>();
        var trivia = methodBody.DescendantTrivia()
            .Where(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                       t.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                       t.IsKind(SyntaxKind.RegionDirectiveTrivia));

        foreach (var comment in trivia)
        {
            var text = comment.ToString().ToLower();
            if (text.Contains("arrange") || text.Contains("#region arrange"))
            {
                sections.Add("Arrange");
            }
            else if (text.Contains("act") || text.Contains("#region act"))
            {
                sections.Add("Act");
            }
            else if (text.Contains("assert") || text.Contains("#region assert"))
            {
                sections.Add("Assert");
            }
        }

        return sections;
    }

    private void ReportDiagnostic(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method, string message)
    {
        string extendedMessage = message switch
        {
            "is missing Arrange section" => "is missing Arrange section - Add a '// Arrange' comment before setup code",
            "is missing Act section" => "is missing Act section - Add a '// Act' comment before the code that exercises the system under test",
            "is missing Assert section" => "is missing Assert section - Add a '// Assert' comment before verification code with .Should() assertions",
            "has sections in incorrect order" => "has sections in incorrect order - Sections must be in order: Arrange, Act, Assert",
            _ => message
        };

        var diagnostic = Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier.Text, extendedMessage);
        context.ReportDiagnostic(diagnostic);
    }
}
