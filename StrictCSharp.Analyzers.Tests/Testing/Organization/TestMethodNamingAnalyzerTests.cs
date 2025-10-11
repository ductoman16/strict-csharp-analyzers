using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Testing.Organization;

namespace StrictCSharp.Analyzers.Tests.Testing.Organization;

/// <summary>
/// Tests for the <see cref="TestMethodNamingAnalyzer"/> to ensure it correctly identifies
/// test methods that don't follow the 3-segment naming pattern.
/// </summary>
public class TestMethodNamingAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new TestMethodNamingAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC242");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new TestMethodNamingAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Test method name must follow 3-segment structure");
        diagnostic.MessageFormat.ToString().Should().Be("Test method '{0}' must follow the naming pattern 'MethodName_Scenario_ExpectedResult' with exactly 3 segments separated by underscores");
        diagnostic.Category.Should().Be("Testing");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a test method with proper 3-segment naming does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithProperNaming_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void CalculateValue_WhenGivenValidInput_ShouldReturnCorrectResult()
    {
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test method with only one segment triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithOneSegment_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TestMethodNamingAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test method with two segments triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithTwoSegments_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void CalculateValue_ShouldReturnResult()
    {
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TestMethodNamingAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test method with four segments triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithFourSegments_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void CalculateValue_WhenGivenValidInput_AndSomeCondition_ShouldReturnCorrectResult()
    {
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TestMethodNamingAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a Theory test method with proper 3-segment naming does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TheoryTestMethodWithProperNaming_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CalculateValue_WhenGivenInput_ShouldReturnExpectedResult(int input)
    {
        var result = input > 0;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test method with empty segments triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithEmptySegments_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void CalculateValue__ShouldReturnResult()
    {
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TestMethodNamingAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a non-test method does not trigger a diagnostic regardless of naming.
    /// </summary>
    [Fact]
    public void NonTestMethod_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
public class TestClass
{
    public void BadMethodName()
    {
        var result = true;
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the TestNamingAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new TestMethodNamingAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}
