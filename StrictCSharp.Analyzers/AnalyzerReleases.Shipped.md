## Release 0.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SC201 | Testing | Error | Changed from Warning to Error - Use FluentAssertions instead of xUnit Assert methods
SC221 | Testing | Error | Changed from Warning to Error - Test methods must have Arrange/Act/Assert comments
SC222 | Testing | Error | Changed from Warning to Error - Multiple assertions require AssertionScope
SC241 | Testing | Error | Changed from Warning to Error - Test classes must end with "Tests" suffix
SC243 | Testing | Error | Changed from Warning to Error - Test classes must have TestOf attribute

## Release 0.0.1

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SC021 | Documentation | Error | TypeDocumentationAnalyzer - prohibits public types without XML documentation
SC041 | Naming | Error | WeaselWordNameAnalyzer - prohibits type names ending with 'Service', 'Manager', 'Helper', or 'Util'

SC101 | Style | Error | UsingBlockAtEndAnalyzer - prohibits using blocks as the last statement in a method; require using statement expressions instead
SC121 | Style | Error | GuardClauseAnalyzer - prohibits manual parameter validation instead of Ardalis.GuardClauses
SC122 | Style | Error | GuardClauseNameofAnalyzer - prohibits using nameof for the second parameter in Guard.Against calls
SC141 | Style | Error | NoInlineCommentsAnalyzer - prohibits inline comments (// or /**/)

SC201 | Testing | Warning | NoXUnitAssertionsAnalyzer - prohibits xUnit Assert methods instead of FluentAssertions
SC204 | Testing | Error | NoTaskDelayAnalyzer - prohibits await Task.Delay() in unit tests
SC221 | Testing | Warning | ArrangeActAssertAnalyzer - requires test methods have Arrange/Act/Assert comments
SC222 | Testing | Warning | FluentAssertionScopeAnalyzer - prohibits multiple assertions without AssertionScope
SC223 | Testing | Error | CategoryTraitAnalyzer - enforces [Trait("Category", ...)] on test classes
SC241 | Testing | Warning | TestClassMustEndWithTestsAnalyzer - requires that test classes end with the suffix"Tests"
SC242 | Testing | Error | TestMethodNamingAnalyzer - enforces 3-segment test method naming: MethodName_Scenario_ExpectedResult
SC243 | Testing | Warning | TestOfRequiredAnalyzer - ensures all test classes have TestOf attribute
SC244 | Testing | Error | TestOfNamespaceAnalyzer - enforces namespace structure for test classes with TestOf attribute

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
