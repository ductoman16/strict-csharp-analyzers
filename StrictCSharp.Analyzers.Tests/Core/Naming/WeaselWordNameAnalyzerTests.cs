using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Core.Naming;

namespace StrictCSharp.Analyzers.Tests.Core.Naming;

/// <summary>
/// Tests for the <see cref="WeaselWordNameAnalyzer"/> to ensure it correctly identifies
/// type names that end with weasel words like 'Service', 'Manager', 'Helper', or 'Util'.
/// </summary>
public class WeaselWordNameAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new WeaselWordNameAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC041");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new WeaselWordNameAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Type name contains a weasel word");
        diagnostic.MessageFormat.ToString().Should().Be("Type name '{0}' ends with a weasel word ('{1}'). Use a more descriptive name.");
        diagnostic.Category.Should().Be("Naming");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a class with a descriptive name does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void ClassWithDescriptiveName_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
public class UserRepository
{
    public void SaveUser() { }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a class ending with 'Service' triggers a diagnostic.
    /// </summary>
    [Fact]
    public void ClassEndingWithService_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public class UserService
{
    public void ProcessUser() { }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(WeaselWordNameAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a class ending with 'Manager' triggers a diagnostic.
    /// </summary>
    [Fact]
    public void ClassEndingWithManager_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public class UserManager
{
    public void ManageUser() { }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(WeaselWordNameAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a class ending with 'Helper' triggers a diagnostic.
    /// </summary>
    [Fact]
    public void ClassEndingWithHelper_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public class StringHelper
{
    public static string FormatString(string input) => input;
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(WeaselWordNameAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a class ending with 'Util' triggers a diagnostic.
    /// </summary>
    [Fact]
    public void ClassEndingWithUtil_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public class DateUtil
{
    public static DateTime GetCurrentDate() => DateTime.Now;
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(WeaselWordNameAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that an interface ending with 'Service' triggers a diagnostic.
    /// </summary>
    [Fact]
    public void InterfaceEndingWithService_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public interface IUserService
{
    void ProcessUser();
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(WeaselWordNameAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a record ending with 'Manager' triggers a diagnostic.
    /// </summary>
    [Fact]
    public void RecordEndingWithManager_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
public record UserManager(string Name, int Id);";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(WeaselWordNameAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a class containing but not ending with a weasel word does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void ClassContainingButNotEndingWithWeaselWord_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
public class ServiceRepository
{
    public void SaveService() { }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that multiple types with weasel words trigger multiple diagnostics.
    /// </summary>
    [Fact]
    public void MultipleTypesWithWeaselWords_WhenCalled_ShouldReportMultipleDiagnostics()
    {
        var test = @"
public class UserService
{
    public void ProcessUser() { }
}

public class DataManager
{
    public void ManageData() { }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(2);
        diagnostics.All(d => d.Id == WeaselWordNameAnalyzer.DiagnosticId).Should().BeTrue();
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the WeaselWordNameAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new WeaselWordNameAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}
