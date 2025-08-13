# VS Code Development Setup for AzStore

This directory contains VS Code configuration files to enable easy development, debugging, and testing of the AzStore CLI application.

## Quick Start

### Running the Application

1. **Using VS Code Command Palette** (Ctrl+Shift+P):
   - Type "Tasks: Run Task"
   - Select `run-cli` to build and run the application
   - Select `run-cli-verbose` to run with debug logging enabled

2. **Using VS Code Terminal**:
   - Open integrated terminal (Ctrl+`)
   - Run: `dotnet run --project src/AzStore.CLI/AzStore.CLI.csproj`

3. **Using Debug Mode**:
   - Press F5 or go to Run and Debug (Ctrl+Shift+D)
   - Select "Launch AzStore CLI" configuration
   - Set breakpoints and debug as needed

## Available Configurations

### Launch Configurations (F5 debugging)

- **Launch AzStore CLI**: Standard debugging with Development environment
- **Launch AzStore CLI (Production Mode)**: Run in Production environment
- **Launch AzStore CLI (Verbose Logging)**: Debug logging enabled for troubleshooting
- **Launch AzStore CLI (External Terminal)**: Run in external terminal window
- **Attach to AzStore CLI Process**: Attach debugger to running process

### Tasks (Ctrl+Shift+P → "Tasks: Run Task")

- **build**: Build the entire solution
- **run-cli**: Build and run the CLI application (default task - Ctrl+Shift+P)
- **run-cli-verbose**: Run with verbose debug logging
- **watch**: Build and run with file watching (auto-restart on changes)
- **publish**: Publish the CLI for distribution
- **test**: Run all unit tests
- **clean**: Clean build artifacts
- **restore**: Restore NuGet packages

## Keyboard Shortcuts

- **F5**: Start debugging (Launch AzStore CLI)
- **Ctrl+F5**: Run without debugging
- **Shift+F5**: Stop debugging
- **Ctrl+Shift+P**: Command palette (access tasks)
- **Ctrl+Shift+`**: Create new terminal
- **Ctrl+`**: Toggle terminal panel

## Environment Variables

The configurations support several environment variables for testing:

- `DOTNET_ENVIRONMENT`: Set to "Development" or "Production"
- `AZSTORE_Logging__LogLevel`: Override log level ("Debug", "Information", "Warning", "Error")
- `AZSTORE_Logging__EnableConsoleLogging`: Enable/disable console logging ("true"/"false")
- `AZSTORE_Logging__EnableFileLogging`: Enable/disable file logging ("true"/"false")

## Terminal Integration

The application is designed to run in VS Code's integrated terminal with full interactive support:

- REPL commands work properly (`:help`, `:exit`, `:ls`)
- Console colors and formatting are preserved
- Logging output appears alongside user interaction
- Easy to stop with Ctrl+C

## Debugging Tips

1. **Set Breakpoints**: Click in the gutter next to line numbers
2. **Watch Variables**: Use the Variables panel or add to Watch panel
3. **Call Stack**: View the current call stack in Debug panel
4. **Debug Console**: Execute expressions while debugging

## File Watching

Use the "watch" task for development workflow:
1. Run the watch task
2. Make code changes
3. Application automatically rebuilds and restarts
4. Test your changes immediately

## Recommended Extensions

The workspace includes recommendations for useful extensions:
- C# DevKit (ms-dotnettools.csharp)
- .NET Runtime (ms-dotnettools.vscode-dotnet-runtime)
- Even Better TOML (tamasfe.even-better-toml) - for config files
- EditorConfig (editorconfig.editorconfig)

## Troubleshooting

### Build Issues
- Run "restore" task first if packages are missing
- Check that .NET 8 SDK is installed
- Verify solution path in settings.json

### Runtime Issues
- Check that all dependencies built successfully
- Verify environment variables are set correctly
- Use verbose logging configuration to debug issues

### Terminal Issues
- Use "External Terminal" configuration if integrated terminal has issues
- Ensure terminal supports ANSI color codes for proper formatting

## Configuration Files

- `launch.json`: Debug launch configurations
- `tasks.json`: Build and run tasks
- `settings.json`: Workspace-specific settings
- `extensions.json`: Recommended extensions