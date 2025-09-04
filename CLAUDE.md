# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

azstore is a .NET 9 terminal application that provides a command-line REPL interface for interacting with Azure Blob Storage. The application allows cloud engineers, developers, and operations personnel to authenticate, browse containers and blobs, and download files using a keyboard-driven workflow with VIM-like keybindings.

## Key Requirements

- Terminal-based REPL for Azure Blob Storage with VIM-like navigation (j/k/h/l keys, gg/G for jump navigation)
- Multi-character key sequence support with configurable timeout for true VIM-like experience
- Session-based workflow with Azure CLI authentication
- File download with mirrored directory structure and conflict resolution
- Cross-platform compatibility with TOML configuration support
- Extensible command system with colon-prefixed built-ins (:ls, :help, :exit, :q)

## Current Architecture

The project uses a layered architecture with the following components:

### Core Projects
- **AzStore.CLI**: Main executable with hosted services architecture, Serilog integration
- **AzStore.Configuration**: Settings management with TOML configuration support
- **AzStore.Core**: Business logic and Azure Blob Storage integration
- **AzStore.Terminal**: REPL engine with theme support and command processing

### Test Projects
- **AzStore.CLI.Tests**: Service registration and dependency injection tests
- **AzStore.Configuration.Tests**: Settings validation and configuration tests
- **AzStore.Core.Tests**: Business logic unit tests
- **AzStore.Terminal.Tests**: REPL functionality and theming tests

### Technical Stack
- **Target Framework**: .NET 9
- **Architecture**: Microsoft.Extensions.Hosting with hosted services
- **Command System**: Extensible command pattern with dependency injection
- **Logging**: Serilog with console and file sinks, structured logging
- **Configuration**: Microsoft.Extensions.Configuration with TOML support
- **Azure Integration**: Azure.Storage.Blobs SDK
- **Testing**: xUnit with standard assertions, NSubstitute for mocking
- **Development**: VS Code integration with comprehensive debugging support

## Configuration

Hierarchical configuration: appsettings.json → TOML config (`%APPDATA%\azstore\azstore.toml` or `~/.config/azstore/azstore.toml`) → `AZSTORE_*` environment variables. Covers logging, themes, key bindings, file conflicts, and session management.

- Terminal selection (multi-account picker):
  - `AzStore:Selection:enableFuzzySearch` (bool, default true)
  - `AzStore:Selection:maxVisibleItems` (int, default 15)
  - `AzStore:Selection:highlightMatches` (bool, default true)
  - `AzStore:Selection:pickerTimeoutMs` (int?, default null)

## Command System Architecture

The application uses an extensible command pattern with dependency injection for maximum testability and maintainability.

### Core Components
- **ICommand Interface**: Defines command contract with `Name`, `Aliases`, `Description`, and `ExecuteAsync`
- **CommandRegistry**: Service that discovers and provides command lookup functionality
- **CommandResult**: Standardized result type with success status, messages, and exit flags
- **Built-in Commands**: ExitCommand, HelpCommand, ListCommand
- **Interactive Selection**: `IAccountSelectionService` renders a non-destructive overlay picker for choosing items like storage accounts.

### Adding New Commands
1. Create a class implementing `ICommand` interface
2. Add constructor dependencies as needed (logger, services, etc.)
3. Register as `ICommand` in ServiceCollectionExtensions
4. Command is automatically discovered and available in REPL

### Example Command Implementation
```csharp
public class MyCommand : ICommand 
{
    public string Name => "mycommand";
    public string[] Aliases => ["mc"];  // Uses C# 12 collection expressions
    public string Description => "Does something useful";
    
    public MyCommand(ILogger<MyCommand> logger) { /* ... */ }
    
    public Task<CommandResult> ExecuteAsync(string[] args, CancellationToken ct)
    {
        // Implementation here
        return Task.FromResult(CommandResult.Ok("Success!"));
    }
}
```

### Exit & Cleanup
- Commands: `:exit`, `:q` for normal shutdown; `:exit!`, `:q!` for forced shutdown.
- Force parsing: The REPL strips `!` and appends `--force` to args; commands check for `--force` to bypass prompts.
- REPL exit: When a command returns `ShouldExit`, the REPL loop breaks and the hosted service triggers `StopApplication()`.
- Downloads: `IDownloadActivity` tracks active downloads; normal exit prompts if any are active, force exit skips confirmation.
- Sessions: `ExitCommand` attempts `ISessionManager.SaveSessionsAsync()` before exit (best effort).

### REPL Parsing Pattern
- Commands must start with `:`. The first token is the command name; remaining tokens are args.
- A trailing `!` on the command token denotes force mode and results in `--force` being appended to args.

### Download Activity
- Use `IDownloadActivity.Begin()` to mark the lifetime of a download; dispose to decrement.
- Check `HasActiveDownloads`/`ActiveCount` when deciding to prompt on exit or display status to the user.

## Development Practices

### Testing Strategy
- **Test Architecture**: Fixture and Assertions pattern for clean test organization
- **Mocking**: NSubstitute for dependency substitution
- **Assertions**: Standard xUnit assertions only (no FluentAssertions)
- **Coverage**: Comprehensive unit tests for all major components
- **Test Categories**: Unit tests use `[Trait("Category", "Unit")]`, Integration tests use `[Trait("Category", "Integration")]`
- **CI Filtering**: Use `--filter "Category!=Integration"` to exclude integration tests in CI builds where Azure CLI may not be available

### Code Standards
- **Dependency Injection**: Constructor injection with ILogger<T> pattern
- **Async/Await**: Proper cancellation token propagation
- **Error Handling**: Comprehensive exception handling with graceful degradation
- **Cross-Platform**: Platform-specific path handling for Windows/macOS/Linux
- **Modern C# Features**: Use C# 12 collection expressions `[]` instead of `new[]` for arrays and simple collections
- **Threading**: Use `System.Threading.Lock` instead of `object` for synchronization in .NET 9 for 25% performance improvement
- **Parameter Validation**: Rely on nullable reference types and compiler safety instead of manual guards - let Azure SDK handle parameter validation with meaningful error messages
- **Comments**: Only include inline comments when they explain non-obvious logic; remove comments that merely restate what code obviously does
- **Testing**: Use standard xUnit assertions (not FluentAssertions), avoid collection expression ambiguity in `Assert.Equal()`
- **File Organization**: **MANDATORY** - Put each class, record, struct, enum, and interface in its own code file for better maintainability and discoverability. **NEVER** place multiple types in the same file. This is a strict requirement and must be enforced during code reviews
- **Expression Bodies**: Use expression-bodied methods when simple and readable. For single-line expressions, use one line: `public int GetValue() => 42;`. For multi-line expressions, put the arrow at the end of the method signature line and indent the body on the next line: `public Result ComplexMethod() => expression;`

### VS Code Integration
Complete development environment setup:
- **Debug Configurations**: Multiple launch profiles (Development, Production, Verbose)
- **Build Tasks**: Automated build, test, watch, and publish tasks
- **Extensions**: Recommended extensions for .NET development
- **Settings**: Optimized workspace configuration

## Build and Runtime

### Commands
- `dotnet build src/Azstore.sln` - Build solution
- `dotnet test src/Azstore.sln` - Run all tests
- `dotnet run --project src/AzStore.CLI` - Start application

### Output Structure
- **Executable**: `src/AzStore.CLI/bin/Debug/net9.0/azstore.dll`
- **Cross-Platform**: Supports Windows, macOS, and Linux
- **Self-Contained**: All dependencies included in output

## Current Implementation Status

### ✅ Completed Features
- Hosted services architecture with graceful shutdown
- Comprehensive logging infrastructure with Serilog
- TOML-based configuration system
- **Extensible command system with dependency injection**
- **Command registry pattern for easy command addition**
- **REPL engine with pluggable command architecture**
- Built-in commands (:help, :exit/:q, :list/:ls)
- Theme support with configurable colors
- Cross-platform path handling
- Dependency injection container setup
- **Comprehensive test coverage (104+ tests across all layers)**
- **Modern C# syntax**: Collection expressions used throughout for cleaner array/collection initialization

### ✅ Foundation Phase Complete
- **Core domain models**: Session, Container, Blob, NavigationState, StorageItem with validation and serialization
- **Service interfaces**: IStorageService, ISessionManager, IAuthenticationService, IConfigurationService, IReplEngine, ICommandProcessor
- **Model tests**: 53 comprehensive unit tests with xUnit assertions for all models

### ✅ Authentication Phase Complete (Issue #6)
- **Azure CLI authentication service**: Full implementation using DefaultAzureCredential with AzureCliCredential only
- **Cross-platform support**: Works on Windows, macOS, and Linux
- **Comprehensive error handling**: Graceful handling of Azure CLI not installed/authenticated scenarios
- **Token management**: Automatic token refresh and caching with expiration handling
- **Subscription management**: Enumerate available subscriptions and storage accounts
- **Test coverage**: 28 unit tests + 8 integration tests with category traits for CI filtering
- **Dependencies**: Azure.Identity (1.12.1), Azure.ResourceManager (1.13.0), Azure.ResourceManager.Storage (1.3.0)

### ✅ Paging Implementation Complete
- **Azure Storage Paging**: Replaced `IAsyncEnumerable` methods with explicit paging using `PagedResult<T>` and `PageRequest`
- **Azure SDK Integration**: Uses `AsPages()` method for efficient server-side pagination with continuation tokens
- **Breaking Change**: Removed streaming enumeration in favor of explicit page-by-page access for better REPL control
- **Memory Efficiency**: Only loads one page at a time instead of streaming all results
- **Test Coverage**: Updated all 156+ tests to use new paged API, added comprehensive paging tests
- **REPL-Optimized**: Perfect for interactive scenarios where users navigate through containers/blobs page by page

### ✅ Multi-Character Key Bindings Complete (Issue #40)
- **KeySequenceBuffer**: Implements buffering system for multi-character key sequences like 'gg' and 'dd'
- **Timeout Management**: Configurable timeout (default 1000ms) to distinguish intentional sequences from accidental key presses
- **Prefix Matching**: Handles cases where one binding is a prefix of another (e.g., 'g' and 'gg')
- **VIM-like Navigation**: True VIM-style bindings with 'gg' (jump to top), 'G' (jump to bottom), 'dd' (download)
- **Backward Compatibility**: Existing single-character bindings continue to work unchanged
- **Configuration Support**: Multi-character sequences and timeout configurable via TOML settings
- **Comprehensive Testing**: 15+ unit tests covering timeout behavior, sequence completion, and prefix conflicts

### 🚧 In Development
- Session persistence
- Interactive blob browser with paging navigation

### 📋 Next Phase
- Interactive blob browser with VIM navigation
- File conflict resolution
- Progress indication for downloads

## Development Notes

- Follows Microsoft .NET hosted services patterns with structured logging and hot-reload configuration
- Extensible command system: implement `ICommand`, register in DI, automatic discovery via `CommandRegistry`
- Interface-driven architecture with comprehensive DI container setup
- **Test coverage**: 156+ tests including comprehensive model validation, edge cases, and paging functionality

## Theming Guidance

- **No hard-coded colors**: Use a theme abstraction instead of `Console.ForegroundColor` in feature code. Routing all color writes through the theme ensures consistency and configurability.
- **Tokens, not literals**: Code should request a semantic token (e.g., Prompt, Status, Error, Selection) and let the theme resolve the color.
- **Service-first**: Prefer an `IThemeService` to resolve tokens and write colored output. Until the service lands, use the REPL helpers (`WritePrompt`, `WriteStatus`, `WriteError`) and avoid direct color changes.
- **Extend `ThemeSettings`**: Add new properties for tokens rather than introducing ad-hoc config flags. Keep old names mapped for backwards compatibility.
- **Terminal.Gui mapping**: For TUI overlays, map tokens to `ColorScheme` via the theme service instead of setting attributes inline.
- **Add tokens when needed**: If a new UI surface needs color, propose a new token and update docs before coding.
- **Docs**: See `docs/THEMING.md` for architecture, tokens, defaults, and examples.

## Lessons Learned

### Azure SDK Pagination Best Practices
- **Use AsPages() for Explicit Control**: `GetBlobsAsync().AsPages(continuationToken, pageSize)` provides better control than `IAsyncEnumerable` for interactive scenarios
- **Single Page Processing**: When implementing pagination, process only the first page and return immediately - avoid `await foreach` loops that process all pages
- **Continuation Token Management**: Azure SDK handles opaque continuation tokens; store and pass them between requests without modification
- **Page Size Limits**: Azure Storage supports up to 5000 items per page; default to smaller sizes (100) for better UX in terminal applications

### REPL Architecture Considerations
- **Explicit Paging vs Streaming**: For interactive applications, explicit paging (`PagedResult<T>`) is superior to streaming (`IAsyncEnumerable<T>`)
- **Memory Management**: Paging prevents memory issues when dealing with storage accounts containing thousands of containers/blobs
- **User Experience**: Page-by-page navigation aligns better with VIM-like keybindings and terminal constraints

### Breaking Changes Management
- **Interface Evolution**: When replacing method signatures, update all dependent code simultaneously to maintain compilation
- **Test Migration**: Convert streaming tests to paged tests by replacing `await foreach` with direct method calls
- **Documentation Updates**: Keep CLAUDE.md current with architectural decisions and implementation status

## Recent Implementation (GitHub Issue #4)

### Foundation Phase Complete
- **Domain Models**: Session, StorageItem, Container, Blob, NavigationState as C# 12 records with validation/serialization
- **Service Interfaces**: IStorageService, ISessionManager, IAuthenticationService, IConfigurationService, IReplEngine, ICommandProcessor with full XML docs
- **Architecture**: Interface extraction, improved DI patterns, command registry with lazy loading
- **Testing**: Migrated from FluentAssertions to standard xUnit assertions, 104+ tests with comprehensive edge case coverage
- **Quality**: Modern C# 12 syntax, proper async/await patterns, cross-platform compatibility
### Account Selection (Issue #55)
- Non-destructive console overlay for multi-account scenarios using `IAccountSelectionService`.
- Fuzzy search with lightweight subsequence/substring matcher (`SimpleFuzzyMatcher`).
- VIM-like navigation supported: `j/k`, `gg/G`, arrows, PageUp/PageDown, Enter/Esc).
- Selection is used when creating a session if multiple accounts are available.
