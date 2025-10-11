using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using StrictCSharp.Analyzers.Style.UsingStatements;

namespace StrictCSharp.Analyzers.Tests.Style.UsingStatements;

public class UsingBlockAtEndAnalyzerTests
{
    [Fact]
    public void UsingBlockAtEndOfMethod_WhenCalled_ShouldReportsDiagnostic()
    {
        var test = @"
using System;

class TestClass
{
    public void TestMethod()
    {
        using (var stream = new System.IO.MemoryStream())
        {
            // Some code
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(UsingBlockAtEndAnalyzer.DiagnosticId);
    }

    [Fact]
    public void UsingStatementExpressionAtEndOfMethod_WhenCalled_ShouldNoDiagnostic()
    {
        var test = @"
using System;

class TestClass
{
    public void TestMethod()
    {
        using var stream = new System.IO.MemoryStream();
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void UsingBlockNotAtEndOfMethod_WhenCalled_ShouldNoDiagnostic()
    {
        var test = @"
using System;

class TestClass
{
    public void TestMethod()
    {
        using (var stream = new System.IO.MemoryStream())
        {
            // Some code
        }
        Console.WriteLine(""After using block"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void UsingBlockInMiddleOfMethod_WhenCalled_ShouldNoDiagnostic()
    {
        var test = @"
using System;

class TestClass
{
    public void TestMethod()
    {
        Console.WriteLine(""Before using block"");
        using (var stream = new System.IO.MemoryStream())
        {
            // Some code
        }
        Console.WriteLine(""After using block"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void UsingStatementExpressionInMiddleOfMethod_WhenCalled_ShouldNoDiagnostic()
    {
        var test = @"
using System;

class TestClass
{
    public void TestMethod()
    {
        Console.WriteLine(""Before using statement"");
        using var stream = new System.IO.MemoryStream();
        Console.WriteLine(""After using statement"");
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void EmptyMethod_WhenCalled_ShouldNoDiagnostic()
    {
        var test = @"
using System;

class TestClass
{
    public void TestMethod()
    {
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void MethodWithOnlyUsingStatementExpression_WhenCalled_ShouldNoDiagnostic()
    {
        var test = @"
using System;

class TestClass
{
    public void TestMethod()
    {
        using var stream = new System.IO.MemoryStream();
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void MultipleUsingBlocksAtEnd_WhenCalled_ShouldReportsDiagnosticForLastOne()
    {
        var test = @"
using System;

class TestClass
{
    public void TestMethod()
    {
        using (var stream1 = new System.IO.MemoryStream())
        {
            // Some code
        }
        using (var stream2 = new System.IO.MemoryStream())
        {
            // Some code
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(UsingBlockAtEndAnalyzer.DiagnosticId);
    }

    [Fact]
    public void UsingBlockWithExpression_WhenCalled_ShouldReportsDiagnostic()
    {
        var test = @"
using System;

class TestClass
{
    public void TestMethod()
    {
        using (new System.IO.MemoryStream())
        {
            // Some code
        }
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Id.Should().Be(UsingBlockAtEndAnalyzer.DiagnosticId);
    }

    [Fact]
    public void UsingStatementExpressionWithExpression_WhenCalled_ShouldNoDiagnostic()
    {
        var test = @"
using System;

class TestClass
{
    public void TestMethod()
    {
        using var stream = new System.IO.MemoryStream();
    }
}";

        var diagnostics = GetDiagnostics(test);
        diagnostics.Should().BeEmpty();
    }

    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new UsingBlockAtEndAnalyzer();
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .Result;

        return diagnostics.ToArray();
    }
}