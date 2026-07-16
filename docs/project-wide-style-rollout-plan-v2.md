# AStar.Dev.Wallpaper.Scraper — Project-Wide Style Rollout Plan (v2)

Supersedes `docs/project-wide-style-rollout-plan.md`. Changes from v1 are called out inline as
**[v2]** notes. v1 is left in place for history; this file is the one to follow.

`Services/PlaywrightService.cs` (post-refactor, see `docs/playwright-service-refactor-options.md`)
is the reference style for this project going forward:

- Public orchestrating methods read as a `Try.RunAsync(...).TapAsync(...).MapAsync(...)` pipeline — no inline
  multi-statement lambdas, no imperative `try`/`catch` for expected failure paths.
- Every pipeline step is a named, 1-3 line private method (`GetOrCreateXAsync`, `HideWebdriverAsync`, etc.) —
  the chain is a table of contents.
- Resource lifetimes (locks, disposables) are scoped via a small `IDisposable` helper (`ConfigureLockScope`)
  instead of a mutable bool threaded through the chain.
- `[ExcludeFromCodeCoverage]` only where a class is genuinely untestable; everything else is designed to be
  tested via DI substitution (`IFileSystem`, `IOptions<T>`, etc.).
- `<inheritdoc />` on interface implementations; full XML docs on public members otherwise; **zero comments**
  inside method bodies, except a documented, narrow set of exceptions (see Phase 4).

**[v2] Correction:** v1 described this as "Option E" from the refactor-options doc, verbatim. It
isn't, quite. The actual implementation is Option E *adapted*, not Option E as literally coded
there:

- The refactor doc's Option E extracts the lock-scope logic into a `SemaphoreSlimExtensions.WaitAndReleaseAsync`
  extension method (using C#'s newer `extension(...)` member syntax) plus a standalone `Releaser` class.
  The shipped code instead uses an instance method, `AcquireConfigureLockAsync`, and a private nested
  `ConfigureLockScope` class — no extension method, no separate static class. Simpler, no dependency on the
  newer extension-member syntax. This is the better choice; the doc is what's wrong, not the code.
- Option E's sample body opens with `if (page is not null) return page;` — an implicit `Exceptional<IPage>`
  conversion. That doesn't actually work: the C# spec ignores user-defined conversion operators when the
  source type is an interface, so an implicit conversion from `IPage` fails to compile. This was hit and
  fixed during the 2026-07-13 review (see memory `playwrightservice-review-findings`) — the shipped code uses
  the explicit `Exceptional.Success(page)` factory call instead. Any future pipeline step returning an
  interface-typed `Exceptional<T>` needs the same explicit-factory treatment; don't copy Option E's sample
  literally.

**Action:** `docs/playwright-service-refactor-options.md` gets a short correction note added at the top of
Option E pointing at the real implementation and these two deltas. No code changes — the shipped
`PlaywrightService.cs` pattern is correct as-is; only the doc was stale.

This plan rewrites every remaining `.cs` file in `AStar.Dev.Wallpaper.Scraper` to match, split into a
Phase 0 cleanup pass plus 15 phases of 2-6 files. Files that already match the style, or carry no logic
(interfaces, plain DTOs), are marked **review-only** — they get a pass for XML-doc consistency and naming,
not a structural rewrite.

Decisions already confirmed with the repo owner:

1. **Scope** — every `.cs` file in the project is in-scope, including DI wiring, XAML codebehind, interfaces,
   and record DTOs (not just classes with heavy logic).
2. **Boundary error handling** — `UpdateService` and `DatabaseMigrator`'s swallow-all `try`/`catch` blocks
   convert to `Try.RunAsync`/`Exceptional` too, for full consistency, despite being `[ExcludeFromCodeCoverage]`.
3. **GH issue granularity** — one GitHub issue per phase below (one branch/PR per phase).
4. **[v2] Config POCOs (Phase 3)** — the five `IOptions<T>`-bound classes (`ScrapeConfiguration`,
   `SearchConfiguration`, `ConnectionStrings`, `UpdateConfiguration`, `SyncSettings`) are a **permanent,
   documented exception** to the "record + Factory" rule. The `IConfiguration` binder needs a parameterless
   constructor and writable (`set`/`init`) properties, and writes them directly — it cannot be routed through
   a validating `Factory.Create` method. Applies to all five, not just the ones v1 flagged as an open
   question. No further investigation needed; the phase task is just to confirm each of the five stays a
   mutable `class` with a one-line doc comment stating why.
5. **[v2] Read-only projection records (Phase 5, 13)** — `ScrapeContext`, `ScrapeCategory`, `DirectoryLayout`,
   `EntityEditorDescriptor<TEntity>` are **exempt** from the "every record needs a `Factory`" rule. They carry
   no user/API-facing validation concern — they're internal projections of DB rows/config or values passed
   between internal collaborators. A `Factory` with nothing to validate is ceremony, not protection. Document
   this exemption once (in this file) rather than repeating the justification in each phase.
6. **[v2] `PlaywrightService.cs` reference-file polish** — see Phase 0 below. Small, low-risk fixes to the
   reference file itself before other phases start copying its pattern.

`PlaywrightService.cs` and `IPlaywrightService.cs`'s implementation are excluded from the numbered phases
(structurally done) — `IPlaywrightService.cs` still gets a review pass in Phase 4 alongside the rest of the
composition root, since its XML docs haven't been touched directly. **[v2]**: Phase 0 below covers the
handful of concrete fixes identified in `PlaywrightService.cs`/`IPlaywrightService.cs` that predate this
plan; Phase 4's review of `IPlaywrightService.cs` is otherwise unchanged.

---

## Phase 0 — Reference file polish **[v2, new]**

**Files:** `Services/PlaywrightService.cs`, `Services/IPlaywrightService.cs`

Small, targeted fixes to the file every other phase is copying. Do this first so nothing downstream
inherits these rough edges.

- `PlaywrightService.DisposeAsync`: `await context.CloseAsync();` is missing `.ConfigureAwait(false)` —
  every other `await` in the class has it. Add it for consistency.
- `PlaywrightService.DisposeAsync`: no exception-safety — if `context.CloseAsync()` throws, `playwright`
  and `configureLock` are never disposed, and the exception propagates past `GC.SuppressFinalize(this)`
  (never called). Wrap so `playwright?.Dispose()` and `configureLock.Dispose()` run even if `CloseAsync`
  throws (e.g. dispose each independently, aggregating exceptions, or a `try`/`finally`).
- `IPlaywrightService.ConfigurePlaywrightAsync`'s XML doc has a `<returns>` but no `<param name="cancellationToken">`
  — add one. Matches the "full XML docs on public members" rule this whole plan is enforcing elsewhere.
- No structural changes — the `Try.RunAsync`/named-step pipeline shape is correct and is the template;
  this phase only touches `DisposeAsync` and the interface doc comment.

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
- **[v2]** The doc-comment spacing inconsistency isn't unique to this phase — `IPlaywrightService.cs`
  (Phase 0/4) has the same `///     ` 5-space pattern. Treat "normalize `///` indentation to match the
  prevailing convention" as a checklist item in **every** phase's review pass, not just Phase 2.

---

## Phase 3 — Options POCOs

**Files:** `Configuration/ScrapeConfiguration.cs`, `Configuration/SearchConfiguration.cs`,
`Configuration/ConnectionStrings.cs`, `Configuration/UpdateConfiguration.cs`, `Configuration/SyncSettings.cs`

- **[v2, was an open question in v1 — now resolved]**: these five stay mutable `class`es with `{ get; set; }`
  properties, as a permanent, documented exception to the "`record` for immutable models/DTOs" and "every
  record needs a `Factory`, validation in factory, never use the public constructor directly" rules. The
  `IConfiguration` binder requires a parameterless constructor and settable/init members that it writes to
  directly — routing that through a validating factory isn't possible, and `record`-with-`init` doesn't
  change that constraint (the binder still needs to reach the properties directly, bypassing any factory).
  `SyncSettings` currently being a `record` with `required`/`init` is the odd one out, not the template to
  follow — confirm whether it's actually bound via `IConfiguration` the same way as the other four; if so,
  convert it to match the other four (mutable `class`) for consistency, rather than converting the other
  four to match it.
- Add a one-line XML doc comment on each of the five classes stating this is a deliberate exception (e.g.
  `/// Mutable to support direct <see cref="IConfiguration"/> binding; not a candidate for the Records rule.`).
- Add missing XML docs on `ScrapeConfiguration.ApplicationName`/`ConnectionStrings`, `ConnectionStrings.Sqlite`,
  `UpdateConfiguration.RepositoryUrl` (currently undocumented, unlike their siblings).

---

## Phase 4 — Composition root

**Files:** `Program.cs`, `App.axaml.cs`, `ServiceCollectionExtensions.cs`, `Services/IPlaywrightService.cs`

- `Program.cs`: has three `//` comment blocks. **[v2 correction]**: v1 treated all three as Avalonia-template
  boilerplate needing a blanket "permanent exception" carve-out. Only two of them actually are:
  - Lines 11-13 (`Initialization code. Don't use any Avalonia, third-party APIs...`) — Avalonia project
    template text. Keep verbatim; flag as an explicit, documented exception, same reasoning as before.
  - Line 24 (`Avalonia configuration, don't remove; also used by visual designer.`) — also Avalonia template
    text. Same treatment.
  - Lines 17-18 (`Velopack hooks install/update/uninstall events and may exit the process; it must run
    before anything else touches the app state.`) — this is **not** template boilerplate, it's a
    project-authored comment explaining a genuinely non-obvious ordering constraint (why Velopack's hook
    must run before anything else). That's exactly the kind of comment CLAUDE.md's own rule allows ("only
    add one when the WHY is non-obvious... a hidden constraint"). It doesn't need an Avalonia-template
    exception — it already complies with the house comment rule as written. No change needed; just don't
    lump it in with the other two when writing the PR justification.
- `App.axaml.cs`: `OnFrameworkInitializationCompleted` blocks on `DatabaseMigrator.MigrateAsync(...).GetAwaiter().GetResult()`
  — not FP-chain style, but this is a synchronous entry point (Avalonia's `OnFrameworkInitializationCompleted`
  isn't `async`), so blocking is closer to unavoidable here. Consider extracting the whole method body into
  named steps (`BuildServices`, `MigrateDatabase`, `ShowMainWindow`) for readability, matching Phase 1's newly
  Exceptional-based `DatabaseMigrator`. The `Dispose(bool)`/`Dispose()` pair has a "do not change this code"
  comment from the Avalonia/analyzer template — keep verbatim, same reasoning as `Program.cs`'s two template
  comments.
- `ServiceCollectionExtensions.AddApplicationServices`: pure DI wiring, no branching — **review-only**, though at
  57 lines and growing it's a candidate to split into per-module `AddScrapingServices`/`AddEntityEditorServices`
  extension methods if it keeps growing; not required now.
- `IPlaywrightService.cs`: **[v2]** the missing `<param name="cancellationToken">` doc is fixed in Phase 0;
  otherwise **review-only** here (already `Exceptional`-based, matches target style).

---

## Phase 5 — Scraping: search context & category discovery

**Files:** `Scraping/IScrapeContextReader.cs`, `Scraping/ScrapeContextReader.cs`, `Scraping/ScrapeContext.cs`,
`Scraping/ScrapeCategory.cs`, `Scraping/DirectoryLayout.cs`

- `ScrapeContextReader.ReadAsync`: five sequential EF Core queries with no failure handling — matches "internal
  operations, exceptions OK" rule; **review-only** for structure, but confirm it wouldn't read better as a
  handful of named private query methods (`ReadSearchConfigurationAsync`, `ReadCategoriesAsync`, ...) the way
  `PlaywrightService` extracts its steps, given the constructor call at the end assembles five results.
- `ScrapeContext`, `ScrapeCategory`, `DirectoryLayout`: plain records — **[v2, resolved]**: no `Factory`
  required. These are read-only projections of DB rows/config with no validation concern, exempted per the
  decision at the top of this plan. **Review-only** for doc/naming consistency only.
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
- `TagData`, `TagCuration`: **review-only** records. **[v2]**: confirm at phase start whether either carries
  any validation concern; if genuinely none, they fall under the same Factory exemption as Phase 5's records
  — if either is constructed from raw scraped/user-facing text (unlike Phase 5's DB/config projections), the
  exemption may not apply and it should get a `Factory` instead. Don't assume — check the construction sites.

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
- **[v2]** `ImageDimensions` and `SavedWallpaperFile` are records — check at phase start whether they need a
  `Factory` under the same test applied in Phases 5/7: pure internal projection with no validation concern,
  exempt; constructed from external/unvalidated input, needs one. Don't assume exempt by default here.

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
  - **[v2]** This is the repository that persists scraped tags. Per memory (`scraper-category-persistence`):
    `SearchCategories` is user-managed and must never be written by scraper code; scraped tags belong under
    `FileClassificationCategories` with an "Unclassified" bucket. Confirm this phase's rewrite doesn't touch
    `SearchCategories` — the LINQ-pipeline rewrite should be scoped to `FileClassificationCategories` writes
    only. Call this out explicitly in the phase's PR description since it's an easy invariant to accidentally
    break while restructuring the loop.
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

- `EntityEditorDescriptor<TEntity>`: a `record` with a `Func<TEntity> CreateNew` constructor parameter.
  **[v2, resolved]**: exempt from the "every record needs a `Factory`" rule, same reasoning as Phase 5 — it's
  a data-holder passed *into* `EntityEditorFactory`, not something users construct directly with
  invalid-input concerns. **Review-only**; no factory to add.
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
  `if (page is not null) return Exceptional.Success(page);` early return before the lock/chain — **[v2]** note
  the explicit-factory call, not a bare `return page;`, per the Option E correction above).
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
  "keep, it's load-bearing Avalonia guidance" call as `Program.cs`'s two genuine template comments in Phase 4.

---

## Summary table

| Phase | Files | Rewrite weight |
|---|---|---|
| 0 | PlaywrightService, IPlaywrightService (polish only) | Low **[v2, new]** |
| 1 | DatabaseMigrator, UpdateService, ApplicationConfigurationFactory, ApplicationOptionsRegistrar, SerilogConfigurator | Medium |
| 2 | ApplicationMetadata, ApplicationDirectories, IApplicationDirectories | Low |
| 3 | ScrapeConfiguration, SearchConfiguration, ConnectionStrings, UpdateConfiguration, SyncSettings | Low **[v2, resolved — was Low-Medium/open]** |
| 4 | Program, App.axaml, ServiceCollectionExtensions, IPlaywrightService | Low |
| 5 | IScrapeContextReader, ScrapeContextReader, ScrapeContext, ScrapeCategory, DirectoryLayout | Low **[v2, resolved]** |
| 6 | IWallpaperCountReader, WallpaperCountReader, IWallpaperHrefCollector, WallpaperHrefCollector | Low-Medium |
| 7 | ITagReader, TagReader, TagData, TagCuration, TagCurator | Medium |
| 8 | IWallpaperImageLocator, WallpaperImageLocator, IWallpaperImageDownloader, WallpaperImageDownloader | Low-Medium (correctness fix) |
| 9 | Thumbnail generator/publisher/feed/broadcaster/decorator (6 files) | None expected |
| 10 | Image dimensions + file store (6 files) | None expected |
| 11 | WallpaperDirectoryResolver, WallpaperCategoryRegistrar, WallpaperFileClassificationRepository (+interfaces) | Low-Medium |
| 12 | IScrapeAction, SearchCategoryScrapeAction | None expected (already exemplar) |
| 13 | EntityEditorDescriptor, IEntityEditorFactory, EntityEditorFactory | Low **[v2, resolved]** |
| 14 | EntityEditorViewModelBase, EntityEditorViewModel, EntityEditorWindow.axaml.cs | **High** |
| 15 | ViewModelBase, MainWindowViewModel, MainWindow.axaml.cs, ConfirmDialog.axaml.cs | **High** |

64 files across Phases 1-15 (unchanged from v1; verified by file count against the actual project tree),
plus 2 files in the new Phase 0 polish pass (`PlaywrightService.cs`, `IPlaywrightService.cs` — not part of
the 64, since they're not being rewritten, only patched).

## Resolved-in-v2 (were open questions in v1)

- **Phase 3:** the five `IOptions<T>`-bound POCOs are a permanent, documented exception to the record/Factory
  rule — config binder constraints make a validating factory structurally impossible, not just impractical.
- **Phase 5 / 13:** read-only projection records (`ScrapeContext`, `ScrapeCategory`, `DirectoryLayout`,
  `EntityEditorDescriptor<TEntity>`) are exempt from the Factory requirement — no validation concern exists
  for internal-only projections. Apply the same test (validation concern vs. none) to any other record
  encountered mid-phase that wasn't explicitly named here (see Phase 7 and Phase 10 notes above — don't
  assume exemption, check the construction site).
- **Phase 4:** the two genuine Avalonia-template comments (`Program.cs` init/AppMain warning + Avalonia-config
  warning, `App.axaml.cs` Dispose "do not change" comment) are accepted as permanent exceptions to the
  no-comments rule. The Velopack-ordering comment in `Program.cs` is a separate case — it already satisfies
  the house "non-obvious WHY" comment rule on its own merits and needs no special exception.

## Still-open questions to resolve at the relevant phase

- **Phase 3:** confirm whether `SyncSettings` (currently the only `record` among the five) is actually bound
  via `IConfiguration` the same way as the other four. If so, it should convert to a mutable `class` to match
  its siblings — the other four should not be forced into matching it.
- **Phase 7 / 10:** for `TagData`, `TagCuration`, `ImageDimensions`, `SavedWallpaperFile` — apply the
  validation-concern test before deciding Factory-exempt vs. needs-Factory; don't default to either without
  checking how each is constructed.
- **Phase 11:** confirm the `WallpaperFileClassificationRepository.RecordAsync` rewrite touches only
  `FileClassificationCategories`, never `SearchCategories` (user-managed, per project memory) — call this out
  explicitly in the PR description as a guardrail for reviewers.
