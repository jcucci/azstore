# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

azstore is a .NET 9 terminal application that provides a command-line REPL interface for interacting with Azure Blob Storage. The application allows cloud engineers, developers, and operations personnel to authenticate, browse containers and blobs, and download files using a keyboard-driven workflow with VIM-like keybindings.

## Key Requirements from PRD

- Terminal-based REPL for Azure Blob Storage interaction
- Session-based workflow with configurable local directory structure
- VIM-like navigation keybindings (j/k for up/down, l/Enter to enter, h/Backspace to go back)
- Azure CLI authentication integration
- File download capability with mirrored directory structure
- Built-in commands prefixed with colon (:ls, :help, :exit, :q)
- Cross-platform compatibility (Windows, macOS, Linux)
- Configuration file support with customizable settings

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
- **Testing**: xUnit with NSubstitute for mocking, comprehensive command system tests
- **Development**: VS Code integration with comprehensive debugging support

## Configuration Management

The application supports hierarchical configuration:

1. **appsettings.json**: Base application settings
2. **TOML config**: User-specific settings at:
   - Windows: `%APPDATA%\azstore\azstore.toml`
   - macOS/Linux: `~/.config/azstore/azstore.toml`
3. **Environment Variables**: Prefixed with `AZSTORE_`

Key configuration areas:
- Logging levels and output targets
- Theme and color customization
- Key binding mappings
- File conflict resolution behavior
- Session directory management

## Command System Architecture

The application uses an extensible command pattern with dependency injection for maximum testability and maintainability.

### Core Components
- **ICommand Interface**: Defines command contract with `Name`, `Aliases`, `Description`, and `ExecuteAsync`
- **CommandRegistry**: Service that discovers and provides command lookup functionality
- **CommandResult**: Standardized result type with success status, messages, and exit flags
- **Built-in Commands**: ExitCommand, HelpCommand, ListCommand

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

## Development Practices

### Testing Strategy
- **Test Architecture**: Fixture and Assertions pattern for clean test organization
- **Mocking**: NSubstitute for dependency substitution
- **Coverage**: Basic unit tests for all major components
- **Naming**: Avoid behavioral naming in assertions (e.g., `DefaultsSet` vs `ShouldHaveDefaults`)

### Code Standards
- **Dependency Injection**: Constructor injection with ILogger<T> pattern
- **Async/Await**: Proper cancellation token propagation
- **Error Handling**: Comprehensive exception handling with graceful degradation
- **Cross-Platform**: Platform-specific path handling for Windows/macOS/Linux
- **Modern C# Features**: Use C# 12 collection expressions `[]` instead of `new[]` for arrays and simple collections
- **Testing with xUnit**: Be careful with collection expressions in `Assert.Equal()` - may cause ambiguity between array and span overloads

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
- **Comprehensive test coverage (52+ tests across all layers)**
- **Modern C# syntax**: Collection expressions used throughout for cleaner array/collection initialization

### 🚧 In Development
- Azure Blob Storage authentication
- Container and blob enumeration
- File download functionality
- VIM-like keyboard navigation
- Session management

### 📋 Planned Features
- Azure CLI authentication integration
- Interactive blob browser
- File conflict resolution
- Progress indication for downloads
- Command history and completion

## Development Notes

- The project follows Microsoft's recommended practices for .NET hosted services
- All logging uses structured logging with correlation IDs
- Configuration changes are hot-reloaded when possible
- **Command system uses dependency injection for extensibility and testability**
- **New commands can be added by implementing ICommand and registering in DI**
- **Command registry automatically discovers and registers all ICommand implementations**
- The REPL engine delegates command execution to the command registry
- Error messages are user-friendly with helpful suggestions
- **Comprehensive test coverage includes unit tests for all command system components**

## Recent Changes and Learnings

### Command System Improvements (Current Session)
- **CommandRegistry Refactoring**: Improved dependency injection architecture with lazy loading of commands
- **HelpCommand Enhancement**: Now uses ICommandRegistry instead of direct IServiceProvider access for better separation of concerns
- **Input Validation**: Added robust null/whitespace checking in CommandRegistry.FindCommand()
- **Case Sensitivity**: Removed unnecessary case conversion - commands are now case-sensitive by design
- **Test Coverage**: Extended test suite with edge cases for whitespace and null input handling

### Collection Expressions Migration
- **Modern Syntax**: Updated all array initializations to use C# 12 collection expressions `[]`
- **Files Updated**: ExitCommand, ListCommand, CommandRegistryFixture, HelpCommandTests
- **Testing Gotcha**: Collection expressions can cause `Assert.Equal()` ambiguity in xUnit - use explicit `new[]` syntax when needed
- **Build Success**: All 52 tests pass after modernization