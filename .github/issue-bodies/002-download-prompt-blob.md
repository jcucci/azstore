Summary
- Enhance the download command to prompt for a blob name or pattern when it is not supplied, enabling a smooth interactive workflow aligned with the REPL’s VIM-like UX.

Motivation
- Users often recall the container but not exact blob names.
- Promoting interactive, keyboard-first discovery reduces friction and aligns with the terminal-first design.

Desired Behavior
- If the user does not provide a blob name or pattern, prompt for one.
- Support simple exact names and glob-like patterns (e.g., `*.json`, `logs/**/2025-*.gz`).
- Continue supporting the non-interactive mode when the blob name or pattern is provided.

User Stories
- As a user, I can run `:download <container>` without a blob and be prompted to input a blob name or pattern.
- As a user, I can cancel at the prompt to abort the command gracefully.

UX/REPL Flow
1) User enters `:download <container>` with no blob argument.
2) REPL shows a minimal inline prompt requesting a blob name or pattern (single-line input; no modal list here).
3) Input is validated client-side to ensure non-empty entry; basic trimming is applied.
4) The command proceeds to evaluate the pattern (via existing glob-to-regex helper) and downloads matching blobs, respecting paging.
5) If the user cancels or submits an empty value, the command aborts with an informative message.

Scope & Technical Notes
- Command parsing: Update `DownloadCommandOptions.FromArgs` to make the blob name/pattern optional.
- Pattern handling: Reuse `AzStore.Terminal.Utilities` glob-to-regex helper (compiled, case-insensitive regex) per CLAUDE.md guidance.
- Paging: Enumerate blobs using explicit paging with `AsPages()` and `PagedResult<T>`. Avoid streaming all results; process page-by-page.
- Conflict resolution: Defer to existing file conflict handling settings and workflow. This issue does not change conflict semantics.
- Cancellation: Honor `CancellationToken` during input wait and page processing. Early-check token before long-running operations.
- Logging: Debug/verbose logs for prompt display and pattern application. No sensitive values in info logs.
- Files & Style: Keep one type per file (e.g., `DownloadCommandOptions`). Use C# 12 collection expressions.
- DI: No new dependencies expected; rely on existing services and utilities injected into the download command.

Acceptance Criteria
- Running the download command with container provided and no blob argument triggers an inline prompt requesting a blob name or pattern.
- Providing a blob name or pattern proceeds to download matching blobs using existing paging and filter logic.
- Cancelling or submitting an empty value aborts the command with a friendly message; no exceptions are thrown.
- Unit tests cover: options parsing when blob is omitted; prompt invocation; acceptance of exact and glob patterns; cancellation flow.
- Integration test (Category=Integration) validates a basic prompt → pattern → download flow against a test environment; CI excludes it by default.

Testing
- Unit: xUnit with NSubstitute; cover `FromArgs` defaults, prompt branching, and glob application via the utility helper.
- Paging behavior: Confirm that only a single page is processed at a time and continuation tokens are respected across iterations.
- CI: Use `--filter "Category!=Integration"` to exclude integration tests where Azure CLI may not be available.

Out of Scope
- Container selection UX (covered by the separate issue to prompt for container when omitted).
- Changes to conflict resolution policies or download progress UI.

Notes
- Follow CLAUDE.md guidance: explicit paging, minimal comments, self-documenting helper methods, and constructor injection of dependencies.
