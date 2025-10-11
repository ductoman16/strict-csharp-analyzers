using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Testing.Organization;

namespace StrictCSharp.Analyzers.Tests.Testing.Organization;

/// <summary>
/// Tests for the <see cref="TestClassMustEndWithTestsAnalyzer"/> to ensure it correctly identifies
/// test classes that don't follow proper naming conventions and organization patterns.
/// </summary>
public class TestClassMustEndWithTestsAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new TestClassMustEndWithTestsAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC241");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new TestClassMustEndWithTestsAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Test class must end with 'Tests'");
        diagnostic.MessageFormat.ToString().Should().Be("Test class '{0}' {1}");
        diagnostic.Category.Should().Be("Testing");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a test class ending with 'Tests' does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassEndingWithTests_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

public class UserServiceTests
{
    [Fact]
    public void TestMethod()
    {
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class not ending with 'Tests' triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassNotEndingWithTests_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class UserServiceTest
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
        diagnostics[0].Id.Should().Be(TestClassMustEndWithTestsAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test class with constructor dependencies and matching fields does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithProperFieldsForConstructorParams_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

public interface IService { }

public class UserServiceTests
{
    private readonly IService _service;
    
    public UserServiceTests(IService service)
    {
        _service = service;
    }

    [Fact]
    public void TestMethod()
    {
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class with constructor dependencies but missing fields triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithMissingFieldsForConstructorParams_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public interface IService { }

public class UserServiceTests
{
    public UserServiceTests(IService service)
    {
        // Missing field for service parameter
    }

    [Fact]
    public void TestMethod()
    {
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TestClassMustEndWithTestsAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a non-test class does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void NonTestClass_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
public class UserService
{
    public void DoSomething()
    {
        var result = true;
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class with both naming and field issues triggers multiple diagnostics.
    /// </summary>
    [Fact]
    public void TestClassWithMultipleIssues_WhenCalled_ShouldReportMultipleDiagnostics()
    {
        var test = @"
using Xunit;

public interface IService { }

public class UserServiceTest
{
    public UserServiceTest(IService service)
    {
        // Missing field for service parameter
    }

    [Fact]
    public void TestMethod()
    {
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(2);
        diagnostics.All(d => d.Id == TestClassMustEndWithTestsAnalyzer.DiagnosticId).Should().BeTrue();
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the TestClassOrganizationAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new TestClassMustEndWithTestsAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}
