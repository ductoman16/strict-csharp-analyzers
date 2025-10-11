using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using StrictCSharp.Analyzers.Testing.Organization;

namespace StrictCSharp.Analyzers.Tests.Testing.Organization;

/// <summary>
/// Tests for the <see cref="TestClassNamespaceAnalyzer"/> to ensure it correctly validates
/// namespace structure for test classes with TestOf attributes.
/// </summary>
public class TestClassNamespaceAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new TestClassNamespaceAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC244");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new TestClassNamespaceAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Test class with TestOf attribute must follow correct namespace structure");
        diagnostic.MessageFormat.ToString().Should().Be("Test class '{0}' should be in namespace '{1}' but is in '{2}'");
        diagnostic.Category.Should().Be("Testing");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a test class without TestOf attribute does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithoutTestOfAttribute_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Xunit;

namespace SomeNamespace;

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
    /// Verifies that a test class with correct namespace structure does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithCorrectNamespace_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

namespace PauseButtonDashboard.Core.Orders
{
    public class OrderSummary
    {
        public void SomeMethod() { }
    }
}

namespace PauseButtonDashboard.Core.Tests.Orders.OrderSummaryTests
{
    [PauseButtonDashboard.Analyzers.TestOf(typeof(PauseButtonDashboard.Core.Orders.OrderSummary))]
    public class OrderSummaryTests
    {
        public void TestMethod()
        {
            // Test implementation
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class with incorrect namespace structure triggers a diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithIncorrectNamespace_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

namespace WrongNamespace
{
    public class DummyClass
    {
        public void SomeMethod() { }
    }

    [PauseButtonDashboard.Analyzers.TestOf(typeof(DummyClass))]
    public class DummyClassTests
    {
        public void TestMethod()
        {
            // Test implementation
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TestClassNamespaceAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("DummyClassTests");
    }

    /// <summary>
    /// Verifies that a test class testing a class from a simple namespace follows correct structure.
    /// </summary>
    [Fact]
    public void TestClassForSimpleNamespace_WhenCorrect_ShouldNotReportDiagnostic()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

namespace MyProject
{
    public class User
    {
        public string Name { get; set; }
    }
}

namespace MyProject.Tests.UserTests
{
    [PauseButtonDashboard.Analyzers.TestOf(typeof(MyProject.User))]
    public class UserTests
    {
        public void TestMethod()
        {
            // Test implementation
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class testing a class from a complex namespace follows correct structure.
    /// </summary>
    [Fact]
    public void TestClassForComplexNamespace_WhenCorrect_ShouldNotReportDiagnostic()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

namespace PauseButtonDashboard.Core.Publishing
{
    public class PublishingStatusOperations
    {
        public void Execute() { }
    }
}

namespace PauseButtonDashboard.Core.Tests.Publishing.PublishingStatusOperationsTests
{
    [PauseButtonDashboard.Analyzers.TestOf(typeof(PauseButtonDashboard.Core.Publishing.PublishingStatusOperations))]
    public class PublishingStatusOperationsTests
    {
        public void TestMethod()
        {
            // Test implementation
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class testing a class from a complex namespace with wrong structure triggers diagnostic.
    /// </summary>
    [Fact]
    public void TestClassForComplexNamespace_WhenIncorrect_ShouldReportDiagnostic()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

namespace PauseButtonDashboard.Core.Publishing
{
    public class PublishingStatusOperations
    {
        public void Execute() { }
    }
}

namespace PauseButtonDashboard.Core.Publishing.PublishingStatusOperationsTests
{
    [PauseButtonDashboard.Analyzers.TestOf(typeof(PauseButtonDashboard.Core.Publishing.PublishingStatusOperations))]
    public class PublishingStatusOperationsTests
    {
        public void TestMethod()
        {
            // Test implementation
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TestClassNamespaceAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("PauseButtonDashboard.Core.Tests.Publishing.PublishingStatusOperationsTests");
    }

    /// <summary>
    /// Verifies that a test class with TestOf attribute using fully qualified name works correctly.
    /// </summary>
    [Fact]
    public void TestClassWithFullyQualifiedTestOfAttribute_WhenCorrect_ShouldNotReportDiagnostic()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

namespace PauseButtonDashboard.Core.Orders
{
    public class OrderSummary
    {
        public void Calculate() { }
    }
}

namespace PauseButtonDashboard.Core.Tests.Orders.OrderSummaryTests
{
    [PauseButtonDashboard.Analyzers.TestOf(typeof(PauseButtonDashboard.Core.Orders.OrderSummary))]
    public class OrderSummaryTests
    {
        public void TestMethod()
        {
            // Test implementation
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class without namespace does not trigger diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithoutNamespace_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

public class SimpleClass
{
    public void DoSomething() { }
}

[PauseButtonDashboard.Analyzers.TestOf(typeof(SimpleClass))]
public class SimpleClassTests
{
    public void TestMethod()
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class with invalid TestOf attribute (no typeof) does not trigger diagnostic.
    /// </summary>
    [Fact]
    public void TestClassWithInvalidTestOfAttribute_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

namespace SomeNamespace
{
    public class SomeClass
    {
        public void DoSomething() { }
    }

    // This TestOf attribute is invalid because it has no arguments
    [PauseButtonDashboard.Analyzers.TestOf]
    public class SomeClassTests
    {
        public void TestMethod()
        {
            // Test implementation
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class testing a class from single-part namespace follows correct structure.
    /// </summary>
    [Fact]
    public void TestClassForSinglePartNamespace_WhenCorrect_ShouldNotReportDiagnostic()
    {
        var test = @"
using PauseButtonDashboard.Analyzers;

namespace MyProject.Tests.UserTests;

public class User
{
    public string Name { get; set; }
}

[TestOf(typeof(User))]
public class UserTests
{
    public void TestMethod()
    {
        // Test implementation
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a test class testing a class from single-part namespace with wrong structure triggers diagnostic.
    /// </summary>
    [Fact]
    public void TestClassForSinglePartNamespace_WhenIncorrect_ShouldReportDiagnostic()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

namespace MyProject
{
    public class User
    {
        public string Name { get; set; }
    }
}

namespace WrongNamespace
{
    [PauseButtonDashboard.Analyzers.TestOf(typeof(MyProject.User))]
    public class UserTests
    {
        public void TestMethod()
        {
            // Test implementation
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TestClassNamespaceAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("UserTests");
    }

    /// <summary>
    /// Verifies that a test class with multiple TestOf attributes only analyzes the first one.
    /// </summary>
    [Fact]
    public void TestClassWithMultipleTestOfAttributes_WhenCalled_ShouldAnalyzeFirstAttribute()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

namespace PauseButtonDashboard.Core.Orders
{
    public class OrderSummary
    {
        public void Calculate() { }
    }

    public class AnotherClass
    {
        public void DoSomething() { }
    }
}

namespace PauseButtonDashboard.Core.Tests.Orders.OrderSummaryTests
{
    [PauseButtonDashboard.Analyzers.TestOf(typeof(PauseButtonDashboard.Core.Orders.OrderSummary))]
    [PauseButtonDashboard.Analyzers.TestOf(typeof(PauseButtonDashboard.Core.Orders.AnotherClass))]
    public class OrderSummaryTests
    {
        public void TestMethod()
        {
            // Test implementation
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a non-test class with TestOf attribute still gets analyzed.
    /// </summary>
    [Fact]
    public void NonTestClassWithTestOfAttribute_WhenIncorrectNamespace_ShouldReportDiagnostic()
    {
        var test = @"
namespace PauseButtonDashboard.Analyzers
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TestOfAttribute : System.Attribute
    {
        public TestOfAttribute(System.Type classUnderTest) { }
    }
}

namespace SomeNamespace
{
    public class SomeClass
    {
        public void DoSomething() { }
    }
}

namespace WrongNamespace
{
    [PauseButtonDashboard.Analyzers.TestOf(typeof(SomeNamespace.SomeClass))]
    public class SomeClassTests
    {
        public void RegularMethod()
        {
            // Not a test method
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(TestClassNamespaceAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the TestOfNamespaceAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new TestClassNamespaceAnalyzer();
        var diagnostics = compilation.WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return [.. diagnostics];
    }
}
