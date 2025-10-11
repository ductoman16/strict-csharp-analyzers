using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Testing.Framework;

namespace StrictCSharp.Analyzers.Tests.Testing.Framework;

/// <summary>
/// Tests for the <see cref="NoTaskDelayAnalyzer"/> to ensure it correctly identifies
/// test methods using await Task.Delay() which should be avoided in unit tests.
/// </summary>
public class NoTaskDelayAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new NoTaskDelayAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC204");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new NoTaskDelayAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Do not use await Task.Delay() in unit tests");
        diagnostic.MessageFormat.ToString().Should().Be("Unit test method '{0}' should not use await Task.Delay(). Use deterministic timing patterns instead.");
        diagnostic.Category.Should().Be("Testing");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a test method with await Task.Delay() triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithTaskDelay_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System.Threading.Tasks;
using Xunit;

public class TestClass
{
    [Fact]
    public async Task TestMethod()
    {
        await Task.Delay(1000);
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(NoTaskDelayAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test method with await Task.Delay(TimeSpan) triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithTaskDelayTimeSpan_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;
using System.Threading.Tasks;
using Xunit;

public class TestClass
{
    [Fact]
    public async Task TestMethod()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        var result = true;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(NoTaskDelayAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a non-test method with await Task.Delay() does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void NonTestMethodWithTaskDelay_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using System.Threading.Tasks;

public class TestClass
{
    public async Task RegularMethod()
    {
        await Task.Delay(1000);
        var result = true;
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test method without Task.Delay does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestMethodWithoutTaskDelay_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using System.Threading.Tasks;
using Xunit;

public class TestClass
{
    [Fact]
    public async Task TestMethod()
    {
        var result = await SomeAsyncOperation();
        Assert.True(result);
    }
    
    private async Task<bool> SomeAsyncOperation()
    {
        return await Task.FromResult(true);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a Theory test method with await Task.Delay() triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TheoryTestMethodWithTaskDelay_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System.Threading.Tasks;
using Xunit;

public class TestClass
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task TestMethod(int value)
    {
        await Task.Delay(value * 100);
        var result = value > 0;
        Assert.True(result);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(NoTaskDelayAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the NoTaskDelayAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Task).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new NoTaskDelayAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}
