using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Style.MethodStyle;

namespace StrictCSharp.Analyzers.Tests.Style.MethodStyle;

/// <summary>
/// Tests for the <see cref="GuardClauseNameofAnalyzer"/> to ensure it correctly identifies
/// Guard.Against calls that use nameof for the second parameter.
/// </summary>
public class GuardClauseNameofAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new GuardClauseNameofAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC122");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new GuardClauseNameofAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Guard.Against calls should not use nameof for the second parameter");
        diagnostic.MessageFormat.ToString().Should().Be("Guard.Against.{0} should not use nameof for the second parameter; the library uses CallerArgumentExpression to automatically capture the expression name");
        diagnostic.Category.Should().Be("Style");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Guard.Against call with nameof triggers a diagnostic.
    /// </summary>
    [Fact]
    public void GuardAgainstCallWithNameof_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Ardalis.GuardClauses;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        Guard.Against.Null(parameter, nameof(parameter));
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseNameofAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("CallerArgumentExpression");
    }

    /// <summary>
    /// Verifies that proper Guard.Against usage with descriptive message does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void ProperGuardAgainstUsageWithMessage_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Ardalis.GuardClauses;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        Guard.Against.Null(parameter, ""Parameter cannot be null"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(0);
    }

    /// <summary>
    /// Verifies that Guard.Against calls without nameof do not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void GuardAgainstCallsWithoutNameof_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Ardalis.GuardClauses;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        Guard.Against.Null(parameter);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the GuardClauseNameofAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new GuardClauseNameofAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}

