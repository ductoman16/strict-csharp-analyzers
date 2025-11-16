using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StrictCSharp.Analyzers.Style.MethodStyle;

/// <summary>
/// Analyzer that detects manual parameter validation that should use Ardalis.GuardClauses.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ManualParameterValidationAnalyzer : BaseAnalyzer
{
    public const string DiagnosticId = "SC121";

    private static readonly DiagnosticDescriptor RuleDescriptor = new(
        DiagnosticId,
        "Use Ardalis.GuardClauses for parameter validation",
        "Use Guard.Against.{0} instead of manual validation",
        "Style",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Use Ardalis.GuardClauses for parameter validation at the start of methods to improve code readability and maintainability.");

    protected override DiagnosticDescriptor Rule => RuleDescriptor;

    protected override SyntaxKind[] SyntaxKindsToAnalyze =>
        [SyntaxKind.IfStatement, SyntaxKind.ThrowExpression];

    protected override void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is IfStatementSyntax ifStatement)
        {
            // Check for null
            if (IsNullCheck(ifStatement.Condition) && HasArgumentException(ifStatement.Statement))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, ifStatement.GetLocation(), "Null"));
                return;
            }

            // Check for empty string/collection
            if (IsEmptyCheck(ifStatement.Condition, context.SemanticModel) && HasArgumentException(ifStatement.Statement))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, ifStatement.GetLocation(), "NullOrEmpty"));
                return;
            }

            // Check for out of range
            if (IsOutOfRangeCheck(ifStatement.Condition) && HasArgumentException(ifStatement.Statement))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, ifStatement.GetLocation(), "OutOfRange"));
                return;
            }

            // Check for default value
            if (IsDefaultValueCheck(ifStatement.Condition) && HasArgumentException(ifStatement.Statement))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, ifStatement.GetLocation(), "Default"));
            }
        }
        else if (context.Node is ThrowExpressionSyntax throwExpression)
        {
            // Check for x ?? throw new ArgumentNullException()
            if (throwExpression.Parent is BinaryExpressionSyntax binary &&
                binary.Kind() == SyntaxKind.CoalesceExpression &&
                IsArgumentException(throwExpression.Expression))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, throwExpression.GetLocation(), "Null"));
            }
        }
    }

    private static bool IsNullCheck(ExpressionSyntax condition)
    {
        if (condition is BinaryExpressionSyntax binary)
        {
            return binary.Kind() == SyntaxKind.EqualsExpression &&
                   (IsNullLiteral(binary.Left) || IsNullLiteral(binary.Right));
        }
        return false;
    }

    private static bool IsEmptyCheck(ExpressionSyntax condition, SemanticModel semanticModel)
    {
        if (condition is BinaryExpressionSyntax binary)
        {
            // Check for string.IsNullOrEmpty() or string.IsNullOrWhiteSpace()
            if (binary.Left is InvocationExpressionSyntax invocation)
            {
                var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (methodSymbol?.ContainingType.SpecialType == SpecialType.System_String)
                {
                    return methodSymbol.Name is "IsNullOrEmpty" or "IsNullOrWhiteSpace";
                }
            }

            // Check for collection.Count == 0 or collection.Length == 0
            if (binary.Kind() == SyntaxKind.EqualsExpression)
            {
                if (binary.Left is MemberAccessExpressionSyntax left &&
                    binary.Right is LiteralExpressionSyntax right &&
                    right.Token.Value is 0)
                {
                    var propertyName = left.Name.ToString();
                    return propertyName is "Count" or "Length";
                }
            }
        }
        return false;
    }

    private static bool IsOutOfRangeCheck(ExpressionSyntax condition)
    {
        if (condition is BinaryExpressionSyntax binary)
        {
            var kind = binary.Kind();
            return kind == SyntaxKind.LessThanExpression ||
                   kind == SyntaxKind.LessThanOrEqualExpression ||
                   kind == SyntaxKind.GreaterThanExpression ||
                   kind == SyntaxKind.GreaterThanOrEqualExpression;
        }
        return false;
    }

    private static bool IsDefaultValueCheck(ExpressionSyntax condition)
    {
        if (condition is BinaryExpressionSyntax binary)
        {
            // Check for == default or == default(T)
            if (binary.Kind() == SyntaxKind.EqualsExpression)
            {
                return IsDefaultExpression(binary.Left) || IsDefaultExpression(binary.Right);
            }
        }
        return false;
    }

    private static bool IsDefaultExpression(ExpressionSyntax expression) =>
        expression.IsKind(SyntaxKind.DefaultExpression) ||
        expression.IsKind(SyntaxKind.DefaultLiteralExpression);

    private static bool IsNullLiteral(ExpressionSyntax expression) =>
        expression.IsKind(SyntaxKind.NullLiteralExpression);

    private static bool HasArgumentException(StatementSyntax statement)
    {
        if (statement is ThrowStatementSyntax throwStatement)
        {
            return IsArgumentException(throwStatement.Expression);
        }
        return false;
    }

    private static bool IsArgumentException(ExpressionSyntax? expression)
    {
        if (expression is ObjectCreationExpressionSyntax creation)
        {
            var type = creation.Type.ToString();
            return type.Contains("ArgumentException") ||
                   type.Contains("ArgumentNullException") ||
                   type.Contains("ArgumentOutOfRangeException");
        }
        return false;
    }
}

