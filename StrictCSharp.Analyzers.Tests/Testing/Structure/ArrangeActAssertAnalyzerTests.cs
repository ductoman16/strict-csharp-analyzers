using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Testing.Structure;

namespace StrictCSharp.Analyzers.Tests.Testing.Structure;

/// <summary>
/// Tests for the <see cref="ArrangeActAssertAnalyzer"/> to ensure it correctly identifies
/// test methods that don't follow the Arrange/Act/Assert pattern.
/// </summary>
public class ArrangeActAssertAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new ArrangeActAssertAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC221");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new ArrangeActAssertAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Test method must follow Arrange/Act/Assert pattern");
        diagnostic.MessageFormat.ToString().Should().Be("Test method '{0}' {1}");
        diagnostic.Category.Should().Be("Testing");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a test method with proper Arrange/Act/Assert structure does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithProperStructure_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Arrange
        var value = 42;
        
        // Act
        var result = value * 2;
        
        // Assert
        Assert.Equal(84, result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test method missing Arrange section triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodMissingArrangeSection_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Act
        var result = 42 * 2;
        
        // Assert
        Assert.Equal(84, result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(ArrangeActAssertAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test method missing Act section triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodMissingActSection_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Arrange
        var value = 42;
        
        // Assert
        Assert.Equal(42, value);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(ArrangeActAssertAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test method missing Assert section triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodMissingAssertSection_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Arrange
        var value = 42;
        
        // Act
        var result = value * 2;
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(ArrangeActAssertAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test method with sections in wrong order triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithWrongSectionOrder_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Act
        var result = 42 * 2;
        
        // Arrange
        var value = 42;
        
        // Assert
        Assert.Equal(84, result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(ArrangeActAssertAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test method using regions for structure does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithRegionStructure_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        #region Arrange
        var value = 42;
        #endregion
        
        #region Act
        var result = value * 2;
        #endregion
        
        #region Assert
        Assert.Equal(84, result);
        #endregion
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that an empty test method does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void EmptyTestMethod_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a non-test method does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void NonTestMethod_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
public class TestClass
{
    public void RegularMethod()
    {
        var value = 42;
        var result = value * 2;
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the TestStructureAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new ArrangeActAssertAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}
