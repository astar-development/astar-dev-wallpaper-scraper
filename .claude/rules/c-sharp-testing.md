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