---
paths:
    - "**/*Test*.cs"
---

# C# Testing Guidelines

## Naming

- Test classes: `Given<Context>` — e.g. `GivenAnAccount`, `GivenADatabaseReadyForSync`
- Test methods: `when_<action>_then_<outcome>` snake_case
- AAA pattern, blank lines between sections, no section comments.
- Shouldly assertions. NSubstitute mocks (prefer real instances).
