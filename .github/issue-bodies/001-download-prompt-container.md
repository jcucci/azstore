Summary
- Make the download command interactive when the container is not specified. Reuse the same selection UI/UX used for choosing storage accounts to present available containers and let the user pick one.

Motivation
- Current behavior requires specifying the container, which breaks flow when users don’t remember exact names.
- Aligns with the REPL-first design, enabling discovery via keyboard without leaving the terminal.
- Consistent with existing account selection/chooser UX and VIM-like navigation.

Desired Behavior
- When the user runs the download command without a container argument, prompt the user to choose a container from the current storage account using the same selection component used for storage account selection.
- Continue to support the non-interactive form where the container is provided.
- Respect paging and VIM-like navigation (j/k, gg/G) within the container list.

User Stories
- As a user, I can run `:download` (or `:download <pattern>`) without a container and be prompted to select one.
- As a user, I can navigate the container list using existing VIM-like bindings and select a container quickly.
- As a user, I can cancel from the container picker to abort the command gracefully.

UX/REPL Flow
1) User enters `:download [<blobNameOrPattern>]` with no container specified.
2) REPL opens the container picker using the same UI/UX and keybindings as the storage account picker (multi-page, VIM-like nav, timeouts preserved).
3) On selection, the command proceeds using the chosen container.
4) If the user cancels, return a friendly message and `CommandResult` with success=false but no error.

Scope & Technical Notes
- Command parsing: Update the download command’s options factory (e.g., `DownloadCommandOptions.FromArgs`) to make the container parameter optional. Preserve existing flags. Prefer optionals/nullable over manual guard code per repo standards.
- Interactive selection: Reuse the existing selection component/service used for storage account picking. It should already support paging via `PagedResult<T>` and VIM-like navigation through `KeySequenceBuffer` behavior.
- Data source: List containers with explicit paging via Azure SDK (`AsPages()`), passing continuation tokens captured in `PagedResult<T>`. Default to a page size consistent with terminal UX (e.g., 100) and existing settings.
- Cancellation: Ensure `CancellationToken` is honored when loading pages and while awaiting user input.
- Logging: Use structured logging at debug/verbose levels; avoid noisy info logs.
- Config: No new required settings. Respect existing paging and keybinding configuration.
- Files & Style: Keep one type per file. If introducing `DownloadCommandOptions`, place it in its own file. Use C# 12 collection expressions in tests and code.
- DI: No new services required; prefer constructor injection of existing services (e.g., `IStorageService`, `IReplEngine`, selection/picker service) per current patterns.

Acceptance Criteria
- Running the download command without a container shows a container picker identical in behavior to the storage account picker (VIM-like bindings, multi-character sequences, timeout handling).
- Selecting a container proceeds with the command as if the container had been provided explicitly.
- Cancelling the picker cleanly aborts the command with an informative message; REPL remains stable.
- Unit tests cover: options parsing with/without container; interactive branch invocation; cancellation behavior; and propagation of the selected container into the command’s execution.
- Integration test (Category=Integration) covers a basic interactive flow against a test account when available; CI excludes it by default.

Testing
- Unit: xUnit with NSubstitute; assert options factory defaults when container is omitted; verify that the command calls into the picker and proceeds with the selected container; verify cancellation path.
- Paging: Include a test that simulates multiple container pages using `PagedResult<T>` and continuity of navigation state.
- CI: Tag integration tests with `[Trait("Category", "Integration")]` so they can be filtered out in CI via `--filter "Category!=Integration"`.

Out of Scope
- Changes to how blobs are filtered or downloaded (handled in separate issue about prompting when blob is omitted).
- New keybindings or theme changes.

Notes
- Follow repository standards in CLAUDE.md for paging, testing, and DI patterns. Use explicit paging and avoid `await foreach` across all results.
