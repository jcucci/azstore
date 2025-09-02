# azstore
Terminal REPL for browsing and downloading Azure Storage blobs

Quick start
- Configure sessions root via TOML or env:
  - TOML: `~/.config/azstore/azstore.toml` (Linux/macOS) or `%APPDATA%\azstore\azstore.toml` (Windows)
  - Key: `AzStore.SessionsDirectory = "/path/to/sessions"`
  - Env: `AZSTORE_AzStore__SessionsDirectory=/path/to/sessions`
- Create a session: `:session create <name> [storage-account]`
  - Session directory is `{AzStore:SessionsDirectory}/{name}`
- List sessions: `:session list`
- Switch session: `:session switch <name>`

Configuration
- Sources (in order): `appsettings.json` → TOML file → `AZSTORE_` env vars → CLI flags
- Default file conflict behavior for downloads: `AzStore.OnFileConflict` (Overwrite | Skip | Rename)
- Keybindings, theme, and logging are configurable under the `AzStore` section
