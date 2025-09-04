# AzStore Theming Guide

This document defines the architecture, tokens, and usage patterns for theming the AzStore terminal UI so current features look consistent and future features can plug in without hard-coded colors.

## Goals

- Consistent, configurable colors across all UI outputs (REPL, lists, prompts, progress).
- No hard-coded colors in feature code — resolve from theme tokens.
- Cross-platform Console and Terminal.Gui support with graceful fallbacks.
- Backwards-compatible with existing `ThemeSettings` and TOML config.

## Architecture Overview

- Theme model exposes semantic tokens (Prompt, Status, Error, Selection, Title, etc.).
- A `IThemeService` resolves tokens to runtime attributes:
  - Console: `ConsoleColor` foreground (and optional background in future).
  - Terminal.Gui: `Attribute`/`ColorScheme` mapping for views.
- Config drives theme selection and color values (TOML). Defaults are sane, but users can override.
- REPL, navigation, and prompts use the service to render text instead of setting colors directly.

## Theme Tokens (initial set)

- Prompt: command prompt text.
- Status: general status/info banners.
- Error: errors and warnings.
- Selection: highlighted/selected list items.
- Title: application title/banner.
- Breadcrumb: path display in prompt/header.
- Item.Container: container name in lists.
- Item.Blob: blob name in lists.
- Pager.Info: page/continuation indicators.
- Input: user input echoing (command mode).

Reserve for upcoming features:
- Success, Warning, Disabled, Accent, Progress.

## Model and Service (planned)

- Extend `ThemeSettings` to a richer palette (no rename yet to avoid breaking changes). Add properties aligned with tokens above. Keep existing properties working as aliases:
  - `PromptColor` -> Prompt
  - `StatusMessageColor` -> Status
  - `SelectedItemColor` -> Selection
  - Add `ErrorColor` -> Error
- Add `ThemePalette` object to group token values and future background/style flags.
- Add `IThemeService`:
  - `ConsoleColor ResolveForeground(ThemeToken token)`
  - `void Write(string text, ThemeToken token)` convenience
  - `Attribute ResolveTui(ThemeToken token)` (Terminal.Gui)
  - `ColorScheme GetSchemeFor(ViewKind view)` for TUI surfaces (later)
  - Hot-reload: reads `IOptionsMonitor<AzStoreSettings>` and reacts to changes.

## Integration Points (current code)

- REPL (`ReplEngine`): replace direct `WriteColored` calls with `IThemeService.Write(text, Token)` for Prompt, Status, Error, Info.
- Navigation views (`INavigationEngine.RenderCurrentView()`): use Selection and item tokens for list rendering and cursor/highlight.
- Confirmation and conflict prompts (`TerminalConfirmation`): inject or statically call theme service for emphasized choices and error text.
- Selection UIs (`ConsoleAccountSelectionService`): remove hard-coded Cyan/Red; use Status/Error/Selection tokens.
- Utilities that print progress or banners: map to Status/Title.

## Configuration (TOML)

Current keys (supported today):
- `AzStore.theme.prompt_color`
- `AzStore.theme.status_message_color`
- `AzStore.theme.selection_color`

Planned keys (added in Phase 5):
- `AzStore.theme.error_color`
- `AzStore.theme.breadcrumb_color`
- `AzStore.theme.container_color`
- `AzStore.theme.blob_color`
- `AzStore.theme.title_color`
- `AzStore.theme.pager_info_color`
- `AzStore.theme.input_color`

Values are case-insensitive console color names. Terminal.Gui attributes will map from these.

## Defaults

- Prompt: Green
- Status: Cyan
- Error: Red
- Selection: Yellow
- Title: White
- Breadcrumb: Gray
- Item.Container: Blue
- Item.Blob: White
- Pager.Info: DarkGray
- Input: White

## Testing Guidelines

- Unit-test the resolver: unknown token -> fallback; invalid color strings -> default.
- REPL tests assert that token usage is correct (e.g., Prompt uses Prompt token, Error uses Error token).
- Ensure no direct `Console.ForegroundColor = ...` remains in production paths (allowable only inside the theme service).

## Migration Plan

1) Introduce `IThemeService` and wire into DI without removing existing `WriteColored` helpers.
2) Update REPL, selection, and prompts to call the service; keep helpers delegating to the service.
3) Expand `ThemeSettings` with new properties; map old names to new tokens for backwards compatibility.
4) Update docs and config samples; add unit tests.
5) Optional: add Terminal.Gui `ColorScheme` mapping for complex dialogs.

## Usage Examples (future-facing)

```csharp
// REPL prompt
_theme.Write(":", ThemeToken.Prompt);

// Status message
_theme.Write("Connected to account", ThemeToken.Status);

// Error message
_theme.Write("Access denied", ThemeToken.Error);
```

```csharp
// Terminal.Gui attribute for a list view
listView.ColorScheme = _theme.GetSchemeFor(ViewKind.List);
```

## Contributor Rules of Thumb

- Do not hard-code colors. Always use theme tokens via `IThemeService`.
- If a new UI surface needs a color, propose a new token in this file before coding.
- Keep one type per file for all new theme-related types (tokens enum, service interface, palette model).

