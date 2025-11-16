using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrictCSharp.Analyzers.Style.MethodStyle;

/// <summary>
/// Analyzer that detects Guard.Against calls that use nameof for the second parameter.
/// The guard clause library uses CallerArgumentExpression on the second parameter to automatically get the expression name.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GuardClauseNameofAnalyzer : BaseAnalyzer
{
    public const string DiagnosticId = "SC122";

    private static readonly DiagnosticDescriptor RuleDescriptor = new(
        DiagnosticId,
        "Guard.Against calls should not use nameof for the second parameter",
        "Guard.Against.{0} should not use nameof for the second parameter; the library uses CallerArgumentExpression to automatically capture the expression name",
        nameof(AnalyzerCategory.Style),
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Guard.Against calls from Ardalis.GuardClauses use CallerArgumentExpression on the second parameter to automatically capture the expression name. Using nameof is redundant and unnecessary.");

    protected override DiagnosticDescriptor Rule => RuleDescriptor;

    protected override SyntaxKind[] SyntaxKindsToAnalyze => [SyntaxKind.InvocationExpression];

    protected override void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is InvocationExpressionSyntax invocation)
        {
            // Check for Guard.Against.X(x, nameof(x))
            if (IsGuardAgainstCall(invocation) && HasNameofArgument(invocation))
            {
                var methodName = GetGuardMethodName(invocation);
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
            }
        }
    }

    private static bool IsGuardAgainstCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is MemberAccessExpressionSyntax guardAgainst)
        {
            return guardAgainst.Expression.ToString() == "Guard" &&
                   guardAgainst.Name.ToString() == "Against";
        }
        return false;
    }

    private static bool HasNameofArgument(InvocationExpressionSyntax invocation) =>
        invocation.ArgumentList.Arguments.Count >= 2 &&
        invocation.ArgumentList.Arguments[1].Expression is InvocationExpressionSyntax nameofInvocation &&
        nameofInvocation.Expression.ToString() == "nameof";

    private static string GetGuardMethodName(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.ToString();
        }
        return "Against";
    }
}

