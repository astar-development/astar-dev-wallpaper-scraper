# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AStar Dev Wallpaper Scraper is a .NET 10 desktop application built with Avalonia + ReactiveUI for the UI, Playwright for web scraping, and SQLite for local data storage. The app includes auto-update functionality via Velopack, reading release packages from GitHub.

## Build & Test

All builds target `net10.0`. Use these commands in the project root:

**Build:**
```bash
dotnet build                                          # Debug build
dotnet build --configuration Release                 # Release build
```

**Test:**
```bash
dotnet test                                           # Run all tests
dotnet test --project AStar.Dev.Utilities.Tests.Unit # Run single project
dotnet test --filter "GivenClassName"               # Run by test class
```

**Coverage:**
```bash
bash code-coverage.sh  # Runs tests with XPlat Cobertura coverage, generates HTML report in CoverageReport/
```

## Codebase Architecture

### Project Structure

```
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

- **Avalonia + ReactiveUI:** Desktop UI uses reactive MVVM. ViewModels inherit from ReactiveObject. UI subscribes to observable streams for state changes.
- **Dependency Injection:** Microsoft.Extensions.DI via configuration-driven bootstrapping (appsettings.json). App.xaml.cs and Program.cs wire up services.
- **Logging:** Serilog with console and Seq sinks. Configured via appsettings.json `Logging` section.
- **Database:** EF Core 10 + SQLite. Connection string in appsettings under `scrapeConfiguration.connectionStrings.sqlite`.
- **Updates:** Velopack integration. App calls `VelopackApp.Build().Run()` before Avalonia startup (Program.cs). UpdateService reads GitHub releases.
- **Tests:** xunit.v3 with MTP v2 runner (pinned in global.json). All test projects compile but test methods only run in projects suffixed with `.Tests` or `.Tests.Unit`.

### Key Configuration

**Directory.Build.props:**
- All projects: `net10.0`, nullable reference types enabled, implicit usings, latest C# language version
- Tests: Snake_case method names (when_[action]_then_[outcome]) + PascalCase class names (Given[Subject])
- All warnings → errors
- Code coverage collector injected into test projects automatically

**Directory.Packages.props:**
- Central Package Management (CPM). Every package version declared once; projects reference without versions.
- If you add a NuGet package, add `<PackageVersion>` entry here, then reference without version in .csproj.

**appsettings.json:**
- Logging.LogLevel, Logging.Console, Logging.Serilog configured via this file (null = use defaults)
- updateConfiguration.repositoryUrl points to GitHub (Velopack reads releases here)
- scrapeConfiguration holds app-specific settings (database connection, app name)

## Release & Deployment

Push a semver tag (`git tag v0.1.1 && git push origin v0.1.1`) to trigger the release workflow:

1. Workflow derives version from tag (strips `v` prefix)
2. Publishes self-contained for linux-x64 and win-x64
3. Packs with `vpk` (Velopack CLI tool)
4. Downloads previous releases (enables delta updates)
5. Uploads to GitHub Release with `--merge` (combines both OS builds into one release)

The app's UpdateService polls this release feed for in-app update checks.

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

- **Nullable reference types:** Always enabled. Fix warnings — they catch real null issues.
- **Warnings as errors:** CI enforces this. Don't suppress unless justified; add comments explaining why.
- **Implicit usings:** System, System.Linq, etc. are available without explicit using statements.
- **Target framework:** Most projects use net10.0; main app uses net10.0-windows for platform APIs.

## CI/CD

**Build & Test** (`.github/workflows/build-and-test.yml`):
- Runs on push/PR to main, or manual dispatch
- Builds, tests with coverage, generates HTML report, uploads as artifact

**Release** (`.github/workflows/release.yml`):
- Triggers on semver tag push
- Serialized: both OS jobs attach to the same GitHub Release (no race conditions)
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
