using FluentAssertions;
using StrictCSharp.Analyzers.Testing.Organization;

namespace StrictCSharp.Analyzers.Tests.Testing.Organization;

/// <summary>
/// Tests for the <see cref="TestOfRequiredAnalyzer"/> to ensure it correctly identifies
/// test classes missing the required TestOf attribute.
/// </summary>
public class TestOfRequiredAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new TestOfRequiredAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC243");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new TestOfRequiredAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Test class must have TestOf attribute");
        diagnostic.MessageFormat.ToString().Should().Be("Test class '{0}' must have a [TestOf(typeof(ClassUnderTest))] attribute");
        diagnostic.Category.Should().Be("Testing");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }
}
