# StrictCSharp.Analyzers

A set of strict .NET Compiler Platform (Roslyn) Analyzers for C# projects.

## Installation

Install the package via NuGet:

```bash
dotnet add package StrictCSharp.Analyzers
```

Or add it to your `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="StrictCSharp.Analyzers" Version="0.0.2" />
</ItemGroup>
```

## Available Analyzers

All analyzers are configured as errors by default to enforce strict coding standards. See [Configuration](#configuration) for more details on how to disable or change the severity of individual analyzers.

### Documentation Analyzers

#### SC021 - TypeDocumentationAnalyzer

**Definition:**
Requires all public types to have XML documentation.

**Motivation:**
Public APIs should explain both what they do and why they do it, to help consumers understand how to use them. This is especially important in the age of AI-generated code, for helping both humans and AIs with understanding.

While excessive documentation can be hard to maintain, requiring XML documentation on only public types provides good value with minimal maintenance effort.

---

### Naming Analyzers

#### SC041 - WeaselWordNameAnalyzer

**Definition:** Prohibits type names ending with generic suffixes like 'Service', 'Manager', 'Helper', or 'Util'.

**Motivation:** These suffixes do not convey meaningful information about what the class actually does, and promote classes with multiple responsibilities. If you find yourself using a weasel word, you should think of a better abstraction.

---

### Style Analyzers

#### SC101 - UsingBlockAtEndAnalyzer

**Definition:** Requires using statement expressions instead of using blocks when the using is the last statement in a method.

**Motivation:** Using statement expressions (`using var x = ...`) are more concise and reduce nesting compared to using blocks (`using (var x = ...) { ... }`). When the using is the last statement, the block adds unnecessary indentation without providing value.

#### SC121 - ManualParameterValidationAnalyzer

**Definition:** Requires using Ardalis.GuardClauses for parameter validation instead of manual if-throw patterns.

**Motivation:** Guard clauses provide a concise and consistent way to validate parameters. The Ardalis.GuardClauses library is the most well fleshed-out and widely used guard clause library in the .NET ecosystem.

#### SC122 - GuardClauseNameofAnalyzer

**Definition:** Prohibits using nameof for the second parameter in Guard.Against calls.

**Motivation:** The Ardalis.GuardClauses library uses CallerArgumentExpression on the second parameter to automatically capture the expression name. Using nameof is redundant and unnecessary.

#### SC141 - NoInlineCommentsAnalyzer

**Definition:** Prohibits inline comments (`//` or `/* */`).

**Motivation:** Code should be self-explanatory. If you need a comment to explain what code does, consider extracting it into a well-named method instead. This is especially important in the age of AI-generated code, because LLMs have a tendency to sprinkle comments absolutely everywhere.

For public APIs, use [XML documentation comments](#sc021---typedocumentationanalyzer) instead.

---

### Testing Analyzers

#### SC201 - NoXUnitAssertionsAnalyzer

**Definition:** Requires using FluentAssertions instead of xUnit's Assert methods.

**Motivation:** FluentAssertions provides more readable assertions with better error messages. Compare `Assert.Equal(expected, actual)` to `actual.Should().Be(expected)`. FluentAssertions also provides detailed failure messages that show exactly what went wrong.

#### SC204 - NoTaskDelayAnalyzer

**Definition:** Prohibits `await Task.Delay()` in tests.

**Motivation:** Unit tests should be fast and deterministic. `Task.Delay()` makes tests slow and can introduce flakiness due to timing issues.

#### SC221 - ArrangeActAssertAnalyzer

**Definition:** Requires test methods to have `// Arrange`, `// Act`, and `// Assert` comments marking each section.

**Motivation:** The Arrange-Act-Assert pattern makes tests easier to read and understand. Explicit comments enforce this structure and help readers quickly identify what's being set up, what action is being tested, and what's being verified. Additionally, explicit sections denoted by comments make it easy to see when one of the sections is getting too large, and could benefit from refactoring.

#### SC222 - FluentAssertionScopeAnalyzer

**Definition:** Requires using `AssertionScope` when a test method has multiple FluentAssertions.

**Motivation:** Without `AssertionScope`, tests stop at the first failed assertion. With `AssertionScope`, all assertions are evaluated and you see all failures at once, making it faster to fix multiple issues.

#### SC223 - CategoryTraitAnalyzer

**Definition:** Requires test classes to have a `[Trait("Category", ...)]` attribute.

**Motivation:** Categorizing tests allows you to run specific subsets of tests (e.g., unit tests vs integration tests). This also helps developers understand which tests are unit tests and should _not_ make external calls, and which tests are integration tests and can make external calls.

#### SC241 - TestClassMustEndWithTestsAnalyzer

**Definition:** Requires test classes to end with the suffix "Tests".

**Motivation:** Consistent and explicit naming conventions make it easy to identify test classes at a glance.

#### SC242 - TestMethodNamingAnalyzer

**Definition:** Requires test methods to follow the 3-segment naming pattern: `MethodName_Scenario_ExpectedResult`.

**Motivation:** This naming convention makes test intent crystal clear. For example, `Authenticate_WithInvalidPassword_ThrowsAuthenticationException` tells you exactly what's being tested, under what conditions, and what should happen.

#### SC243 - TestOfRequiredAnalyzer

**Definition:** Requires test classes to have a `[TestOf(typeof(...))]` attribute.

**Motivation:** This attribute explicitly links test classes to the code they test, making it easy to find tests for a given class and ensuring tests are organized around the code under test.

#### SC244 - TestClassNamespaceAnalyzer

**Definition:** Requires test class namespaces to match the namespace of the class under test.

**Motivation:** Mirroring namespaces between production code and tests makes it easy to navigate between them, and easy to find which classes are missing tests.

## Configuration

All analyzers are enabled by default when you install the package. You can selectively enable or disable analyzers using an `.editorconfig` file in your project.

### Disabling Individual Analyzers

To disable specific analyzers, add entries to your `.editorconfig` file:

```ini
root = true

[*.cs]
# Disable specific analyzers by their diagnostic ID
dotnet_diagnostic.SC021.severity = none  # Type documentation
dotnet_diagnostic.SC041.severity = none  # Weasel word names
dotnet_diagnostic.SC141.severity = none  # Inline comments
```

### Disabling Analyzers by Category

You can disable entire categories of analyzers:

```ini
[*.cs]
# Disable all testing rules
dotnet_analyzer_diagnostic.category-Testing.severity = none

# Disable all documentation rules
dotnet_analyzer_diagnostic.category-Documentation.severity = none

# Disable all naming rules
dotnet_analyzer_diagnostic.category-Naming.severity = none

# Disable all style rules
dotnet_analyzer_diagnostic.category-Style.severity = none
```

### Changing Severity Levels

You can also change the severity of analyzers instead of disabling them:

```ini
[*.cs]
# Change from error to warning
dotnet_diagnostic.SC021.severity = warning

# Change from warning to error
dotnet_diagnostic.SC201.severity = error

# Available severity levels: none, silent, suggestion, warning, error
```

## Analyzer Categories

The analyzers use a structured diagnostic ID system with the prefix `SC` (StrictCSharp) followed by a three-digit number:

### 0xx - Core/Fundamental Rules (000-099)

Rules that enforce fundamental project configuration and essential code quality standards.

- **000-019**: Project Configuration
  - Project-level settings like warnings as errors, nullable context, required analyzer packages

- **020-039**: Documentation Requirements
  - XML documentation requirements, comment quality checks

- **040-059**: Naming Conventions
  - Type and member naming rules, forbidden naming patterns

### 1xx - Style/Code Quality Rules (100-199)

Rules that enforce consistent coding style and improve code readability.

- **100-119**: Using Statements & Resource Management
  - Using statement patterns, resource disposal

- **120-139**: Method/Member Style
  - Expression bodies, guard clauses, method structure

- **140-159**: Comment Style
  - Inline comment restrictions, comment formatting

### 2xx - Testing Rules (200-299)

Rules specific to test code organization, structure, and quality.

- **200-219**: Test Framework Requirements
  - Allowed testing frameworks, assertion libraries, forbidden test patterns

- **220-239**: Test Structure
  - Test attributes, test organization, assertion patterns

- **240-259**: Test Naming & Organization
  - Test class naming, namespace organization, method naming patterns

### 9xx - Meta/Configuration Rules (900-999)

Rules about analyzer configuration and rule management.

- **900-919**: Analyzer Configuration
- **920-939**: Suppression Rules
- **940-959**: Severity Configuration

## Roadmap

### Internal improvements

- Set up nuget package publish
- Document motivation for the project in this readme
- Ensure each analyzer explains Motivation
- Create enum for analyzer categories
- Use published analyzers in this codebase
- Convert to centralized package management

### Analyzer improvements

- Figure out consistent analyzer naming (e.g. SomethingRequiredAnalyzer or SomethingForbiddenAnalyzer) (Require/Enforce vs Forbid/Prohibit)
- Directory.build.props for consistent project settings
- TestCategory enum/constants
- Remove region support from AAA analyzer
- Update TestOf to also specify the method under test

### New analyzers

- Warnings as errors must be enabled
- nullable context must be enabled
- Require microsoft threading analyzers
- No pramga warning disable
- dumb comment analyzer (detect if the comment is mostly just the property name)
- Single-line members must be expression bodies
- Class name must match file name
- Don't allow empty param/return tags in XML comments
- Limit C# file length to like 300 lines?

#### Testing

- Only allow Xunit tests
- Forbid multiple assertions per test
- Test classes must only test a single method
- Test methods must follow the pattern ScenarioUnderTest_ExpectedBehavior (currently 3 segments)
