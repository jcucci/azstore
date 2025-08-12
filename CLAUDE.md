# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

azstore is a .NET terminal application that provides a command-line REPL interface for interacting with Azure Blob Storage. The application allows cloud engineers, developers, and operations personnel to authenticate, browse containers and blobs, and download files using a keyboard-driven workflow with VIM-like keybindings.

## Key Requirements from PRD

- Terminal-based REPL for Azure Blob Storage interaction
- Session-based workflow with configurable local directory structure
- VIM-like navigation keybindings (j/k for up/down, l/Enter to enter, h/Backspace to go back)
- Azure CLI authentication integration
- File download capability with mirrored directory structure
- Built-in commands prefixed with colon (:ls, :help, :exit, :q)
- Cross-platform compatibility (Windows, macOS, Linux)
- Configuration file support with customizable settings

## Architecture Notes

This is an early-stage project with only a Visual Studio solution file present. The codebase structure is not yet established, but based on the PRD:

- Target framework: .NET 8 or newer
- Console application architecture
- REPL interface implementation needed
- Azure SDK integration for blob storage operations
- Configuration management system
- Session management with local file operations

## Development Status

The project is in initial stages with only:
- Visual Studio solution file (src/Azstore.sln)
- Product Requirements Document (docs/prd.md)
- Basic README and MIT license

No actual .NET project files or source code exist yet - this is a greenfield project ready for initial development.