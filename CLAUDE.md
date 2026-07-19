# CLAUDE.md

Guidance for Claude Code (claude.ai/code) working in this repo.

## Project Overview

AStar Dev Wallpaper Scraper: .NET 10 desktop app. Avalonia + ReactiveUI for UI, Playwright for scraping, SQLite for local storage. Auto-update via Velopack, release packages from GitHub.

Full test suite runs automatically at end of turn - only run new / affected tests.

## Project Rules

- Add XML docs on ALL production public methods / properties etc. NEVER add comments within code blocks. NO exception. NEVER document tests.
- Async methods MUST end in `Async` - exceptions: EventHandlers and tests. Neither have the suffix
- return statements MUST be proceeded by a blank line - except when they immediately follow a control statement (`if` etc.)
- ALL new code must use have GH issue, must use TDD, commit failing test first (red), confirm fail, then implement + commit production code separately (green). Never batch test + production code in one commit. New Git branch MUST be created: `feature/<gh-issue-number-if-available>-short-description` / `bug/<gh-issue-number-if-available>-short-description` / etc.
- When a class is not testable/offers little regression: add `[ExcludeFromCodeCoverage]`
- When development complete. Stop, request human review and offer to raise PR
- NEVER change code unrelated to the requested change (no judgement-call restructuring, reordering, or "while I'm here" cleanup). If a change might be beneficial (logical grouping, indirect refactor, etc.), SUGGEST it as a separate item - do NOT implement it
- Before editing a table/data store, confirm correct target. `SearchCategories` manually-maintained - never insert/restructure (progress-field updates fine). New/unrecognised tags go to `FileClassificationCategories` under "Unclassified"

## Shared Utility Placement

When adding new shared extensions/helpers:

- **AStar.Dev.Utilities:** Pure LINQ/collection helpers independent of domain types. Examples: `ForEach`, `ForEachAsync`. No dependencies on other AStar packages.
- **AStar.Dev.FunctionalParadigm:** Functional combinators, monadic operations, and LINQ bridges that return or work with `Option<T>`, `Result<T,E>`, etc. Examples: `FirstOrNone`, `FirstOrNoneAsync`, `Map`, `Bind`. Must not depend on Utilities.

**Challenge requests that specify Utilities location if:**

- Extension returns a type from FunctionalParadigm (e.g., `Option<T>`, `Result<T,E>`)
- Extension is inherently functional (monadic, transforms over types from FunctionalParadigm)
- Adding it to Utilities would create a circular dependency

**Decision framework:** Utilities → FunctionalParadigm is one-way (or not at all). FunctionalParadigm must be self-contained and not depend on Utilities.

## Build & Test

All builds target `net10.0`. Run from project root:

**Build:**

```bash
dotnet build                                         # Debug build
dotnet build --configuration Release                 # Release build
```

**Test:**

```bash
dotnet test                                          # Run all tests
dotnet test --project AStar.Dev.Utilities.Tests.Unit # Run single project
dotnet test -- --filter-class "*GivenClassName"      # Run by test class (MTP runner: VSTest --filter syntax does NOT work; xunit.v3 options go after --)
dotnet test -- --filter-method "*when_action_then*"  # Run by test method
```

**Coverage:**

```bash
bash code-coverage.sh  # Runs tests with coverage, HTML report: CoverageReport/ - runs as stop hook.
```

Before commit, run build + affected/new tests, report pass count (e.g. `206/206 passing`).

## Codebase Architecture

### Project Structure

``` Text
AStar.Dev.Wallpaper.Scraper/          Main desktop app (Avalonia/ReactiveUI, net10.0-windows)
AStar.Dev.Wallpaper.Scraper.Tests.Unit/

AStar.Dev.Infrastructure.AppDb/       Database layer (SQLite via EF Core 10)

AStar.Dev.Utilities/                  Shared utility library
AStar.Dev.Utilities.Tests.Unit/

AStar.Dev.FunctionalParadigm/         Functional programming utilities
AStar.Dev.FunctionalParadigm.Tests.Unit/

AStar.Dev.Source.Generators/          Code generation (Roslyn-based)
AStar.Dev.Source.Generators.Attributes/
AStar.Dev.Source.Generators.Tests.Unit/
AStar.Dev.Source.Generators.Attributes.Tests.Unit/

AStar.Dev.Logging.Extensions/         Serilog + Microsoft.Extensions.Logging integration
```

### Architecture Patterns

- **Avalonia + ReactiveUI:** Reactive MVVM. ViewModels inherit ReactiveObject. UI subscribes to observable streams for state changes.
- **Dependency Injection:** Microsoft.Extensions.DI, configuration-driven bootstrap (appsettings.json). App.xaml.cs and Program.cs wire services.
- **Logging:** Serilog, console + Seq sinks. Configured via appsettings.json `Logging` section.
- **Database:** EF Core 10 + SQLite. Connection string in appsettings under `scrapeConfiguration.connectionStrings.sqlite`.
- **Updates:** Velopack. App calls `VelopackApp.Build().Run()` before Avalonia startup (Program.cs). UpdateService reads GitHub releases.
- **Tests:** xunit.v3, MTP runner (pinned in global.json) via `xunit.v3.mtp-v1` — MTP v1 protocol, because C# Dev Kit Test Explorer cannot run MTP v2 tests yet ("test case did not report any output"). Keep `Microsoft.Testing.Extensions.CodeCoverage` on 17.x (18.x drags in MTP 2.x, causes TypeLoadException with mtp-v1). All test projects compile; test methods run only in projects suffixed `.Tests` or `.Tests.Unit`.

### Key Configuration

**Directory.Build.props:**

- All projects: `net10.0`, nullable reference types on, implicit usings, latest C# version
- Tests: Snake_case method names (when_[action]_then_[outcome]) + PascalCase class names (Given[Subject])
- All warnings become errors
- Coverage collector injected into test projects automatically

**Directory.Packages.props:**

- Central Package Management (CPM). Each package version declared once; projects reference without versions.
- New NuGet package: add `<PackageVersion>` entry here, reference without version in .csproj.

**appsettings.json:**

- Logging.LogLevel, Logging.Console, Logging.Serilog configured here (null = defaults)
- updateConfiguration.repositoryUrl points to GitHub (Velopack reads releases there)
- scrapeConfiguration holds app settings (database connection, app name)

## Release & Deployment

Push semver tag (`git tag v0.1.1 && git push origin v0.1.1`). Triggers release workflow:

1. Workflow derives version from tag (strips `v` prefix)
2. Publishes self-contained for linux-x64 and win-x64
3. Packs with `vpk` (Velopack CLI tool)
4. Downloads previous releases (enables delta updates)
5. Uploads to GitHub Release with `--merge` (combines both OS builds into one release)

UpdateService polls this release feed for in-app update checks.

## Test Naming Convention

Enforced via CA1707/IDE1006 suppressions in Directory.Build.props:

**Test classes:** `Given[SubjectUnderTest]`  
**Test methods:** `when_[action]_then_[outcome]`

Example:

```csharp
public class GivenUpdateService
{
    [Fact]
    public void when_checking_for_updates_then_fetches_github_releases() { }
}
```

## Code Analysis & Style

- **Nullable reference types:** Always on. Fix warnings — they catch real null issues.
- **Warnings as errors:** CI enforces. No suppress without reason; add comment explaining why.
- **Implicit usings:** System, System.Linq, etc. available without explicit using statements.
- **Target framework:** Most projects net10.0; main app net10.0-windows for platform APIs.

## CI/CD

**Build & Test** (`.github/workflows/build-and-test.yml`):

- Runs on push/PR to main, or manual dispatch
- Builds, tests with coverage, generates HTML report, uploads as artifact

**Release** (`.github/workflows/release.yml`):

- Triggers on semver tag push
- Serialized: both OS jobs attach to same GitHub Release (no race conditions)
- Uses `vpk` CLI to pack and upload

## Common Development Patterns

**DI & Configuration:**

```csharp
// Services configured in ServiceCollectionExtensions
services.AddScoped<IUpdateService, UpdateService>();
services.Configure<ScrapeConfiguration>(config.GetSection("scrapeConfiguration"));
```

**Reactive UI Updates:**

```csharp
// ReactiveUI binding in ViewModel
this.WhenAnyValue(vm => vm.Property)
    .Subscribe(value => HandleUpdate(value))
    .DisposeWith(disposables);
```

**Database Access:**

```csharp
// EF Core context via DI
using var db = serviceProvider.GetRequiredService<AppDbContext>();
await db.YourEntity.ToListAsync();
```

**Logging:**

```csharp
using var logger = new LoggerFactory().CreateLogger<MyClass>();
logger.LogInformation("Message with {Property}", value);
```
