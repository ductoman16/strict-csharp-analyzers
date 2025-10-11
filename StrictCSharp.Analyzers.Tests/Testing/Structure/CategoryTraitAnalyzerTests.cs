using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Testing.Structure;

namespace StrictCSharp.Analyzers.Tests.Testing.Structure;

/// <summary>
/// Tests for the <see cref="CategoryTraitAnalyzer"/> to ensure it correctly identifies
/// test classes missing the required [Trait("Category", ...)] attribute.
/// </summary>
public class CategoryTraitAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new CategoryTraitAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC223");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new CategoryTraitAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Test class must have a Category Trait");
        diagnostic.MessageFormat.ToString().Should().Be("Test class '{0}' must have a [Trait(\"Category\", ...)] attribute");
        diagnostic.Category.Should().Be("Testing");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a test class without a Category trait attribute triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithoutCategoryTrait_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(CategoryTraitAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("TestClass");
    }

    /// <summary>
    /// Verifies that a test class with a proper Category trait attribute does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithCategoryTrait_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

[Trait(""Category"", ""Unit"")]
public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class with Theory methods and Category trait does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithTheoryAndCategoryTrait_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

[Trait(""Category"", ""Integration"")]
public class TestClass
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void TestMethod(int value)
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class with Theory methods but no Category trait triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithTheoryButNoCategoryTrait_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void TestMethod(int value)
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(CategoryTraitAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a non-test class (no test methods) does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void NonTestClass_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
public class RegularClass
{
    public void RegularMethod()
    {
        // Regular implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class with mixed Fact and Theory methods but no Category trait triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithMixedTestMethodsButNoCategoryTrait_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

public class TestClass
{
    [Fact]
    public void FactTestMethod()
    {
        // Test implementation
    }

    [Theory]
    [InlineData(""test"")]
    public void TheoryTestMethod(string value)
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(CategoryTraitAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test class with Category trait using fully qualified name does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithFullyQualifiedCategoryTrait_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

[Xunit.Trait(""Category"", ""Unit"")]
public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class with non-Category trait but no Category trait triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithOtherTraitButNoCategoryTrait_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

[Trait(""Priority"", ""High"")]
public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(CategoryTraitAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that a test class with multiple traits including Category trait does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithMultipleTraitsIncludingCategory_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

[Trait(""Priority"", ""High"")]
[Trait(""Category"", ""Integration"")]
[Trait(""Owner"", ""TeamA"")]
public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class with Category trait having insufficient arguments triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithIncompleteCategoryTrait_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

[Trait(""Category"")]
public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(CategoryTraitAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Verifies that an empty class (no methods) does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void EmptyClass_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
public class EmptyClass
{
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class with Category trait using different casing for the key does not trigger a diagnostic.
    /// Note: This tests the exact string matching behavior of the analyzer.
    /// </summary>
    [Fact]
    public void TestClassWithCaseSensitiveCategoryTrait_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Xunit;

[Trait(""category"", ""Unit"")]
public class TestClass
{
    [Fact]
    public void TestMethod()
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(CategoryTraitAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the CategoryTraitAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new CategoryTraitAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}