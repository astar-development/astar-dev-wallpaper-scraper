# PlaywrightService — `ConfigurePlaywrightAsync` Refactor Options

Current method (`AStar.Dev.Wallpaper.Scraper/Services/PlaywrightService.cs:20-58`) is a single `Try.RunAsync().TapAsync().MapAsync().MapAsync().MapAsync().TapAsync().MapAsync().Ensure()` chain. It works and keeps the FP style (`Bind`/`Map`/`Tap`/`Ensure` from `AStar.Dev.FunctionalParadigm`), but every step is an inline lambda, so the chain reads as one long block and any new logging has to be squeezed into an existing `TapAsync` or a new one inserted mid-chain.

Five options below, roughly ordered from smallest diff to largest. All keep the monadic chain — none suggest going back to imperative try/catch. Pick one, or mix (Option E is a suggested combination).

---

## Option A — Extract each step into a named private method

Keep the exact same chain shape, but every lambda becomes a call to a 1-3 line private method. The chain becomes a table of contents; each method is independently readable and a natural place to drop a log line.

```csharp
public async Task<Exceptional<IPage>> ConfigurePlaywrightAsync(CancellationToken cancellationToken)
{
    var lockAcquired = false;

    return await Try.RunAsync(async () => await AcquireLockAndCreateDirectoryAsync(cancellationToken))
        .TapAsync(_ => LogDirectoryCreated(lockAcquired))
        .MapAsync(_ => GetOrCreatePlaywrightAsync())
        .MapAsync(GetOrCreateContextAsync)
        .MapAsync(AddHideWebdriverScriptAsync)
        .TapAsync(_ => LogConfigured(lockAcquired))
        .MapAsync(GetOrCreatePageAsync)
        .Ensure(_ => ReleaseLockIfAcquired(lockAcquired));

    async Task<IPage?> AcquireLockAndCreateDirectoryAsync(CancellationToken token)
    {
        if (page is not null) return page;

        await configureLock.WaitAsync(token);
        lockAcquired = true;
        fileSystem.Directory.CreateDirectory(scrapeConfiguration.Value.UserDataDirectory);

        return null;
    }
}

private async Task<IPlaywright> GetOrCreatePlaywrightAsync()
    => playwright ??= await Playwright.CreateAsync().ConfigureAwait(false);

private async Task<IBrowserContext> GetOrCreateContextAsync(IPlaywright _)
    => context ??= await playwright!.Chromium.LaunchPersistentContextAsync(scrapeConfiguration.Value.UserDataDirectory, SetContext()).ConfigureAwait(false);

private async Task<IBrowserContext> AddHideWebdriverScriptAsync(IBrowserContext browserContext)
{
    await browserContext.AddInitScriptAsync(HideWebdriverScript).ConfigureAwait(false);

    return browserContext;
}

private async Task<IPage> GetOrCreatePageAsync(IBrowserContext browserContext)
    => page ??= await browserContext.NewPageAsync().ConfigureAwait(false);

private void LogDirectoryCreated(bool lockAcquired)
{
    if (lockAcquired) LogMessage.Information(logger, "User data directory created successfully.");
}

private void LogConfigured(bool lockAcquired)
{
    if (lockAcquired) LogMessage.Information(logger, "Playwright configured successfully.");
}

private void ReleaseLockIfAcquired(bool lockAcquired)
{
    if (lockAcquired) configureLock.Release();
}
```

**Pros**

- Smallest possible diff — chain length and step order unchanged.
- Every step name documents intent; new logging goes straight into the relevant method, no lambda surgery.
- Each private method is directly unit-testable in isolation if you ever want that.

**Cons**

- `lockAcquired` still threaded around as a captured local plus a nested local function — doesn't fix the awkward lock bookkeeping, just relocates it.
- Method count roughly doubles; some may feel like ceremony for a one-line body (e.g. `AddHideWebdriverScriptAsync`).

---

## Option B — Replace the `lockAcquired` flag with a lock-scope helper

The flag exists purely to answer "did *this* call acquire the lock, or did it see `page is not null` and skip entirely?" — so logging/release only fire when real work happened. Wrap that in a small disposable scope instead of a bool threaded through `Tap`/`Ensure`.

```csharp
private async Task<IDisposable?> AcquireConfigureLockAsync(CancellationToken cancellationToken)
{
    if (page is not null) return null;

    await configureLock.WaitAsync(cancellationToken).ConfigureAwait(false);

    return new Releaser(configureLock);
}

private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
{
    public void Dispose() => semaphore.Release();
}
```

```csharp
public async Task<Exceptional<IPage>> ConfigurePlaywrightAsync(CancellationToken cancellationToken)
{
    using var lockScope = await AcquireConfigureLockAsync(cancellationToken);

    if (page is not null) return page;

    return await Try.RunAsync(async () =>
        {
            fileSystem.Directory.CreateDirectory(scrapeConfiguration.Value.UserDataDirectory);

            return page;
        })
        .TapAsync(_ => LogMessage.Information(logger, "User data directory created successfully."))
        .MapAsync(async _ => playwright ??= await Playwright.CreateAsync().ConfigureAwait(false))
        .MapAsync(async _ => context ??= await playwright!.Chromium.LaunchPersistentContextAsync(scrapeConfiguration.Value.UserDataDirectory, SetContext()).ConfigureAwait(false))
        .MapAsync(async browserContext =>
        {
            await browserContext!.AddInitScriptAsync(HideWebdriverScript).ConfigureAwait(false);

            return browserContext;
        })
        .TapAsync(_ => LogMessage.Information(logger, "Playwright configured successfully."))
        .MapAsync(async browserContext => page ??= await browserContext!.NewPageAsync().ConfigureAwait(false));
}
```

**Pros**

- No more `lockAcquired` bool, no `Ensure` step doing double duty as "release the lock" — `using` makes acquire/release symmetric and impossible to forget.
- `Tap` calls become unconditional — they only run when the pipeline actually runs, since the early `page is not null` return happens before entering the monad at all.
- Chain shrinks by one step (`Ensure` gone entirely).

**Cons**

- The "did we skip because already configured" check exists twice (once in `AcquireConfigureLockAsync`, once as a guard clause) — small duplication, but each check is one line and cheap.
- `Releaser` is a new small type — minor ceremony for a single call site, though it is a common, well-understood pattern.
- Loses the "only log if this call is the one that did the work" nuance unless the early return is kept — worth confirming that's still the desired behaviour (a second concurrent caller arriving after the lock releases but with `page` now set would previously skip logging too; here it skips via the guard clause, same result).

---

## Option C — Merge steps, cut the chain down to essentials

Rather than one `Map` per side effect, fold closely-related steps together. Two candidates:

1. **Context creation + init script are really one "get me a ready-to-use context" step** — no reason to `Map` twice.
2. **Two `Tap` calls for logging can become one**, logged once configuration is fully done (directory + browser + page), if per-stage granularity isn't actually needed.

```csharp
public async Task<Exceptional<IPage>> ConfigurePlaywrightAsync(CancellationToken cancellationToken)
{
    if (page is not null) return page;

    var lockAcquired = false;

    return await Try.RunAsync(async () =>
        {
            await configureLock.WaitAsync(cancellationToken);
            lockAcquired = true;
            fileSystem.Directory.CreateDirectory(scrapeConfiguration.Value.UserDataDirectory);

            playwright ??= await Playwright.CreateAsync().ConfigureAwait(false);
            context ??= await CreateReadyContextAsync().ConfigureAwait(false);

            return page ??= await context.NewPageAsync().ConfigureAwait(false);
        })
        .TapAsync(_ =>
        {
            if (lockAcquired) LogMessage.Information(logger, "Playwright configured successfully.");
        })
        .Ensure(_ =>
        {
            if (lockAcquired) configureLock.Release();
        });
}

private async Task<IBrowserContext> CreateReadyContextAsync()
{
    var browserContext = await playwright!.Chromium.LaunchPersistentContextAsync(scrapeConfiguration.Value.UserDataDirectory, SetContext()).ConfigureAwait(false);

    await browserContext.AddInitScriptAsync(HideWebdriverScript).ConfigureAwait(false);

    return browserContext;
}
```

**Pros**

- Chain drops from 7 links to 3 (`Try.RunAsync` → `TapAsync` → `Ensure`) — much less visual noise.
- `CreateReadyContextAsync` reads as one coherent unit of work ("give me a context that's ready to use"), matching how you'd describe it in a sentence.

**Cons**

- **This is the one that costs you something**: per-stage logging (directory created vs. Playwright launched vs. context ready vs. page ready) is gone — you'd only know "configured successfully" as one lump, or "failed" with no stage information beyond the exception message. Given you specifically said logging matters here, weigh this against Option A before picking it.
- The body of `Try.RunAsync` is back to an imperative-feeling multi-statement block rather than a Map-per-step pipeline — still safe (still funnelled through `Try`/`Exceptional`), but less "each line is a pipeline stage" than the original.

---

## Option D — Thread an explicit state object through `Bind` instead of mutable fields

Most structurally different option. The current lambdas close over `playwright`/`context`/`page` mutable fields, so `Map` steps aren't really pure functions of their input — they're reading/writing instance state. An alternative: carry an explicit immutable state record through `Bind`, only assigning back to the fields once at the very end (or not at all, if callers can hold onto the returned state).

```csharp
private sealed record PlaywrightState(IPlaywright Playwright, IBrowserContext Context, IPage Page);

public async Task<Exceptional<IPage>> ConfigurePlaywrightAsync(CancellationToken cancellationToken)
{
    if (page is not null) return page;

    var lockAcquired = false;

    return await Try.RunAsync(async () =>
        {
            await configureLock.WaitAsync(cancellationToken);
            lockAcquired = true;
            fileSystem.Directory.CreateDirectory(scrapeConfiguration.Value.UserDataDirectory);

            return await Playwright.CreateAsync().ConfigureAwait(false);
        })
        .BindAsync(async instance =>
        {
            var browserContext = await instance.Chromium.LaunchPersistentContextAsync(scrapeConfiguration.Value.UserDataDirectory, SetContext()).ConfigureAwait(false);
            await browserContext.AddInitScriptAsync(HideWebdriverScript).ConfigureAwait(false);
            var newPage = await browserContext.NewPageAsync().ConfigureAwait(false);

            playwright = instance;
            context = browserContext;
            page = newPage;

            return new Success<PlaywrightState>(new PlaywrightState(instance, browserContext, newPage));
        })
        .TapAsync(_ => LogMessage.Information(logger, "Playwright configured successfully."))
        .MapAsync(state => state.Page)
        .Ensure(_ =>
        {
            if (lockAcquired) configureLock.Release();
        });
}
```

**Pros**

- Makes the "this is really one atomic setup operation producing three related objects" explicit via the record, rather than three independent `??=` checks scattered across three `Map` calls.

**Cons**

- **Not actually simpler** — it reintroduces a multi-statement lambda body (same complaint as Option C) *and* adds a new record type *and* still needs to assign back to the mutable fields somewhere. This option is included for completeness/contrast, not as a real recommendation — it fights the grain of the existing "fields as cache, `??=` per step" design rather than embracing it. Skip unless you're planning to remove the mutable fields entirely and make `PlaywrightService` re-entrant/stateless, which is a bigger change than "simplify this method."

---

## Option E — Suggested combination (A + B + a lighter touch of C)

Take the parts that are unambiguous wins and skip the ones with real trade-offs:

- **From B**: drop the `lockAcquired` bool/`Ensure`-release pattern in favour of an `AcquireConfigureLockAsync` + `using` scope. This is a strict improvement — same behaviour, less bookkeeping.
- **From A**: extract `GetOrCreatePlaywrightAsync`, `GetOrCreateContextAsync`, `AddHideWebdriverScriptAsync`, `GetOrCreatePageAsync` as named private methods, so the chain reads top-to-bottom as a sequence of named stages.
- **Skip C's merge** — keep four separate `Map` stages (not folded into one `CreateReadyContextAsync` + imperative block), so **per-stage logging stays possible** without re-threading a lock-acquired flag through every `Tap`. Logging can move from two `Tap` calls into as many as you want, one per `Map`, without needing the `lockAcquired` guard at all (guard is now handled once, up front, by the early-return + lock scope).

```csharp
public async Task<Exceptional<IPage>> ConfigurePlaywrightAsync(CancellationToken cancellationToken)
{
    if (page is not null) return page;

    using var lockScope = await configureLock.WaitAndReleaseAsync(cancellationToken);

    return await Try.RunAsync(() => CreateUserDataDirectoryAsync())
        .TapAsync(_ => LogMessage.Information(logger, "User data directory created successfully."))
        .MapAsync(_ => GetOrCreatePlaywrightAsync())
        .MapAsync(GetOrCreateContextAsync)
        .MapAsync(HideWebdriverAsync)
        .TapAsync(_ => LogMessage.Information(logger, "Playwright configured successfully."))
        .MapAsync(GetOrCreatePageAsync);
}

private Task<IPage?> CreateUserDataDirectoryAsync()
{
    fileSystem.Directory.CreateDirectory(scrapeConfiguration.Value.UserDataDirectory);

    return Task.FromResult<IPage?>(null);
}

private async Task<IPlaywright> GetOrCreatePlaywrightAsync()
    => playwright ??= await Playwright.CreateAsync().ConfigureAwait(false);

private async Task<IBrowserContext> GetOrCreateContextAsync(IPlaywright _)
    => context ??= await playwright!.Chromium.LaunchPersistentContextAsync(scrapeConfiguration.Value.UserDataDirectory, SetContext()).ConfigureAwait(false);

private async Task<IBrowserContext> HideWebdriverAsync(IBrowserContext browserContext)
{
    await browserContext.AddInitScriptAsync(HideWebdriverScript).ConfigureAwait(false);

    return browserContext;
}

private async Task<IPage> GetOrCreatePageAsync(IBrowserContext browserContext)
    => page ??= await browserContext.NewPageAsync().ConfigureAwait(false);
```

Where `WaitAndReleaseAsync` is a tiny extension on `SemaphoreSlim`:

```csharp
public static class SemaphoreSlimExtensions
{
    extension(SemaphoreSlim semaphore)
    {
        public async Task<IDisposable> WaitAndReleaseAsync(CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            return new Releaser(semaphore);
        }
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
```

**Pros**

- No `lockAcquired` bool anywhere — lock lifetime is scoped by `using`, matching every other resource in this class (`configureLock`, `context` disposal in `DisposeAsync`).
- Chain still reads as "directory → playwright → context → hide-webdriver → page", one line per concept, each independently loggable/testable.
- Adding a new stage (e.g. a future cookie-restore step) means adding one `MapAsync(...)` line and one small private method — no lambda surgery required.

**Cons**

- Slightly more files/methods than today (one extension class, four private methods) — the trade is fewer lines *per method*, not fewer methods overall.
- `WaitAndReleaseAsync`'s extension-member syntax (`extension(SemaphoreSlim semaphore) { ... }`) is a newer C# feature — confirm it's available/idiomatic for this codebase's C# version before adopting; a plain static method (`SemaphoreSlimExtensions.WaitAndReleaseAsync(semaphore, cancellationToken)`) works identically if not.

---

## Summary Table

| Option | Chain length | Removes `lockAcquired` bool | Keeps per-stage logging | New types | Risk |
|---|---|---|---|---|---|
| A — extract methods | unchanged (7) | no | yes | 0 | very low |
| B — lock-scope helper | 6 | yes | yes | 1 (`Releaser`) | low |
| C — merge steps | 3 | no | **no — lost** | 1 (`CreateReadyContextAsync`) | medium (logging regression) |
| D — explicit state record | 5 | no | partial | 1 (`PlaywrightState`) | not recommended |
| E — A + B combined | 6 | yes | yes | 2 (`Releaser`, extension class) | low |

**Recommendation: Option E**, or Option A alone if you'd rather not touch the locking mechanics in this pass.
