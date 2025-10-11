using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Style.CommentStyle;

namespace StrictCSharp.Analyzers.Tests.Style.CommentStyle;

/// <summary>
/// Tests for the <see cref="NoInlineCommentsAnalyzer"/> to ensure it correctly identifies
/// inline comments that should be replaced with self-explanatory code or XML documentation.
/// </summary>
public class NoInlineCommentsAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new NoInlineCommentsAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC141");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new NoInlineCommentsAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Inline comments are not allowed; code should be self-explanatory or use XML documentation comments");
        diagnostic.MessageFormat.ToString().Should().Be("Inline comments are not allowed. Code should be self-explanatory (e.g., extract a method with a descriptive name). If that's not possible, use XML documentation comments (///).");
        diagnostic.Category.Should().Be("Style");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that code with XML documentation comments does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void CodeWithXmlDocumentationComments_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
/// <summary>
/// This is a valid XML documentation comment.
/// </summary>
public class TestClass
{
    /// <summary>
    /// This method does something important.
    /// </summary>
    public void TestMethod()
    {
        var result = 42;
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that code with TODO comments does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void CodeWithTodoComments_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
public class TestClass
{
    public void TestMethod()
    {
        // TODO: Implement this feature
        var result = 42;
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that code with null-forgiving operator justification comments does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void CodeWithNullForgiveComments_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
public class TestClass
{
    public void TestMethod()
    {
        string? value = GetValue();
        var result = value!; // ! Justified by previous null check
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that code with Arrange/Act/Assert comments does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void CodeWithArrangeActAssertComments_WhenCalled_ShouldNotReportDiagnostic()
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
    /// Verifies that code with regular inline comments triggers a diagnostic.
    /// </summary>
    [Fact]
    public void CodeWithInlineComments_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public class TestClass
{
    public void TestMethod()
    {
        var value = 42; // This is a regular inline comment
        var result = value * 2;
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(NoInlineCommentsAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that code with multi-line comments triggers a diagnostic.
    /// </summary>
    [Fact]
    public void CodeWithMultiLineComments_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public class TestClass
{
    public void TestMethod()
    {
        /* This is a multi-line comment
           that should trigger a diagnostic */
        var result = 42;
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(NoInlineCommentsAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that code with multiple inline comments triggers multiple diagnostics.
    /// </summary>
    [Fact]
    public void CodeWithMultipleInlineComments_WhenCalled_ShouldReportMultipleDiagnostics()
    {
        var test = @"
public class TestClass
{
    public void TestMethod()
    {
        var value = 42; // First comment
        var result = value * 2; // Second comment
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(2);
        diagnostics.All(d => d.Id == NoInlineCommentsAnalyzer.DiagnosticId).Should().BeTrue();
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the NoInlineCommentsAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new NoInlineCommentsAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}
