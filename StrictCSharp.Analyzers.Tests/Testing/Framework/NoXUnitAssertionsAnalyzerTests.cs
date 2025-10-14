using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Testing.Framework;

namespace StrictCSharp.Analyzers.Tests.Testing.Framework;

/// <summary>
/// Tests for the <see cref="NoXUnitAssertionsAnalyzer"/> to ensure it correctly identifies
/// test methods using xUnit Assert methods instead of FluentAssertions.
/// </summary>
public class NoXUnitAssertionsAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new NoXUnitAssertionsAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC201");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new NoXUnitAssertionsAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Use FluentAssertions instead of xUnit assertions");
        diagnostic.MessageFormat.ToString().Should().Be("Test method '{0}' {1}");
        diagnostic.Category.Should().Be("Testing");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a test method using FluentAssertions does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithFluentAssertions_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using FluentAssertions;
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var result = 42;
        result.Should().Be(42);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test method using Assert.Equal triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithAssertEqual_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var result = 42;
        Assert.Equal(42, result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(NoXUnitAssertionsAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test method using Assert.True triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithAssertTrue_WhenCalled_ShouldReportDiagnostic()
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
        diagnostics[0].Id.Should().Be(NoXUnitAssertionsAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test method using multiple xUnit assertions triggers multiple diagnostics.
    /// </summary>
    [Fact]
    public void TestMethodWithMultipleXUnitAssertions_WhenCalled_ShouldReportMultipleDiagnostics()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var result1 = 42;
        var result2 = true;
        Assert.Equal(42, result1);
        Assert.True(result2);
        Assert.NotNull(result1);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(3);
        diagnostics.All(d => d.Id == NoXUnitAssertionsAnalyzer.DiagnosticId).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a Theory test method using xUnit assertions triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TheoryTestMethodWithXUnitAssertions_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(3, 4)]
    public void TestMethod(int value1, int value2)
    {
        Assert.Equal(value1 + 1, value2);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(NoXUnitAssertionsAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a non-test method using Assert does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void NonTestMethodWithAssert_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    public void RegularMethod()
    {
        var result = 42;
        Assert.Equal(42, result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the NoTaskDelayAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new NoXUnitAssertionsAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}
