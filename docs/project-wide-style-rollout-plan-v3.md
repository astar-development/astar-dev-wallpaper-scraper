# AStar.Dev.Wallpaper.Scraper — Project-Wide Style Rollout Plan (v3)

Supersedes `docs/project-wide-style-rollout-plan-v2.md`. Changes from v2 are called out inline as
**[v3]** notes; v2's own **[v2]** notes are left in place so the plan's history stays legible. v1 and
v2 remain on disk for history; this file is the one to follow.

## [v3] Architecture decision considered and rejected: event sourcing

Before this revision, moving `AStar.Dev.Infrastructure.AppDb` to event-sourced persistence was
considered, specifically to unlock more immutability across the codebase (mutations become
recorded facts/events, aggregates rebuilt by folding events, no mutable EF-tracked entities at all).

**Rejected.** This is a single-user desktop app over local SQLite with a CRUD-shaped domain
(categories, tags, scraped files, thumbnails) — none of the properties that earn event sourcing its
cost are present: no audit/temporal-query requirement, no multiple read-model projections, no
replay/rebuild need, no undo/redo, no multi-writer eventual consistency. The stated driver was
"more immutability," which is a style preference, not a domain requirement, and event sourcing here
would trade a small style win for a large amount of new machinery (event schema versioning,
snapshotting, projections separate from the write model, migrating existing schema/data into an
event log, state now derived rather than stored). That's added complexity, not the "codebase gets
simpler overall" outcome hoped for.

**Chosen instead: immutability up to the persistence boundary.** Domain values (`ScrapeContext`,
`TagData`, scrape results, etc.) stay immutable records everywhere in service/scraping/orchestration
code; EF Core keeps tracking and mutating entities, but only inside the repository/persistence layer,
which acts as the translation point (anti-corruption layer) between immutable domain records and
mutable EF entities. This gets nearly all the composability/declarative benefit FP immutability
offers, without an architecture rewrite. See the new **Immutability Principle** section below for the
concrete rule and its documented exceptions.

**One narrower case flagged for later, separately:** the classification/tagging subsystem
(`SearchCategories` vs. `FileClassificationCategories`/"Unclassified" — see project memory
`scraper-category-persistence`) has a real provenance story (scraper-proposed vs. user-confirmed
classification) that a small, scoped event log could genuinely serve better than a plain EF table.
This is **not** part of this plan — if it's ever pursued, it should be its own initiative, scoped to
that one subsystem, evaluated on its own merits after Phases 1-15 land.

---

## [v3] Immutability Principle

**Default: immutable.** Domain values, DTOs, scrape/tag/category data, configuration projections,
and pipeline intermediate values are `record`s with `init` properties and `IReadOnlyList<T>`/
`IReadOnlyDictionary<K,V>` collections. Service and scraping logic is written as LINQ/declarative
pipelines over these values, not imperative loops mutating local state.

**Documented exceptions — mutation is allowed only in these five places, for the reason given:**

1. **EF Core entities, inside the repository/persistence layer only** (Phase 11's
   `WallpaperCategoryRegistrar`, `WallpaperFileClassificationRepository`, and `ScrapeContextReader`
   from Phase 5). EF's change tracker requires mutable, identity-tracked entities. These classes are
   the anti-corruption boundary: they read/write mutable EF entities internally but only ever accept
   or return immutable domain records across their public API. No EF entity type crosses out of this
   layer in either direction.
2. **`IOptions<T>`-bound configuration POCOs** (Phase 3's five classes). The `IConfiguration` binder
   requires a parameterless constructor and settable/init members it writes to directly — this can't
   be routed through a validating factory, and `record`-with-`init` doesn't remove the constraint
   either, so these stay mutable `class`es.
3. **ReactiveUI ViewModels** (`MainWindowViewModel`, `EntityEditorViewModelBase`/`EntityEditorViewModel`,
   etc.). `ReactiveObject`'s whole model is mutation + change notification (`WhenAnyValue`, property
   setters raising `PropertyChanged`) — an immutable VM would mean replacing the entire instance per
   property change, which breaks data binding. Not a candidate for immutability.
4. **`PlaywrightService`-style resource caches.** `playwright`/`context`/`page` are mutable fields
   because they cache expensive, stateful external resources (browser process/context) across calls.
   The refactor-options doc explored an explicit immutable-state-record alternative (Option D) and
   rejected it as fighting the grain of a legitimate caching design — same reasoning applies to any
   future service with the same shape.
5. **`EntityEditorViewModel<TEntity>`'s edited entity state** (Phase 14). The entity bound to the
   editable DataGrid needs settable properties for two-way binding to work at all — same underlying
   reason as (3), called out separately here because it's specifically about the edited-entity model,
   not the ViewModel's own reactive properties.

Anything not on this list defaults to immutable. If a new case seems to need mutation, check it
against these five reasons (EF change-tracking, external framework binding contract, UI two-way
binding, or caching a genuinely stateful external resource) before adding a sixth exception.

---

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

**[v2] Correction, carried forward:** the file is Option E from `playwright-service-refactor-options.md`,
*adapted*, not verbatim:

- Real code uses an instance method `AcquireConfigureLockAsync` + a private nested `ConfigureLockScope`
  class — not Option E's `SemaphoreSlimExtensions.WaitAndReleaseAsync` extension + standalone `Releaser`.
  Simpler, no dependency on newer C# extension-member syntax. The doc is what's stale, not the code.
- Option E's sample `return page;` doesn't compile as written — implicit `Exceptional<IPage>` conversion
  fails because the C# spec ignores user-defined conversions from interface source types. Shipped code
  uses the explicit `Exceptional.Success(page)` factory instead (fixed during the 2026-07-13 review; see
  memory `playwrightservice-review-findings`). Any future interface-typed `Exceptional<T>` return needs the
  same explicit-factory treatment.

**Action (unchanged from v2):** `docs/playwright-service-refactor-options.md` gets a short correction note
at the top of Option E pointing at the real implementation and these two deltas. No code changes needed —
the shipped pattern is correct; only the doc was stale.

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
4. **Config POCOs (Phase 3)** — the five `IOptions<T>`-bound classes are a permanent, documented exception
   to the "record + Factory" rule (see Immutability Principle, exception 2). Applies to all five.
5. **Read-only projection records (Phase 5, 13)** — `ScrapeContext`, `ScrapeCategory`, `DirectoryLayout`,
   `EntityEditorDescriptor<TEntity>` are exempt from the "every record needs a `Factory`" rule — no
   user/API-facing validation concern for internal-only projections.
6. **`PlaywrightService.cs` reference-file polish** — see Phase 0. Small, low-risk fixes to the reference
   file itself before other phases start copying its pattern.
7. **[v3] Persistence architecture stays EF Core, not event-sourced** — see the rejected-alternative note
   above. Immutability is pursued up to the persistence boundary only, per the new Immutability Principle.

`PlaywrightService.cs` and `IPlaywrightService.cs`'s implementation are excluded from the numbered phases
(structurally done) — `IPlaywrightService.cs` still gets a review pass in Phase 4 alongside the rest of the
composition root, since its XML docs haven't been touched directly. Phase 0 covers the handful of concrete
fixes identified in `PlaywrightService.cs`/`IPlaywrightService.cs` that predate this plan; Phase 4's review
of `IPlaywrightService.cs` is otherwise unchanged.

---

## Phase 0 — Reference file polish

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
- The doc-comment spacing inconsistency isn't unique to this phase — `IPlaywrightService.cs` (Phase 0/4)
  has the same `///     ` 5-space pattern. Treat "normalize `///` indentation to match the prevailing
  convention" as a checklist item in **every** phase's review pass, not just Phase 2.

---

## Phase 3 — Options POCOs

**Files:** `Configuration/ScrapeConfiguration.cs`, `Configuration/SearchConfiguration.cs`,
`Configuration/ConnectionStrings.cs`, `Configuration/UpdateConfiguration.cs`, `Configuration/SyncSettings.cs`

- These five stay mutable `class`es with `{ get; set; }` properties — permanent, documented exception to
  the record/Factory rule per the Immutability Principle (exception 2). The `IConfiguration` binder requires
  a parameterless constructor and settable/init members it writes to directly; a validating factory can't sit
  in front of that, and `record`-with-`init` doesn't remove the constraint.
- **[v3, resolved]** `SyncSettings` is currently the odd one out — a `record` with `required`/`init`
  properties, while the other four are mutable classes. Confirm it's bound via `IConfiguration` the same way
  as the other four (it almost certainly is, per `appsettings.json`'s `scrapeConfiguration`/`updateConfiguration`
  structure) and **convert it to a mutable `class` to match its siblings.** The other four should not be
  changed to match it — consistency direction is class, not record, for this exception group.
- Add a one-line XML doc comment on each of the five classes stating this is a deliberate exception (e.g.
  `/// Mutable to support direct <see cref="IConfiguration"/> binding; not a candidate for the Records rule.`).
- Add missing XML docs on `ScrapeConfiguration.ApplicationName`/`ConnectionStrings`, `ConnectionStrings.Sqlite`,
  `UpdateConfiguration.RepositoryUrl` (currently undocumented, unlike their siblings).

---

## Phase 4 — Composition root

**Files:** `Program.cs`, `App.axaml.cs`, `ServiceCollectionExtensions.cs`, `Services/IPlaywrightService.cs`

- `Program.cs`: has three `//` comment blocks, only two of which are Avalonia-template boilerplate:
  - Lines 11-13 (`Initialization code. Don't use any Avalonia, third-party APIs...`) — Avalonia project
    template text. Keep verbatim; documented exception.
  - Line 24 (`Avalonia configuration, don't remove; also used by visual designer.`) — also Avalonia template
    text. Same treatment.
  - Lines 17-18 (`Velopack hooks install/update/uninstall events and may exit the process; it must run
    before anything else touches the app state.`) — **not** template boilerplate, it's a project-authored
    comment explaining a genuinely non-obvious ordering constraint. This already satisfies the house
    "non-obvious WHY" comment rule on its own — no Avalonia-template exception needed, no change required;
    just don't lump it in with the other two when writing the PR justification.
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
- `IPlaywrightService.cs`: the missing `<param name="cancellationToken">` doc is fixed in Phase 0; otherwise
  **review-only** here (already `Exceptional`-based, matches target style).

---

## Phase 5 — Scraping: search context & category discovery

**Files:** `Scraping/IScrapeContextReader.cs`, `Scraping/ScrapeContextReader.cs`, `Scraping/ScrapeContext.cs`,
`Scraping/ScrapeCategory.cs`, `Scraping/DirectoryLayout.cs`

- `ScrapeContextReader.ReadAsync`: five sequential EF Core queries with no failure handling — matches "internal
  operations, exceptions OK" rule; **review-only** for structure, but confirm it wouldn't read better as a
  handful of named private query methods (`ReadSearchConfigurationAsync`, `ReadCategoriesAsync`, ...) the way
  `PlaywrightService` extracts its steps, given the constructor call at the end assembles five results.
- **[v3]** `ScrapeContextReader.ReadAsync` is this phase's designated anti-corruption boundary (Immutability
  Principle, exception 1) — confirm as an explicit acceptance check, not just structure cleanup, that no EF
  entity type is returned from or accepted by `ReadAsync`; every EF query result gets projected into
  `ScrapeContext`/`ScrapeCategory`/`DirectoryLayout` before it leaves the method.
- `ScrapeContext`, `ScrapeCategory`, `DirectoryLayout`: plain records — no `Factory` required. These are
  read-only projections of DB rows/config with no validation concern, exempted per the decision at the top
  of this plan. **Review-only** for doc/naming consistency only.
- `IScrapeContextReader`: **review-only**.

---

## Phase 6 — Scraping: search results page reading

**Files:** `Scraping/IWallpaperCountReader.cs`, `Scraping/WallpaperCountReader.cs`,
`Scraping/IWallpaperHrefCollector.cs`, `Scraping/WallpaperHrefCollector.cs`

- `WallpaperHrefCollector.CollectAsync`: builds a `List<string>` via manual `foreach` + conditional `Add` —
  rewrite as a LINQ pipeline (`previews.SelectAsync(...).Where(...)` or similar via
  `AStar.Dev.FunctionalParadigm`'s async LINQ helpers, matching how `SearchCategoryScrapeAction` composes
  collections elsewhere in the codebase) instead of an imperative loop. **[v3]** The rewritten method should
  return `IReadOnlyList<string>`, not `List<string>` — matches the existing "immutable collections for
  multi-value properties and return types" rule, which the current mutable-list return doesn't honor.
- `WallpaperCountReader.ReadAsync`: already a tight LINQ-ish expression — **review-only**.
- Both interfaces: **review-only**.

---

## Phase 7 — Scraping: wallpaper detail page & tag curation

**Files:** `Scraping/ITagReader.cs`, `Scraping/TagReader.cs`, `Scraping/TagData.cs`, `Scraping/TagCuration.cs`,
`Scraping/TagCurator.cs`

- `TagCurator.Curate`: uses a C-style `for (int i = 0; i < tags.Count; i++)` loop with `continue`. **[v3,
  firmed up from v2's "likely needs"]**: rewrite as `tags.Select(NormalizeModelSuffix).Where(...)`, where
  `NormalizeModelSuffix` returns an immutable per-tag result record (e.g. `TagCurationOutcome(bool Kept,
  string Message)`), folded via LINQ into the final `TagCuration` at the end. No mutable `kept`/`messages`
  lists at any point in the method — this is the definitive shape, not one option among several.
- `TagReader.ReadAsync`: already clean (`Task.WhenAll` + `Select`) — **review-only**.
- `TagData`, `TagCuration`: **review-only** records. **[v3, decision rule replacing v2's per-file
  open question]**: a record needs a `Factory` if it's constructed from scraped/external/unvalidated input;
  it's exempt if it's a pure internal projection built only from already-validated data (matching Phase 5's
  reasoning). Apply this test to `TagData`/`TagCuration` at phase start — check the construction site, don't
  assume either way. The same test applies to any other record encountered mid-phase across the whole plan.

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
- `ImageDimensions` and `SavedWallpaperFile` are records — apply the same Factory decision rule as Phase 7:
  needs a `Factory` if constructed from scraped/external/unvalidated input, exempt if it's a pure internal
  projection built only from already-validated data. Check the construction site at phase start; don't assume
  exempt by default here.

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
  - Per memory (`scraper-category-persistence`): `SearchCategories` is user-managed and must never be written
    by scraper code; scraped tags belong under `FileClassificationCategories` with an "Unclassified" bucket.
    Confirm this phase's rewrite doesn't touch `SearchCategories` — scope the LINQ-pipeline rewrite to
    `FileClassificationCategories` writes only. Call this out explicitly in the phase's PR description since
    it's an easy invariant to accidentally break while restructuring the loop.
- **[v3]** Both `WallpaperCategoryRegistrar` and `WallpaperFileClassificationRepository` are, together with
  `ScrapeContextReader` (Phase 5), the designated mutation boundary under the Immutability Principle
  (exception 1) — confirm their public method signatures accept/return only immutable domain records, never
  EF entity types, so mutability stays contained to these classes' internals.
- `WallpaperDirectoryResolver.Resolve`: already a clean LINQ pipeline — **review-only**.
- Both interfaces: **review-only**.

---

## Phase 12 — Scraping: orchestration

**Files:** `Scraping/IScrapeAction.cs`, `Scraping/SearchCategoryScrapeAction.cs`

- `SearchCategoryScrapeAction` already uses `Try.RunAsync`/`Exceptional`/`MatchAsync` and named private methods
  per pipeline stage — this is effectively already at the target style (it's the second-best example in the
  codebase after `PlaywrightService`).
- **[v3, elevated from v2's "optional follow-up"]**: `VisitCategoryPageAsync` (7 params) and
  `DownloadWallpaperAsync` (8 params) should be collapsed into a single immutable context record now that
  Phase 5 has already built `ScrapeContext` for exactly this shape of data. This isn't just a param-count
  cleanup — it's the concrete FP-composability win the Immutability Principle is chasing: one immutable value
  threaded through the pipeline instead of 7-8 loose primitives. Low risk (both methods are internal,
  well-covered), do it as part of this phase rather than deferring.
- `IScrapeAction`: **review-only**.

---

## Phase 13 — Entity editor: descriptor & factory

**Files:** `Configuration/EntityEditor/EntityEditorDescriptor.cs`, `Configuration/EntityEditor/IEntityEditorFactory.cs`,
`Configuration/EntityEditor/EntityEditorFactory.cs`

- `EntityEditorDescriptor<TEntity>`: a `record` with a `Func<TEntity> CreateNew` constructor parameter —
  exempt from the "every record needs a `Factory`" rule, same reasoning as Phase 5 — it's a data-holder
  passed *into* `EntityEditorFactory`, not something users construct directly with invalid-input concerns.
  **Review-only**; no factory to add.
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
  `if (page is not null) return Exceptional.Success(page);` early return before the lock/chain — explicit
  factory call, not a bare `return page;`, per the Option E correction above).
- **[v3]** `TEntity` itself stays mutable while bound to the editable DataGrid — this is Immutability
  Principle exception 5, called out separately from the ViewModel exception (3) because it's specifically
  about the edited-entity model, not `EntityEditorViewModel`'s own reactive properties. No change expected
  here; noted so this phase doesn't get flagged as an inconsistency against the immutable-by-default rule.
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
| 0 | PlaywrightService, IPlaywrightService (polish only) | Low |
| 1 | DatabaseMigrator, UpdateService, ApplicationConfigurationFactory, ApplicationOptionsRegistrar, SerilogConfigurator | Medium |
| 2 | ApplicationMetadata, ApplicationDirectories, IApplicationDirectories | Low |
| 3 | ScrapeConfiguration, SearchConfiguration, ConnectionStrings, UpdateConfiguration, SyncSettings | Low **[v3: SyncSettings converts class]** |
| 4 | Program, App.axaml, ServiceCollectionExtensions, IPlaywrightService | Low |
| 5 | IScrapeContextReader, ScrapeContextReader, ScrapeContext, ScrapeCategory, DirectoryLayout | Low |
| 6 | IWallpaperCountReader, WallpaperCountReader, IWallpaperHrefCollector, WallpaperHrefCollector | Low-Medium |
| 7 | ITagReader, TagReader, TagData, TagCuration, TagCurator | Medium |
| 8 | IWallpaperImageLocator, WallpaperImageLocator, IWallpaperImageDownloader, WallpaperImageDownloader | Low-Medium (correctness fix) |
| 9 | Thumbnail generator/publisher/feed/broadcaster/decorator (6 files) | None expected |
| 10 | Image dimensions + file store (6 files) | None expected |
| 11 | WallpaperDirectoryResolver, WallpaperCategoryRegistrar, WallpaperFileClassificationRepository (+interfaces) | Low-Medium |
| 12 | IScrapeAction, SearchCategoryScrapeAction | Low **[v3: param-collapse now in scope]** |
| 13 | EntityEditorDescriptor, IEntityEditorFactory, EntityEditorFactory | Low |
| 14 | EntityEditorViewModelBase, EntityEditorViewModel, EntityEditorWindow.axaml.cs | **High** |
| 15 | ViewModelBase, MainWindowViewModel, MainWindow.axaml.cs, ConfirmDialog.axaml.cs | **High** |

64 files across Phases 1-15, plus 2 files in the Phase 0 polish pass (`PlaywrightService.cs`,
`IPlaywrightService.cs` — not part of the 64, since they're patched, not rewritten). File count verified
against the actual project tree.

## Resolved in v3

- **Architecture:** event-sourced persistence considered, explicitly rejected (see top of this file).
  Immutability pursued up to the persistence boundary, codified as the Immutability Principle.
- **Phase 3:** `SyncSettings` converts from `record` to mutable `class` to match its four siblings.
- **Phase 6:** `WallpaperHrefCollector.CollectAsync` returns `IReadOnlyList<string>`.
- **Phase 7:** `TagCurator` fix is now definitive (immutable per-tag result record, no mutable lists),
  not one option among several.
- **Phase 7 / 10:** record-Factory question replaced with a reusable decision rule (validated-input
  construction needs a Factory; pure internal projection is exempt) instead of a per-file open question.
- **Phase 12:** `VisitCategoryPageAsync`/`DownloadWallpaperAsync` param-collapse into an immutable context
  record moves from optional follow-up to in-scope for this phase.
- **Phase 14:** the edited `TEntity`'s mutability is now an explicitly documented exception (5) rather than
  an implicit, unstated one.

## Resolved in v2 (carried forward)

- **Phase 3:** the five `IOptions<T>`-bound POCOs are a permanent, documented exception to the record/Factory
  rule.
- **Phase 5 / 13:** read-only projection records are exempt from the Factory requirement.
- **Phase 4:** the two genuine Avalonia-template comments are permanent exceptions to the no-comments rule;
  the Velopack-ordering comment needs no such exception, it already satisfies the house comment rule.

## Still-open questions to resolve at the relevant phase

- **Phase 7 / 10:** apply the record-Factory decision rule to `TagData`, `TagCuration`, `ImageDimensions`,
  `SavedWallpaperFile` at phase start — check each construction site, don't default either way.
- **Phase 11:** confirm the `WallpaperFileClassificationRepository.RecordAsync` rewrite touches only
  `FileClassificationCategories`, never `SearchCategories` — call this out explicitly in the PR description
  as a guardrail for reviewers.
