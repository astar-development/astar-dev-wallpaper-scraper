---
description: This file describes the C# testing guidelines for the project.
applyTo: "**/*Test*.cs"
---

# C# Testing Guidelines

## Setup

- Avoid mocks whenever possible. Use real instances of classes instead of mocks to ensure that tests are more reliable and reflect actual behavior. Swap out dependencies with mocks only when necessary, such as when testing error handling or when IO is involved. For example, if a class depends on a file system, use a mock for the file system to avoid actual file IO during tests.
- When instantiating the system under test (SUT), use a helper method to create the SUT with all its dependencies. This keeps the test code clean and focused on the behavior being tested. For example, create a `CreateSut` method that sets up the SUT with either real instances or mocks as needed.

## Naming

- Test classes: `Given<Context>` — e.g. `GivenAnAccount`, `GivenADatabaseReadyForSync`
- Test methods: `when_<action>_then_<outcome>` snake_case
- AAA pattern, blank lines between sections, no section comments.
- Shouldly assertions. NSubstitute mocks (prefer real instances).

## Assertions

Test assertions should be explicit and not rely on implicit behavior. For example, instead of using `Path.Combine` to construct expected paths in tests, use hardcoded strings to ensure clarity and avoid platform-specific issues.

Never:

```csharp
[Fact]
public void when_the_root_directory_starts_with_a_drive_letter_then_the_drive_colon_is_preserved()
{
    var layout = new DirectoryLayout("C:/w", "/regular", "/famous");

    var directory = WallpaperDirectoryResolver.Resolve(layout, [], category, []);

    directory.ShouldBe(Path.Combine("C:/w/regular", "L", category.Name));
}
```

instead:

```csharp
[Fact]
public void when_the_root_directory_starts_with_a_drive_letter_then_the_drive_colon_is_preserved()
{
    var layout = new DirectoryLayout("C:/w", "/regular", "/famous");

    var directory = WallpaperDirectoryResolver.Resolve(layout, [], category, []);

    directory.ShouldBe("C:/w/regular/L/Landscapes");
}
```