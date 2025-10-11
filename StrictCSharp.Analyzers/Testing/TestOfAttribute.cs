using System;

namespace StrictCSharp.Analyzers.Testing;

/// <summary>
/// Attribute used to mark the class that a particular unit test file is testing.
/// This attribute helps enforce proper test organization and namespace structure.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TestOfAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the TestOfAttribute class.
    /// </summary>
    /// <param name="classUnderTest">The type of the class being tested</param>
    public TestOfAttribute(Type classUnderTest)
    {
        ClassUnderTest = classUnderTest;
    }

    /// <summary>
    /// Gets the type of the class being tested.
    /// </summary>
    public Type ClassUnderTest { get; }
}
