# AStar.Dev.Wallpaper.Scraper — Project-Wide Style Rollout Plan

`Services/PlaywrightService.cs` (post-refactor, see `docs/playwright-service-refactor-options.md`, Option E) is the
reference style for this project going forward:

- Public orchestrating methods read as a `Try.RunAsync(...).TapAsync(...).MapAsync(...)` pipeline — no inline
  multi-statement lambdas, no imperative `try`/`catch` for expected failure paths.
- Every pipeline step is a named, 1-3 line private method (`GetOrCreateXAsync`, `HideWebdriverAsync`, etc.) —
  the chain is a table of contents.
- Resource lifetimes (locks, disposables) are scoped via a small `IDisposable` helper (`ConfigureLockScope`)
  instead of a mutable bool threaded through the chain.
- `[ExcludeFromCodeCoverage]` only where a class is genuinely untestable; everything else is designed to be
  tested via DI substitution (`IFileSystem`, `IOptions<T>`, etc.).
- `<inheritdoc />` on interface implementations; full XML docs on public members otherwise; **zero comments**
  inside method bodies.

This plan rewrites every remaining `.cs` file in `AStar.Dev.Wallpaper.Scraper` to match, split into 15 phases
of 2-6 files. Files that already match the style, or carry no logic (interfaces, plain DTOs), are marked
**review-only** — they get a pass for XML-doc consistency and naming, not a structural rewrite.

Decisions already confirmed with the repo owner:

1. **Scope** — every `.cs` file in the project is in-scope, including DI wiring, XAML codebehind, interfaces,
   and record DTOs (not just classes with heavy logic).
2. **Boundary error handling** — `UpdateService` and `DatabaseMigrator`'s swallow-all `try`/`catch` blocks
   convert to `Try.RunAsync`/`Exceptional` too, for full consistency, despite being `[ExcludeFromCodeCoverage]`.
3. **GH issue granularity** — one GitHub issue per phase below (one branch/PR per phase).

`PlaywrightService.cs` and `IPlaywrightService.cs`'s implementation are excluded from the phases (already
done) — `IPlaywrightService.cs` itself still gets a review pass in Phase 4 alongside the rest of the
composition root, since it hasn't been touched directly.

---

## Phase 1 — Startup & background boundary error handling

**Files:** `Startup/DatabaseMigrator.cs`, `Services/UpdateService.cs`, `Startup/ApplicationConfigurationFactory.cs`,
`Startup/ApplicationOptionsRegistrar.cs`, `Startup/SerilogConfigurator.cs`

**Why first:** Lowest coupling to UI, establishes the "swallow-all boundary as `Try.RunAsync` + logged error tap"
pattern that Phase 15 (MainWindowViewModel) and Phase 14 (EntityEditorViewModel) will reuse.

- `DatabaseMigrator.MigrateAsync`: replace `try { ... } catch (Exception ex) { LogMessage.Error(...) }` with
  `Try.RunAsync(...).TapErrorAsync(exception => LogMessage.Error(logger, "Database migration failed", exception))`
  (or equivalent `Exceptional` error tap available in `AStar.Dev.FunctionalParadigm` — confirm exact API name).
- `UpdateService.CheckForUpdatesAsync`: same pattern for the outer catch; extract each stage
  (`CreateManager`, `CheckForUpdateAsync`, `DownloadUpdateAsync`, `PromptAndApplyAsync`) as named private methods,
  chained via `Try.RunAsync(...).MapAsync(...)...`. The inner `Log()` try/catch around file I/O is genuinely a
  "never let logging break anything" guard — keep as-is or convert to a one-line `Try.Run(...).Match(...)`
  discard if that reads cleaner; call out in the PR if kept imperative.
- `ApplicationConfigurationFactory`, `ApplicationOptionsRegistrar`, `SerilogConfigurator`: **review-only** —
  already single-expression, `[ExcludeFromCodeCoverage]`, no imperative branching. Confirm XML doc consistency.

---

## Phase 2 — Application directories & metadata

**Files:** `Configuration/ApplicationMetadata.cs`, `Configuration/ApplicationDirectories.cs`,
`Configuration/IApplicationDirectories.cs`

- `ApplicationDirectories.CreateIfRequired`: currently four sequential `fileSystem.Directory.CreateDirectory`
  calls — fine as imperative (no failure path worth modelling), but tidy up the inconsistent XML doc comment
  spacing (`///   Gets...` vs `///     Gets...`) to match the rest of the codebase.
- `ApplicationMetadata`: **review-only**, const holder — just fix doc-comment spacing.
- `IApplicationDirectories`: **review-only**.

---

## Phase 3 — Options POCOs

**Files:** `Configuration/ScrapeConfiguration.cs`, `Configuration/SearchConfiguration.cs`,
`Configuration/ConnectionStrings.cs`, `Configuration/UpdateConfiguration.cs`, `Configuration/SyncSettings.cs`

- These are mutable `class`es with `{ get; set; }`, which conflicts with the style rule "`record` for immutable
  models/DTOs; `class` for entities with mutable state" — but they're bound via `IOptions<T>`/`services.Configure<T>`,
  which needs settable (or `init`) properties.
- **Open question to resolve at phase start:** can these become `record`s with `init` properties and still bind
  correctly via `IConfiguration`'s binder? If yes, convert for consistency with the "records for DTOs" rule
  (`SyncSettings` already is a `record` with `required`/`init` — use it as the template). If binding breaks,
  document why these five are the deliberate exception to the record rule.
- Add missing XML docs on `ScrapeConfiguration.ApplicationName`/`ConnectionStrings`, `ConnectionStrings.Sqlite`,
  `UpdateConfiguration.RepositoryUrl` (currently undocumented, unlike their siblings).

---

## Phase 4 — Composition root

**Files:** `Program.cs`, `App.axaml.cs`, `ServiceCollectionExtensions.cs`, `Services/IPlaywrightService.cs`

- `Program.cs`: has three `//` comments (Avalonia template boilerplate) — against the "never add comments"
  rule. Two are template guidance ("don't remove", "don't use Avalonia APIs before AppMain") worth keeping
  verbatim as they're load-bearing warnings the Avalonia project template itself relies on; flag this as an
  explicit, documented exception rather than removing them silently.
- `App.axaml.cs`: `OnFrameworkInitializationCompleted` blocks on `DatabaseMigrator.MigrateAsync(...).GetAwaiter().GetResult()`
  — not FP-chain style, but this is a synchronous entry point (Avalonia's `OnFrameworkInitializationCompleted`
  isn't `async`), so blocking is closer to unavoidable here. Consider extracting the whole method body into
  named steps (`BuildServices`, `MigrateDatabase`, `ShowMainWindow`) for readability, matching Phase 1's newly
  Exceptional-based `DatabaseMigrator`. The `Dispose(bool)`/`Dispose()` pair has a "do not change this code"
  comment from the Avalonia/analyzer template — keep verbatim, same reasoning as `Program.cs`.
- `ServiceCollectionExtensions.AddApplicationServices`: pure DI wiring, no branching — **review-only**, though at
  57 lines and growing it's a candidate to split into per-module `AddScrapingServices`/`AddEntityEditorServices`
  extension methods if it keeps growing; not required now.
- `IPlaywrightService.cs`: **review-only** (already `Exceptional`-based, matches target style).

---

## Phase 5 — Scraping: search context & category discovery

**Files:** `Scraping/IScrapeContextReader.cs`, `Scraping/ScrapeContextReader.cs`, `Scraping/ScrapeContext.cs`,
`Scraping/ScrapeCategory.cs`, `Scraping/DirectoryLayout.cs`

- `ScrapeContextReader.ReadAsync`: five sequential EF Core queries with no failure handling — matches "internal
  operations, exceptions OK" rule; **review-only** for structure, but confirm it wouldn't read better as a
  handful of named private query methods (`ReadSearchConfigurationAsync`, `ReadCategoriesAsync`, ...) the way
  `PlaywrightService` extracts its steps, given the constructor call at the end assembles five results.
- `ScrapeContext`, `ScrapeCategory`, `DirectoryLayout`: plain records — **review-only**, confirm they don't need
  a `...Factory` per the "Records" style rule (they're read-only projections of DB rows/config, not
  user/API-constructed values, so factories may not add value — flag for discussion in the PR).
- `IScrapeContextReader`: **review-only**.

---

## Phase 6 — Scraping: search results page reading

**Files:** `Scraping/IWallpaperCountReader.cs`, `Scraping/WallpaperCountReader.cs`,
`Scraping/IWallpaperHrefCollector.cs`, `Scraping/WallpaperHrefCollector.cs`

- `WallpaperHrefCollector.CollectAsync`: builds a `List<string>` via manual `foreach` + conditional `Add` —
  rewrite as a LINQ pipeline (`previews.SelectAsync(...).Where(...)` or similar via
  `AStar.Dev.FunctionalParadigm`'s async LINQ helpers, matching how `SearchCategoryScrapeAction` composes
  collections elsewhere in the codebase) instead of an imperative loop.
- `WallpaperCountReader.ReadAsync`: already a tight LINQ-ish expression — **review-only**.
- Both interfaces: **review-only**.

---

## Phase 7 — Scraping: wallpaper detail page & tag curation

**Files:** `Scraping/ITagReader.cs`, `Scraping/TagReader.cs`, `Scraping/TagData.cs`, `Scraping/TagCuration.cs`,
`Scraping/TagCurator.cs`

- `TagCurator.Curate`: uses a C-style `for (int i = 0; i < tags.Count; i++)` loop with `continue` — rewrite as
  a LINQ pipeline (`tags.Select(NormalizeModelSuffix).Where(...)`) to match the functional style used
  everywhere else; the two-list side effect (`kept` + `messages`) makes this the trickiest rewrite in this
  phase — likely needs an intermediate record per tag (`kept: bool`, `message: string`) folded at the end into
  `TagCuration`.
- `TagReader.ReadAsync`: already clean (`Task.WhenAll` + `Select`) — **review-only**.
- `TagData`, `TagCuration`: **review-only** records.

---

## Phase 8 — Scraping: image location & download

**Files:** `Scraping/IWallpaperImageLocator.cs`, `Scraping/WallpaperImageLocator.cs`,
`Scraping/IWallpaperImageDownloader.cs`, `Scraping/WallpaperImageDownloader.cs`

- `WallpaperImageDownloader.DownloadAsync` uses `response!.BodyAsync()` — a null-forgiving operator masking a
  real possible-null case (`GotoAsync` can return `null` per Playwright's contract). This is the one concrete
  correctness gap in this phase: wrap in `Try.RunAsync`/return `Exceptional<byte[]>` (or at minimum guard and
  throw a named exception) instead of risking a `NullReferenceException`.
- `WallpaperImageLocator.LocateAsync`: already uses `Option<string>` correctly — **review-only**, good template
  for the downloader fix above.

---

## Phase 9 — Scraping: thumbnail pipeline

**Files:** `Scraping/IWallpaperThumbnailGenerator.cs`, `Scraping/WallpaperThumbnailGenerator.cs`,
`Scraping/ThumbnailPublishingWallpaperImageDownloader.cs`, `Scraping/IWallpaperThumbnailPublisher.cs`,
`Scraping/IWallpaperThumbnailFeed.cs`, `Scraping/WallpaperThumbnailBroadcaster.cs`

- All five implementation classes are already small, single-purpose, and side-effect-free beyond their one
  job — **review-only** across this phase. `WallpaperThumbnailGenerator`'s SkiaSharp `using` chain is a good
  existing example of resource-scoping discipline; no change expected.

---

## Phase 10 — Scraping: image dimensions & file persistence

**Files:** `Scraping/IImageDimensionsReader.cs`, `Scraping/SkiaImageDimensionsReader.cs`,
`Scraping/ImageDimensions.cs`, `Scraping/IWallpaperFileStore.cs`, `Scraping/WallpaperFileStore.cs`,
`Scraping/SavedWallpaperFile.cs`

- All **review-only** — both implementations are already minimal, correctly use `IFileSystem`/SkiaSharp
  abstractions, follow the blank-line-before-return rule. No structural change expected; confirm doc
  consistency only.

---

## Phase 11 — Scraping: directory resolution & DB persistence

**Files:** `Scraping/WallpaperDirectoryResolver.cs`, `Scraping/IWallpaperCategoryRegistrar.cs`,
`Scraping/WallpaperCategoryRegistrar.cs`, `Scraping/IWallpaperFileClassificationRepository.cs`,
`Scraping/WallpaperFileClassificationRepository.cs`

- `WallpaperCategoryRegistrar.EnsureCategoriesExistAsync`: `foreach` loop with an `await`-ed `AnyAsync` guard
  and conditional `Add` inside — this is genuinely sequential (each iteration's DB check depends on nothing
  from prior iterations, but EF's `DbContext` isn't thread-safe, so it can't trivially become
  `ForEachAsync`/parallel). Rewrite as a filtered LINQ projection first (`tags.Where(HasCategoryAndTag)`), then
  a plain `foreach` over the pre-filtered list for the awaited body — reduces nesting even though the loop
  itself stays.
- `WallpaperFileClassificationRepository.RecordAsync`: same shape — `foreach` with an awaited lookup
  (`FirstAsync`) per tag. Same treatment: pre-filter/project with LINQ, keep the minimal `foreach` for the
  awaited DB write since `ForEachAsync` used in `SearchCategoryScrapeAction` is for read-only iteration, not
  something threading writes through a single shared `DbContext` safely.
- `WallpaperDirectoryResolver.Resolve`: already a clean LINQ pipeline — **review-only**.
- Both interfaces: **review-only**.

---

## Phase 12 — Scraping: orchestration

**Files:** `Scraping/IScrapeAction.cs`, `Scraping/SearchCategoryScrapeAction.cs`

- `SearchCategoryScrapeAction` already uses `Try.RunAsync`/`Exceptional`/`MatchAsync` and named private methods
  per pipeline stage — this is effectively already at the target style (it's the second-best example in the
  codebase after `PlaywrightService`). **Review-only**: confirm parameter counts on `VisitCategoryPageAsync`
  (7 params) and `DownloadWallpaperAsync` (8 params) aren't worth collapsing into a small context record now
  that Phase 5 will have already looked at `ScrapeContext`; not required, flag as an optional follow-up only.
- `IScrapeAction`: **review-only**.

---

## Phase 13 — Entity editor: descriptor & factory

**Files:** `Configuration/EntityEditor/EntityEditorDescriptor.cs`, `Configuration/EntityEditor/IEntityEditorFactory.cs`,
`Configuration/EntityEditor/EntityEditorFactory.cs`

- `EntityEditorDescriptor<TEntity>`: a `record` with a `Func<TEntity> CreateNew` constructor parameter — per
  the "Records" style rule, records should have no methods and be paired with a `...Factory`, but this one is
  intentionally a data-holder passed *into* `EntityEditorFactory`, not something users construct directly with
  invalid-input concerns. **Review-only**, flag the tension for a decision rather than force-fitting a
  `EntityEditorDescriptorFactory` that adds no validation value.
- `EntityEditorFactory`: eight near-identical `Create*Editor` methods constructing a `new EntityEditorViewModel<T>`
  each — **review-only** for correctness (no imperative branching to convert), but worth flagging as
  repetitive; not touching unless the phase reviewer wants a table-driven rewrite (risk of over-engineering a
  working, simple factory).
- `IEntityEditorFactory`: **review-only**.

---

## Phase 14 — Entity editor: view model & window

**Files:** `Configuration/EntityEditor/EntityEditorViewModelBase.cs`,
`Configuration/EntityEditor/EntityEditorViewModel.cs`, `Configuration/EntityEditor/EntityEditorWindow.axaml.cs`

**This is the single biggest behavioral rewrite in the plan.** `EntityEditorViewModel<TEntity>.SaveAsync`,
`ExportAsync`, and `ImportAsync` each wrap their body in `try { ... } catch (Exception exception) { StatusMessage = ... }`
— exactly the pattern Phase 1 establishes a house alternative for:

- Convert all three to `Try.RunAsync(...).MatchAsync(onSuccess, onError)` (or `.Match` for the sync `ExportAsync`/
  `ImportAsync` bodies), assigning `StatusMessage` in each branch — mirrors `MainWindowViewModel`'s existing use
  of `Exceptional`/`MatchAsync` for the scrape command, so both view models end up consistent with each other.
- `ImportAsync`'s "file not found" early return is a validation case, not an exceptional one — keep as an
  early guard clause *before* entering the `Try.RunAsync` pipeline (same shape as `PlaywrightService`'s
  `if (page is not null) return page;` early return before the lock/chain).
- `EntityEditorViewModelBase`: **review-only** — already a clean abstract contract.
- `EntityEditorWindow.axaml.cs`: **review-only** — `OnAutoGeneratingColumn`'s guard-clause style already
  matches house rules.

---

## Phase 15 — Home: main window & scrape UI

**Files:** `ViewModels/ViewModelBase.cs`, `Home/MainWindowViewModel.cs`, `Home/MainWindow.axaml.cs`,
`ConfirmDialog.axaml.cs`

**Second-biggest rewrite.** `MainWindowViewModel.CreateScrapeCommand`'s inner `ReactiveCommand.CreateFromTask`
body is a ~50-line lambda mixing confirmation, cancellation, Playwright configuration, action execution, and
status reporting behind one `try`/`catch (OperationCanceledException)`/`finally`:

- Extract the lambda body into named private methods per stage (`ConfirmAndRunAsync`, `RunActionAsync`,
  `ReportError`), following `PlaywrightService`'s "chain reads top-to-bottom as named stages" pattern — even
  though `ReactiveCommand.CreateFromTask` isn't itself a monadic chain, its body can still be decomposed the
  same way.
- `page.MatchAsync(...)` and `result.Match(...)` are already used correctly for the `Exceptional` values —
  keep, just hoist the nested nested lambdas into named methods.
- `OperationCanceledException` catch stays (cancellation is genuinely exceptional control flow in .NET, not
  something `Exceptional`/`Try` models) — but move it to wrap only the narrowest scope that can throw it,
  rather than the whole command body, once the body is split into named steps.
- `ViewModelBase`: **review-only** (7-line empty base class).
- `MainWindow.axaml.cs`, `ConfirmDialog.axaml.cs`: **review-only** — thin codebehind, already guard-clause style
  where present; each has one harmless template comment (parameterless-constructor-for-XAML-previewer) — same
  "keep, it's load-bearing Avalonia guidance" call as `Program.cs` in Phase 4.

---

## Summary table

| Phase | Files | Rewrite weight |
|---|---|---|
| 1 | DatabaseMigrator, UpdateService, ApplicationConfigurationFactory, ApplicationOptionsRegistrar, SerilogConfigurator | Medium |
| 2 | ApplicationMetadata, ApplicationDirectories, IApplicationDirectories | Low |
| 3 | ScrapeConfiguration, SearchConfiguration, ConnectionStrings, UpdateConfiguration, SyncSettings | Low-Medium (open question) |
| 4 | Program, App.axaml, ServiceCollectionExtensions, IPlaywrightService | Low |
| 5 | IScrapeContextReader, ScrapeContextReader, ScrapeContext, ScrapeCategory, DirectoryLayout | Low |
| 6 | IWallpaperCountReader, WallpaperCountReader, IWallpaperHrefCollector, WallpaperHrefCollector | Low-Medium |
| 7 | ITagReader, TagReader, TagData, TagCuration, TagCurator | Medium |
| 8 | IWallpaperImageLocator, WallpaperImageLocator, IWallpaperImageDownloader, WallpaperImageDownloader | Low-Medium (correctness fix) |
| 9 | Thumbnail generator/publisher/feed/broadcaster/decorator (6 files) | None expected |
| 10 | Image dimensions + file store (6 files) | None expected |
| 11 | WallpaperDirectoryResolver, WallpaperCategoryRegistrar, WallpaperFileClassificationRepository (+interfaces) | Low-Medium |
| 12 | IScrapeAction, SearchCategoryScrapeAction | None expected (already exemplar) |
| 13 | EntityEditorDescriptor, IEntityEditorFactory, EntityEditorFactory | Low |
| 14 | EntityEditorViewModelBase, EntityEditorViewModel, EntityEditorWindow.axaml.cs | **High** |
| 15 | ViewModelBase, MainWindowViewModel, MainWindow.axaml.cs, ConfirmDialog.axaml.cs | **High** |

64 files across 15 phases (`PlaywrightService.cs` excluded — already done).

## Open questions to resolve before/at the relevant phase

- **Phase 3:** Do `IOptions<T>`-bound POCOs bind correctly as `record`s with `init` properties in this
  codebase's .NET/config-binder version? Decides whether these five become records or stay documented
  exceptions to the immutability rule.
- **Phase 5 / 13:** Should read-only projection records (`ScrapeContext`, `ScrapeCategory`, `DirectoryLayout`,
  `EntityEditorDescriptor<TEntity>`) get a `...Factory` per the house "Records" rule, or is that rule scoped to
  records with real validation concerns (user/API input)? Affects whether Phases 5 and 13 add factory classes
  or just leave a documented exception.
- **Phase 4:** Confirm the two Avalonia-template comments (`Program.cs` STAThread/AppMain warning,
  `App.axaml.cs` Dispose "do not change" comment) are accepted as permanent exceptions to the no-comments rule,
  since removing them risks contradicting Avalonia's own project template guidance.
