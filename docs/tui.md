# Terminal TUI Refactor Plan (Epic #63)

This document summarizes the Terminal.Gui refactor epic and its child issues for quick reference during implementation.

**Epic Goal**
- Replace the ad‑hoc REPL UI with a full‑screen, pane‑based TUI built on `Terminal.Gui`, integrated with the existing command system, explicit paging APIs, configuration, and theming.

**Epic Acceptance**
- App opens full‑screen and cleanly restores the terminal on exit.
- End‑to‑end flows work on Windows/macOS/Linux: select session → pick account/container → search → page → download → preview → re‑run commands.

---

## Architecture Overview
- **Framework**: `Terminal.Gui` hosting with full‑screen lifecycle and graceful shutdown hooked into `IHostApplicationLifetime`.
- **Composition**: Header (sessions, account, container, search), main body (results list + preview), footer (command editor + command history).
- **Navigation**: Tab/Shift‑Tab for focus traversal; VIM‑like key sequences preserved via existing `KeySequenceBuffer` where applicable.
- **Commands**: The bottom command editor integrates with the `ICommandProcessor`/`CommandRegistry` so colon‑prefixed commands (e.g., `:ls`, `:help`, `:q`) work unchanged.
- **Paging**: All lists use explicit paging over Azure SDK (`AsPages()`) via `PagedResult<T>` and `PageRequest` (no streaming). Continuation tokens persist in TUI state.
- **State & Events**: Central TUI state with pub/sub style eventing to keep panes synchronized (selection, paging, search query, active session/account/container, downloads).
- **Theming**: Map existing theme settings to `Terminal.Gui` color schemes; keep cross‑platform parity.
- **Config**: Honor hierarchical config (JSON → TOML → env) for keybindings, timeouts, paging sizes, and theme.
- **Downloads**: Integrate with `IDownloadActivity` for activity counts, progress, and exit prompts (`--force` bypass).

---

## Cross‑Cutting Requirements
- **Key Sequences**: Retain VIM‑like navigation (j/k/h/l, gg/G, dd) with multi‑char timeout. Map to `Terminal.Gui` key events where feasible; fall back to the existing buffer for sequences.
- **Accessibility**: Clear focus visuals, predictable tab order, and resilient behavior on small terminals.
- **Error Handling**: Non‑fatal UI toasts/status lines for recoverable errors; structured logging via Serilog.
- **Cancellation**: Honor `CancellationToken` in long operations (search, paging, download).
- **Tests**: xUnit unit tests for presenters/state/eventing; avoid GUI flakiness. Use `[Trait("Category", "Unit")]` and filter out integration in CI.

---

## Phased Delivery
- **M1: Hosting & Skeleton**
  - Bootstrap `Terminal.Gui` host, full‑screen lifecycle, basic layout + focus navigation.
- **M2: Header + Lists**
  - Sessions, account picker, container picker, search box, results list with paging + actions.
- **M3: Footer + Preview + Activity**
  - Command editor, command history, preview pane, download list/activity + exit behavior.

---

## Child Issues Summary

1) `#64` Bootstrap Terminal.Gui Host + Full‑Screen Lifecycle
- **Scope**: Initialize `Application.Run()`, full‑screen top‑level view, clean shutdown restoring terminal state.
- **Integrations**: Wire to hosted services; ensure `StopApplication()` flows close TUI.
- **Acceptance**: Launches full‑screen; Esc/:q cleanly exits; terminal restored across OSes.

2) `#65` Layout Skeleton + Focus Navigation (Tab/Shift‑Tab)
- **Scope**: Compose header/body/footer, panes sized with responsive constraints.
- **Navigation**: Tab/Shift‑Tab cycles predictable order; initial focus sensible (search or results).
- **Acceptance**: All panes reachable by tabbing; focus clearly indicated.

3) `#66` Command Editor Pane (Bottom) Integrated with Command System
- **Scope**: Bottom input accepts colon‑prefixed commands; sends to `ICommandProcessor`.
- **Behavior**: Trailing `!` → `--force` arg; history add on execute; status line feedback.
- **Acceptance**: `:help`, `:ls`, `:q`, `:q!` work as in REPL; errors surfaced non‑disruptively.

4) `#67` Command History Pane (Bottom‑Right) with Re‑Run/Load
- **Scope**: Scrollable list of previous commands; select to load into editor or re‑run.
- **Keys**: Up/Down to navigate; Enter to load; a dedicated key (e.g., r) to re‑run.
- **Acceptance**: Re‑run respects force flags; editor sync is reliable.

5) `#68` Session List (Header) with Select/Rename/Delete
- **Scope**: Header sessions dropdown/list with create/select/rename/delete.
- **Data**: Integrates with `ISessionManager` (persistence follows separate session work).
- **Acceptance**: Switching session updates dependent panes (account/container/results).

6) `#69` Storage Account Picker (Header) with Modal Selection
- **Scope**: Modal to list subscriptions/accounts; supports filtering and paging if needed.
- **Auth**: Uses existing Azure CLI auth service; error surfacing if unauthenticated.
- **Acceptance**: Selected account updates container picker and results context.

7) `#70` Container Picker (Header) with Paged Selection
- **Scope**: Paged container list using `PagedResult<Container>`; continuation tokens handled.
- **Acceptance**: Selection sets the container context; results pane refreshes accordingly.

8) `#71` Search Box (Header) for Prefix Queries
- **Scope**: Input for prefix search (e.g., path prefix); debounce and submit behavior.
- **Config**: Page size and defaults from settings; Enter triggers search.
- **Acceptance**: Query reflected in results; clear action resets.

9) `#72` Search Results List with Explicit Paging + Actions
- **Scope**: Center list shows paged results; next/prev page commands and jump keys (gg/G).
- **Actions**: Open preview, download (dd), copy name/path; multi‑select optional later.
- **Acceptance**: Smooth page transitions; cursor state maintained; actions invoke services.

10) `#73` Download List Pane + Download Activity Integration
- **Scope**: Side/bottom pane listing active/completed downloads; progress where available.
- **Exit**: Normal exit prompts when `IDownloadActivity.HasActiveDownloads`.
- **Acceptance**: Activity count accurate; `:q!` bypasses prompt; files saved with mirror paths.

11) `#74` Blob Preview Pane (Right) with Basic Viewers
- **Scope**: Right pane preview for text (UTF‑8), JSON, and small binary metadata.
- **Limits**: Size caps; show informative message for large/unpreviewable blobs.
- **Acceptance**: Selection in results updates preview; error resilient.

12) `#75` TUI State Synchronization + Eventing
- **Scope**: Central state record(s) and event bus to broadcast selection, paging, search, session/account/container changes.
- **Contracts**: Immutable updates; subscribers update views idempotently.
- **Acceptance**: No stale UI; switching context propagates predictably across panes.

---

## Key Design Decisions
- **Explicit Paging**: Always request a single page and return immediately; maintain continuation tokens in view state.
- **Command Parsing**: Continue using command options factories (e.g., `FromArgs`) for built‑ins; UI only orchestrates.
- **One Type Per File**: Apply repository standard rigorously to new TUI types (views, state, presenters, services).
- **Modern C#**: Use collection expressions `[]`, expression‑bodied members where readable.
- **Locking**: Prefer `System.Threading.Lock` for shared state sections.

---

## Testing & Validation
- **Unit Tests**: State reducers, eventing, and presenters; deterministic without live UI.
- **Integration (Opt‑in)**: Azure auth/account listing behind `[Trait("Category", "Integration")]`.
- **CI Command**: `dotnet test src/Azstore.sln --filter "Category!=Integration" -v minimal -nr:false -m:1`
- **Manual**: Verify end‑to‑end flows on Windows/macOS/Linux terminals; confirm terminal restore.

---

## Open Questions
- **Keybinding Unification**: Which actions remain bound to VIM sequences vs. `Terminal.Gui` accelerators?
- **Multi‑Select & Bulk Actions**: In scope for M3 or follow‑up?
- **Preview Enhancements**: Syntax highlighting, hex view, streaming previews—post‑MVP?

---

## Out of Scope (Epic)
- Full session persistence feature set (tracked separately; basic selection supported).
- Non‑Azure storage providers.
- Rich diff viewers or advanced editors.

---

## References
- Epic `#63`: Terminal TUI Redesign with Terminal.Gui
- Children: `#64`–`#75` (see summaries above)
- Existing standards: `CLAUDE.md` (configuration, paging, commands, testing)

