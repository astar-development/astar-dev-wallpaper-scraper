---
name: ship
description: Full issue -> branch -> TDD red/green -> build/test -> PR pipeline for new development work in this repo, per CLAUDE.md Project Rules.
---

Follow this pipeline for any new development task in this repo. Do not skip steps even if not explicitly asked - they are standing project rules (see CLAUDE.md).

1. **Issue** - Confirm a GitHub Issue exists for this work. If not, create one with full context (`gh issue create`), asking the user for missing details.
2. **Branch** - Create `feature/<issue-number>-short-description` or `bug/<issue-number>-short-description` (or matching prefix) from the issue.
3. **Red** - Write a failing test for the next behaviour. Run it, confirm it fails. Commit it alone: `test: <description> (red)`.
4. **Green** - Implement the minimal production code to pass. Run the test again, confirm it passes. Commit separately: `feat: <description>` / `fix: <description>`. Never combine steps 3 and 4 into one commit.
5. Repeat steps 3-4 until the feature/fix is complete. Follow CLAUDE.md Project Rules throughout (XML docs, `Async` suffix, blank line before `return`, no unrelated changes).
6. **Verify** - Build (`dotnet build`) and run the affected/new test projects (`dotnet test --project <Project>`). Report the pass count (e.g. `206/206 passing`). Do not proceed with unresolved failures.
7. **Stop for review** - Do not push or open a PR automatically. Stop, summarize the change, and ask the user to review. Offer to raise the PR.
8. **PR** - Only after the user confirms: push the branch and open the PR (`gh pr create`), referencing the issue.
