using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Style.MethodStyle;

namespace StrictCSharp.Analyzers.Tests.Style.MethodStyle;

/// <summary>
/// Tests for the <see cref="GuardClauseAnalyzer"/> to ensure it correctly identifies
/// manual parameter validation that should use Ardalis.GuardClauses.
/// </summary>
public class GuardClauseAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer can be instantiated and has the correct diagnostic configuration.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldCanBeInstantiated()
    {
        // Arrange & Act
        var analyzer = new GuardClauseAnalyzer();

        // Assert
        analyzer.Should().NotBeNull();
        analyzer.SupportedDiagnostics.Should().HaveCount(1);
        analyzer.SupportedDiagnostics[0].Id.Should().Be("SC121");
        analyzer.SupportedDiagnostics[0].DefaultSeverity.Should().Be(DiagnosticSeverity.Error);
    }

    /// <summary>
    /// Verifies that the analyzer has the correct diagnostic properties including title, message format, and category.
    /// </summary>
    [Fact]
    public void Analyzer_WhenCalled_ShouldHasCorrectDiagnosticProperties()
    {
        // Arrange
        var analyzer = new GuardClauseAnalyzer();
        var diagnostic = analyzer.SupportedDiagnostics[0];

        // Assert
        diagnostic.Title.ToString().Should().Be("Use Ardalis.GuardClauses for parameter validation");
        diagnostic.MessageFormat.ToString().Should().Be("Use Guard.Against.{0} instead of manual validation");
        diagnostic.Category.Should().Be("Style");
        diagnostic.IsEnabledByDefault.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that null coalescing with throw expression triggers a diagnostic.
    /// </summary>
    [Fact]
    public void NullCoalescingWithThrowExpression_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        var value = parameter ?? throw new ArgumentNullException(nameof(parameter));
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.Null");
    }

    /// <summary>
    /// Verifies that Guard.Against call with nameof triggers a diagnostic for descriptive message.
    /// </summary>
    [Fact]
    public void GuardAgainstCallWithNameof_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using Ardalis.GuardClauses;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        Guard.Against.Null(parameter, nameof(parameter));
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("with descriptive message");
    }

    /// <summary>
    /// Verifies that proper Guard.Against usage with descriptive message does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void ProperGuardAgainstUsageWithMessage_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Ardalis.GuardClauses;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        Guard.Against.Null(parameter, ""Parameter cannot be null"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that Guard.Against calls without nameof do not trigger the descriptive message diagnostic.
    /// </summary>
    [Fact]
    public void GuardAgainstCallsWithoutNameof_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using Ardalis.GuardClauses;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        Guard.Against.Null(parameter);
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that null check without ArgumentException does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void NullCheckWithoutArgumentException_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        if (parameter == null)
        {
            return;
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that regular if statements without exceptions do not trigger diagnostics.
    /// </summary>
    [Fact]
    public void RegularIfStatementsWithoutExceptions_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
public class TestClass
{
    public void TestMethod(int value)
    {
        if (value > 0)
        {
            value = value * 2;
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that if statement with null check and ArgumentNullException triggers a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithNullCheckAndThrow_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        if (parameter == null)
            throw new ArgumentNullException(nameof(parameter));
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.Null");
    }

    /// <summary>
    /// Verifies that if statement with null check using 'is null' pattern does not trigger a diagnostic (not currently supported by analyzer).
    /// </summary>
    [Fact]
    public void IfStatementWithIsNullPatternAndThrow_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(object parameter)
    {
        if (parameter is null)
            throw new ArgumentNullException(nameof(parameter));
    }
}";

        var diagnostics = GetDiagnostics(test);
        // Note: The analyzer currently only supports == null, not 'is null' pattern
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that if statement with string.IsNullOrEmpty check does not trigger a diagnostic (analyzer expects binary expression).
    /// </summary>
    [Fact]
    public void IfStatementWithIsNullOrEmptyCheckAndThrow_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
            throw new ArgumentException(""Parameter cannot be empty"", nameof(parameter));
    }
}";

        var diagnostics = GetDiagnostics(test);
        // Note: The analyzer currently expects string.IsNullOrEmpty() as part of a binary expression
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that if statement with string.IsNullOrWhiteSpace check does not trigger a diagnostic (analyzer expects binary expression).
    /// </summary>
    [Fact]
    public void IfStatementWithIsNullOrWhiteSpaceCheckAndThrow_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter))
            throw new ArgumentException(""Parameter cannot be whitespace"", nameof(parameter));
    }
}";

        var diagnostics = GetDiagnostics(test);
        // Note: The analyzer currently expects string.IsNullOrWhiteSpace() as part of a binary expression
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that if statement with string.IsNullOrEmpty in binary expression and ArgumentException triggers a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithIsNullOrEmptyBinaryExpressionAndThrow_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        if (string.IsNullOrEmpty(parameter) == true)
            throw new ArgumentException(""Parameter cannot be empty"", nameof(parameter));
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.NullOrEmpty");
    }

    /// <summary>
    /// Verifies that if statement with collection Count check and ArgumentException triggers a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithCollectionCountCheckAndThrow_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;
using System.Collections.Generic;

public class TestClass
{
    public void TestMethod(List<int> items)
    {
        if (items.Count == 0)
            throw new ArgumentException(""Collection cannot be empty"", nameof(items));
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.NullOrEmpty");
    }

    /// <summary>
    /// Verifies that if statement with array Length check and ArgumentException triggers a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithArrayLengthCheckAndThrow_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(int[] items)
    {
        if (items.Length == 0)
            throw new ArgumentException(""Array cannot be empty"", nameof(items));
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.NullOrEmpty");
    }

    /// <summary>
    /// Verifies that if statement with less than range check and ArgumentOutOfRangeException triggers a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithLessThanRangeCheckAndThrow_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), ""Value must be non-negative"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.OutOfRange");
    }

    /// <summary>
    /// Verifies that if statement with greater than range check and ArgumentOutOfRangeException triggers a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithGreaterThanRangeCheckAndThrow_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(int value)
    {
        if (value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), ""Value must not exceed 100"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.OutOfRange");
    }

    /// <summary>
    /// Verifies that if statement with less than or equal range check and ArgumentOutOfRangeException triggers a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithLessThanOrEqualRangeCheckAndThrow_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(int value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), ""Value must be positive"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.OutOfRange");
    }

    /// <summary>
    /// Verifies that if statement with greater than or equal range check and ArgumentOutOfRangeException triggers a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithGreaterThanOrEqualRangeCheckAndThrow_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(int value)
    {
        if (value >= 100)
            throw new ArgumentOutOfRangeException(nameof(value), ""Value must be less than 100"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.OutOfRange");
    }

    /// <summary>
    /// Verifies that if statement with default value check and ArgumentException triggers a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithDefaultValueCheckAndThrow_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(DateTime date)
    {
        if (date == default)
            throw new ArgumentException(""Date cannot be default"", nameof(date));
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.Default");
    }

    /// <summary>
    /// Verifies that if statement with default(T) value check and ArgumentException triggers a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithDefaultTValueCheckAndThrow_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(Guid id)
    {
        if (id == default(Guid))
            throw new ArgumentException(""ID cannot be default"", nameof(id));
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.Default");
    }

    /// <summary>
    /// Verifies that if statement with block syntax for null check does not trigger a diagnostic (analyzer limitation).
    /// </summary>
    [Fact]
    public void IfStatementWithBlockSyntaxNullCheckAndThrow_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        if (parameter == null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        // Note: The analyzer currently only handles single statement throws, not block statements
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that if statement with different ArgumentException types triggers appropriate diagnostics.
    /// </summary>
    [Fact]
    public void IfStatementWithDifferentArgumentExceptionTypes_WhenCalled_ShouldReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        if (parameter == null)
            throw new ArgumentException(""Parameter is null"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[0].GetMessage().Should().Contain("Guard.Against.Null");
    }

    /// <summary>
    /// Verifies that if statement with null check but non-ArgumentException does not trigger a diagnostic.
    /// </summary>
    [Fact]
    public void IfStatementWithNullCheckButNonArgumentException_WhenCalled_ShouldNotReportDiagnostic()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(string parameter)
    {
        if (parameter == null)
            throw new InvalidOperationException(""Something went wrong"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that multiple validation checks in same method trigger multiple diagnostics.
    /// </summary>
    [Fact]
    public void MultipleValidationChecksInSameMethod_WhenCalled_ShouldReportMultipleDiagnostics()
    {
        var test = @"
using System;

public class TestClass
{
    public void TestMethod(string name, int age)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        if (age < 0)
            throw new ArgumentOutOfRangeException(nameof(age));
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(2);
        diagnostics[0].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
        diagnostics[1].Id.Should().Be(GuardClauseAnalyzer.DiagnosticId);
    }

    /// <summary>
    /// Helper method to get diagnostics from source code using the GuardClauseAnalyzer.
    /// </summary>
    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new GuardClauseAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}
