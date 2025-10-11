using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Testing.Structure;

namespace StrictCSharp.Analyzers.Tests.Testing.Structure;

/// <summary>
/// Tests for the <see cref="FluentAssertionScopeAnalyzer"/> to ensure it correctly identifies
/// test methods with multiple assertions that should use AssertionScope.
/// </summary>
public class FluentAssertionScopeAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new FluentAssertionScopeAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC222");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new FluentAssertionScopeAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Test method with multiple assertions should use AssertionScope");
        diagnostic.MessageFormat.ToString().Should().Be("Test method '{0}' {1}");
        diagnostic.Category.Should().Be("Testing");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a test method with multiple assertions but no AssertionScope triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithMultipleAssertionsButNoScope_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using FluentAssertions;
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var result1 = 1;
        var result2 = 2;
        
        result1.Should().Be(1);
        result2.Should().Be(2);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(FluentAssertionScopeAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("TestMethod");
        diagnostics[0].GetMessage().Should().Contain("multiple assertions without using AssertionScope");
    }

    /// <summary>
    /// Verifies that a test method with multiple assertions and AssertionScope does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithMultipleAssertionsAndScope_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var result1 = 1;
        var result2 = 2;
        
        using (new AssertionScope())
        {
            result1.Should().Be(1);
            result2.Should().Be(2);
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test method with only one assertion does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithSingleAssertion_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using FluentAssertions;
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var result = 1;
        result.Should().Be(1);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a non-test method with multiple assertions does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void NonTestMethodWithMultipleAssertions_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using FluentAssertions;

public class TestClass
{
    public void RegularMethod()
    {
        var result1 = 1;
        var result2 = 2;
        
        result1.Should().Be(1);
        result2.Should().Be(2);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test method with no assertions does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithNoAssertions_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var result = 1;
        // No assertions
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a Theory test method with multiple assertions but no AssertionScope triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TheoryTestMethodWithMultipleAssertionsButNoScope_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using FluentAssertions;
using Xunit;

public class TestClass
{
    [Theory]
    [InlineData(1, 2)]
    public void TestMethod(int value1, int value2)
    {
        value1.Should().Be(1);
        value2.Should().Be(2);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(FluentAssertionScopeAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test method with multiple assertions using AssertionScope variable declaration does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithMultipleAssertionsAndScopeVariable_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var result1 = 1;
        var result2 = 2;

        var scope = new AssertionScope();
        result1.Should().Be(1);
        result2.Should().Be(2);
        scope.Dispose();
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test method with multiple chained assertions (single statement) does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithChainedAssertions_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using FluentAssertions;
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var result = ""test"";
        result.Should().NotBeNull().And.HaveLength(4).And.Be(""test"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test method with multiple assertions on different objects but no AssertionScope triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithMultipleAssertionsOnDifferentObjects_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using FluentAssertions;
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        var list = new List<int> { 1, 2, 3 };
        var count = list.Count;

        list.Should().NotBeEmpty();
        count.Should().Be(3);
        list.Should().Contain(2);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(FluentAssertionScopeAnalyzer.DiagnosticId);
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
    /// Verifies that a test method with expression body syntax does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithExpressionBody_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using FluentAssertions;
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod() => 1.Should().Be(1);
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the FluentAssertionScopeAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new FluentAssertionScopeAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}
