using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace StrictCSharp.Analyzers.Core.Naming;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WeaselWordNameAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SC041";
    private static readonly LocalizableString _title = "Type name contains a weasel word";
    private static readonly LocalizableString _messageFormat = "Type name '{0}' ends with a weasel word ('{1}'). Use a more descriptive name.";
    private static readonly LocalizableString _description = "Type names should not end with generic words like 'Service', 'Manager', 'Helper', or 'Util'.";
    private const string _category = nameof(AnalyzerCategory.Naming);

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: _description);

    private static readonly string[] _forbiddenSuffixes = [
        "Service",
        "Manager",
        "Helper",
        "Helpers",
        "Util",
        "Utils"
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.InterfaceDeclaration, SyntaxKind.RecordDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        var name = typeDeclaration.Identifier.Text;
        foreach (var suffix in _forbiddenSuffixes)
        {
            if (name.EndsWith(suffix))
            {
                var diagnostic = Diagnostic.Create(_rule, typeDeclaration.Identifier.GetLocation(), name);
                context.ReportDiagnostic(diagnostic);
                break;
            }
        }
    }
}