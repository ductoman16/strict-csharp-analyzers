# StrictCSharp.Analyzers

A set of strict .NET Compiler Platform (Roslyn) Analyzers for C# projects.

## Diagnostic ID Categories

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
- Ensure each analyzer explains why it's useful
- Create enum for analyzer categories
- Use published analyzers in this codebase
- Figure out consistent analyzer naming (e.g. SomethingRequiredAnalyzer or SomethingForbiddenAnalyzer) (Require/Enforce vs Forbid/Prohibit)
- Directory.build.props for consistent project settings

### New analyzers

- Warnings as errors must be enabled
- nullable context must be enabled
- Require microsoft threading analyzers
- No pramga warning disable
- dumb comment analyzer (detect if the comment is mostly just the property name)
- Single-line members must be expression bodies

#### Testing

- Only allow Xunit tests
- Forbid multiple assertions per test
- Test classes must only test a single method
- Test methods must follow the pattern ScenarioUnderTest_ExpectedBehavior (currently 3 segments)
