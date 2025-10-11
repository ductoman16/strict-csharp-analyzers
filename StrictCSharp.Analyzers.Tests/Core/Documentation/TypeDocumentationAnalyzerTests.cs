using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Core.Documentation;

namespace StrictCSharp.Analyzers.Tests.Core.Documentation;

/// <summary>
/// Tests for the <see cref="TypeDocumentationAnalyzer"/> to ensure it correctly identifies
/// public types that are missing XML documentation.
/// </summary>
public class TypeDocumentationAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new TypeDocumentationAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC021");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new TypeDocumentationAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Public type must have XML documentation");
        diagnostic.MessageFormat.ToString().Should().Be("Public type '{0}' must have XML documentation");
        diagnostic.Category.Should().Be("Documentation");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a public class with XML documentation does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void PublicClassWithDocumentation_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
/// <summary>
/// This is a documented public class.
/// </summary>
public class DocumentedClass
{
    public void Method() { }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a public class without XML documentation triggers a diagnostic.
    /// </summary>
    [Fact]
    public void PublicClassWithoutDocumentation_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public class UndocumentedClass
{
    public void Method() { }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TypeDocumentationAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a public interface with XML documentation does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void PublicInterfaceWithDocumentation_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
/// <summary>
/// This is a documented public interface.
/// </summary>
public interface IDocumentedInterface
{
    void Method();
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a public interface without XML documentation triggers a diagnostic.
    /// </summary>
    [Fact]
    public void PublicInterfaceWithoutDocumentation_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public interface IUndocumentedInterface
{
    void Method();
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TypeDocumentationAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a public enum with XML documentation does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void PublicEnumWithDocumentation_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
/// <summary>
/// This is a documented public enum.
/// </summary>
public enum DocumentedEnum
{
    Value1,
    Value2
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a public enum without XML documentation triggers a diagnostic.
    /// </summary>
    [Fact]
    public void PublicEnumWithoutDocumentation_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public enum UndocumentedEnum
{
    Value1,
    Value2
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TypeDocumentationAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a public record with XML documentation does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void PublicRecordWithDocumentation_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
/// <summary>
/// This is a documented public record.
/// </summary>
public record DocumentedRecord(string Name, int Value);";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a public record without XML documentation triggers a diagnostic.
    /// </summary>
    [Fact]
    public void PublicRecordWithoutDocumentation_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public record UndocumentedRecord(string Name, int Value);";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TypeDocumentationAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a private class does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void PrivateClass_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
private class PrivateClass
{
    public void Method() { }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that an internal class does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void InternalClass_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
internal class InternalClass
{
    public void Method() { }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the TypeDocumentationAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new TypeDocumentationAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}
