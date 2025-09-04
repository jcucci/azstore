# AzStore Themes

This folder contains ready-to-use theme snippets for AzStore. Each file is a TOML snippet with an `[AzStore.theme]` section that you can copy into your personal config.

## Quick Start

1) Pick a theme file from this directory (e.g., `catppuccin-mocha.toml`).
2) Open your AzStore config file:
   - Windows: `%APPDATA%\azstore\azstore.toml`
   - macOS: `~/.config/azstore/azstore.toml`
   - Linux: `~/.config/azstore/azstore.toml`
3) Copy the entire `[AzStore.theme]` block from the chosen theme file into your config, replacing or merging any existing `AzStore.theme` settings.
4) Save the file and start AzStore.

Notes:
- AzStore reads ConsoleColor names (case-insensitive). Terminal.Gui views map these colors to the closest available TUI colors.
- The `selection_color` is used as a background highlight in TUI lists with a contrasting foreground for readability.

## Available Keys

Supported keys under `[AzStore.theme]`:
- `prompt_color`
- `status_message_color`
- `error_color`
- `selection_color`
- `breadcrumb_color`
- `container_color`
- `blob_color`
- `title_color`
- `pager_info_color`
- `input_color`

See `docs/configuration-example.toml` for descriptions and defaults.

## Environment Variable Overrides (optional)

You can override specific colors via environment variables using .NET’s nested configuration syntax:
- `AzStore:Theme:PromptColor`
- `AzStore:Theme:StatusMessageColor`
- `AzStore:Theme:ErrorColor`
- `AzStore:Theme:SelectedItemColor`
- `AzStore:Theme:BreadcrumbColor`
- `AzStore:Theme:ContainerColor`
- `AzStore:Theme:BlobColor`
- `AzStore:Theme:TitleColor`
- `AzStore:Theme:PagerInfoColor`
- `AzStore:Theme:InputColor`

Examples:
- PowerShell: `$env:AzStore:Theme:PromptColor = 'Green'`
- Bash: `export AzStore:Theme:PromptColor=Green`

If your environment requires double underscores for nested keys:
- `export AzStore__Theme__PromptColor=Green`

## Catppuccin

Four Catppuccin flavors are provided as approximations of the palette using ConsoleColor names:
- `catppuccin-latte.toml` (light)
- `catppuccin-frappe.toml` (dark)
- `catppuccin-macchiato.toml` (dark)
- `catppuccin-mocha.toml` (dark)

You can use these as-is or tweak the color names to your taste.
