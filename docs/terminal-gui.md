# Terminal.Gui Reference Guide

This document provides a comprehensive reference for working with the Terminal.Gui library in the AzStore project.

## Overview

**Terminal.Gui** is a cross-platform, .NET-based framework for building modern terminal/console user interfaces. It provides a rich set of views (controls) and layout capabilities to create full-screen, keyboard-driven applications that work consistently across Windows, macOS, and Linux.

### Why Terminal.Gui

AzStore uses Terminal.Gui to provide:
- Full-screen TUI experience replacing the ad-hoc REPL
- Pane-based layout with header/body/footer sections
- Consistent cross-platform keyboard navigation (Tab/Shift-Tab, VIM bindings)
- Rich theming with 24-bit RGB color support
- Focus management and accessibility features

## Version Information

**Current Version**: `2.0.0-alpha.*` (v2 Alpha)

Package reference in `AzStore.Terminal.csproj`:
```xml
<PackageReference Include="Terminal.Gui" Version="2.0.0-alpha.*" />
```

**Important**: AzStore uses the v2 Alpha API, which has significant differences from v1. Always verify that documentation and examples reference v2.

### Key v2 Improvements
- Enhanced keyboard handling with multi-layered key binding system
- Improved view hierarchy and layout engine
- Better driver architecture for cross-platform support
- 24-bit RGB (True Color) support
- More consistent event handling patterns

## Color System

Terminal.Gui v2 provides comprehensive color support with 24-bit RGB values and alpha channels.

### Color Struct

The `Terminal.Gui.Color` struct represents RGBA colors:

```csharp
// 24-bit RGB color with alpha channel
public Color(int r, int g, int b, int alpha = 255)

// Properties
public int R { get; }  // Red: 0-255
public int G { get; }  // Green: 0-255
public int B { get; }  // Blue: 0-255
public int A { get; }  // Alpha: 0-255 (0=transparent, 255=opaque)
```

### Alpha Channel Limitations

**Current Status (v2.0.0 Alpha)**: The alpha channel is not yet fully supported for transparency effects. Setting alpha values < 255 will be stored but may not render transparently.

**Pending Feature**: PR #4234 is tracking alpha/transparency support for future Terminal.Gui releases.

**Current Workaround**: To achieve transparent backgrounds, use the terminal's default background color via escape sequences (see Escape Sequences section).

### Color Parsing

Terminal.Gui supports multiple color formats:

#### 1. Hex Colors
```csharp
// Parse hex color manually (AzStore pattern)
private static Color ParseHexColor(string hex, int alpha)
{
    hex = hex.TrimStart('#');
    if (hex.Length != 6) return new Color(255, 255, 255, alpha);

    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
    int b = Convert.ToInt32(hex.Substring(4, 2), 16);

    return new Color(r, g, b, alpha);
}
```

#### 2. Named Colors
Terminal.Gui provides static color properties:
```csharp
Color.Black
Color.DarkBlue / Color.Blue
Color.DarkGreen / Color.Green
Color.DarkCyan / Color.Cyan
Color.DarkRed / Color.Red
Color.DarkMagenta / Color.Magenta
Color.DarkYellow / Color.Yellow
Color.Gray / Color.DarkGray
Color.White
```

#### 3. ConsoleColor Mapping
Map from `System.ConsoleColor` to Terminal.Gui colors (AzStore pattern):
```csharp
private static Color MapConsoleToTui(ConsoleColor color) => color switch
{
    ConsoleColor.Black => Color.Black,
    ConsoleColor.DarkBlue => Color.Blue,
    ConsoleColor.DarkGreen => Color.Green,
    // ... etc
    _ => Color.White
};
```

### Using Terminal Default Background

For transparent/default terminal backgrounds, use CSI escape sequences instead of the alpha channel:

```csharp
// CSI 49m sets the default/transparent background
// CSI 39m sets the default foreground
// See Escape Sequences section for details
```

## Key Classes and Concepts

### Application Lifecycle

**Initialization**:
```csharp
Application.Init();  // Initialize Terminal.Gui framework
```

**Running**:
```csharp
var top = new Toplevel { ColorScheme = colorScheme };
// Add views to top
Application.Run(top);  // Blocks until RequestStop() is called
```

**Shutdown**:
```csharp
Application.RequestStop();  // Request graceful shutdown
Application.Shutdown();     // Clean up and restore terminal
```

**Global Access**:
```csharp
Application.Top  // Access the top-level view
Application.KeyDown += OnApplicationKeyDown;  // Application-level key events
```

### Attribute (Foreground + Background Pair)

An `Attribute` combines a foreground and background color:

```csharp
var attr = new Attribute(foregroundColor, backgroundColor);

// Used for rendering text with specific colors
view.ColorScheme = new ColorScheme
{
    Normal = new Attribute(Color.White, Color.Black)
};
```

### ColorScheme

A `ColorScheme` defines color attributes for different view states:

```csharp
public class ColorScheme
{
    public Attribute Normal { get; set; }      // Default state
    public Attribute Focus { get; set; }       // When view has focus
    public Attribute HotNormal { get; set; }   // Hot keys (accelerators) unfocused
    public Attribute HotFocus { get; set; }    // Hot keys when focused
    public Attribute Disabled { get; set; }    // Disabled state
}
```

**Example** (from AzStore):
```csharp
public ColorScheme GetListColorScheme()
{
    var selBg = ParseColor(theme.SelectedItemColor, isBackground: false);
    var selFg = GetContrastingForeground(selBg);

    return new ColorScheme
    {
        Normal = ResolveTui(ThemeToken.ItemBlob),
        Focus = new Attribute(selFg, selBg),
        HotNormal = ResolveTui(ThemeToken.ItemContainer),
        HotFocus = new Attribute(selFg, selBg),
        Disabled = new Attribute(new Color(169, 169, 169, 255), backgroundColor)
    };
}
```

### Views and Layouts

**Base View Class**: All UI elements inherit from `Terminal.Gui.View`

**Common Properties**:
```csharp
view.X = 0;                    // Absolute or computed position
view.Y = 0;
view.Width = Dim.Fill();       // Dim for responsive layouts
view.Height = Dim.Percent(50); // Use percentages or fill
view.ColorScheme = colorScheme;
```

**Container Views**:
- `Toplevel` - Top-level view for Application.Run()
- `Window` - Window with border and title
- `FrameView` - Frame with title and border
- `TabView` - Tabbed interface

**Data Views**:
- `ListView` - Scrollable list
- `TreeView` - Hierarchical tree
- `TableView` - Tabular data
- `TextField` - Single-line text input
- `TextView` - Multi-line text

**Adding Views**:
```csharp
parentView.Add(childView);  // Add child to parent
```

### Drivers (Cross-Platform)

Terminal.Gui uses platform-specific drivers for rendering:

- **NetDriver** - Pure .NET implementation (cross-platform fallback)
- **CursesDriver** - ncurses-based (macOS/Linux, better performance)
- **WindowsDriver** - Windows Console API (Windows-specific features)

The driver is selected automatically based on the platform. Application code typically doesn't interact with drivers directly.

**Platform Detection**: Terminal.Gui handles platform differences transparently through the driver layer.

## Important Escape Sequences

Terminal.Gui uses ANSI escape sequences for advanced terminal control. While the framework handles most of this internally, understanding key sequences is useful for workarounds and debugging.

### CSI (Control Sequence Introducer)

CSI sequences start with `ESC [` (or `\x1b[`):

#### Foreground RGB Color
```
CSI 38;2;{r};{g};{b}m
Example: \x1b[38;2;255;0;0m  (red foreground)
```

#### Background RGB Color
```
CSI 48;2;{r};{g};{b}m
Example: \x1b[48;2;0;0;255m  (blue background)
```

#### Default Colors (Transparency Workaround)
```
CSI 49m  - Reset to default/transparent background
CSI 39m  - Reset to default foreground
```

**Important**: CSI 49m is the current workaround for transparent backgrounds until PR #4234 adds proper alpha support.

#### Reset All Attributes
```
CSI 0m   - Reset all colors and attributes
```

### Usage in AzStore

While AzStore primarily uses Terminal.Gui's high-level color APIs, escape sequences may be needed for:
- Achieving terminal default backgrounds (transparency)
- Debugging color issues
- Advanced rendering scenarios not yet supported by the framework

## Common Patterns in AzStore Codebase

### ThemeService Integration

The `ThemeService` bridges AzStore's theme configuration with Terminal.Gui's color system:

```csharp
// Resolve theme token to Terminal.Gui Attribute
public Attribute ResolveTui(ThemeToken token)
{
    var colorString = GetColorStringForToken(token);
    var fg = ParseColor(colorString, isBackground: false);
    var bg = ParseColor(theme.BackgroundColor, isBackground: true);
    return new Attribute(fg, bg);
}
```

### Parsing Hex Colors to Color Struct

```csharp
private static Color ParseHexColor(string hex, int alpha)
{
    hex = hex.TrimStart('#');

    if (hex.Length != 6)
        return new Color(255, 255, 255, alpha);

    int r = Convert.ToInt32(hex.Substring(0, 2), 16);
    int g = Convert.ToInt32(hex.Substring(2, 2), 16);
    int b = Convert.ToInt32(hex.Substring(4, 2), 16);

    return new Color(r, g, b, alpha);
}
```

### Applying Alpha to Backgrounds Only

AzStore applies alpha transparency to backgrounds while keeping foregrounds opaque:

```csharp
private Color ParseColor(string colorString, bool isBackground = false)
{
    var alpha = isBackground ? theme.Alpha : 255;
    // ... parse color with specified alpha
}
```

This pattern anticipates future alpha support while maintaining code clarity.

### Color Scheme Creation

```csharp
// Label with consistent colors across all states
public ColorScheme GetLabelColorScheme(ThemeToken token)
{
    return new ColorScheme
    {
        Normal = ResolveTui(token),
        Focus = ResolveTui(token),
        HotNormal = ResolveTui(token),
        HotFocus = ResolveTui(token),
        Disabled = new Attribute(Color.DarkGray, ResolveBackground())
    };
}
```

### Application Initialization Pattern

```csharp
try
{
    Application.Init();
    _isRunning = true;

    Application.KeyDown += OnApplicationKeyDown;  // Global key handling

    var baseScheme = _theme.GetLabelColorScheme(ThemeToken.Background);

    if (Application.Top != null)
        Application.Top.ColorScheme = baseScheme;

    var top = new Toplevel { ColorScheme = baseScheme };
    // ... add views

    Application.Run(top);
}
finally
{
    Application.Shutdown();
    _isRunning = false;
}
```

## Known Issues and Workarounds

### Alpha Channel Not Yet Supported

**Issue**: Setting alpha values < 255 on colors does not produce transparency effects.

**Status**: Tracked in PR #4234 for Terminal.Gui

**Workaround**: Use CSI 49m escape sequence to reset to terminal default background for transparency effects.

```csharp
// Current: Set alpha (stored but not rendered transparently)
var transparentBg = new Color(30, 30, 46, 128);  // Alpha=128

// Workaround: Use terminal default via escape sequences
// (Requires direct console write or custom rendering)
Console.Write("\x1b[49m");  // Default background
```

### True Color (24-bit) Support Requirements

**Requirement**: The terminal must support true color (24-bit RGB) for proper rendering of hex colors.

**Detection**: Terminal.Gui auto-detects color capabilities through the driver.

**Fallback**: On terminals without true color support, colors may be approximated to the nearest 8-bit or 16-color palette.

**Testing**: Test on target platforms to verify color rendering:
- Windows Terminal (supports true color)
- iTerm2/Terminal.app on macOS (support true color)
- Most modern Linux terminals (gnome-terminal, konsole, etc.)

### Focus Traversal with Tab/Shift-Tab

**Pattern**: Handle focus traversal at the Application level for predictable tab order:

```csharp
private void OnApplicationKeyDown(object? sender, Key e)
{
    if (e == Key.Tab || e == Key.Tab.WithShift)
    {
        if (_layoutRoot.HandleFocusTraversal(e))
        {
            e.Handled = true;  // Prevent default handling
        }
    }
}
```

**Why**: Application-level handling ensures tab order works consistently across complex pane layouts.

## Useful Resources

### Official Documentation
- **Terminal.Gui v2 Documentation**: https://gui-cs.github.io/Terminal.Gui/docs/index.html
  - Getting Started guide
  - Deep dives on keyboard handling, views, layout
  - API reference
- **GitHub Repository**: https://github.com/gui-cs/Terminal.Gui
  - Source code and examples
  - Issue tracking (including PR #4234 for alpha support)
  - Community discussions

### Key Documentation Pages
- **Keyboard Handling**: https://gui-cs.github.io/Terminal.Gui/docs/keyboard.html
  - Key bindings, events, scopes
  - Multi-character sequences
  - Processing flow (Before/During/After pattern)
- **Views**: https://gui-cs.github.io/Terminal.Gui/docs/views.html
  - View hierarchy and lifecycle
  - Built-in controls
  - Composition patterns
- **Layout**: Position and sizing with Pos/Dim
- **Drivers**: Cross-platform console architecture

### API Discovery Workflow
1. **Read Documentation** for concepts and patterns
2. **Use Sherlock MCP** to inspect exact APIs (types, methods, properties)
3. **Check GitHub** for examples and issue discussions

### Terminal.Gui in AzStore Context
- **Migration from v1**: AzStore is built on v2 from the start
- **Integration Points**: `TerminalGuiUI`, `ThemeService`, pane views
- **Key Bindings**: VIM-like navigation layered on Terminal.Gui's event system
- **Cross-Platform**: Tested on Windows, macOS, Linux via CI

## Quick Reference

### Initialization & Lifecycle
```csharp
Application.Init();              // Initialize
Application.Run(toplevel);       // Run (blocking)
Application.RequestStop();       // Request stop
Application.Shutdown();          // Clean up
```

### Colors & Schemes
```csharp
// Create color
var color = new Color(r, g, b, alpha);

// Create attribute
var attr = new Attribute(fgColor, bgColor);

// Create color scheme
var scheme = new ColorScheme
{
    Normal = attr,
    Focus = focusAttr,
    // ...
};

// Apply to view
view.ColorScheme = scheme;
```

### Layouts
```csharp
// Position
view.X = Pos.At(5);              // Absolute
view.Y = Pos.Center();           // Centered

// Size
view.Width = Dim.Fill();         // Fill available space
view.Height = Dim.Percent(50);   // 50% of parent
```

### Key Events
```csharp
// Application-level
Application.KeyDown += OnKeyDown;

// View-level
view.KeyDown += OnViewKeyDown;
```

### Common Gotchas
- Always call `Application.Shutdown()` to restore terminal state
- Alpha channel not yet supported (use CSI 49m for transparency)
- Terminal.Gui v2 API differs significantly from v1
- Test on all target platforms for color rendering
- Use Sherlock MCP to verify exact API signatures before coding

---

**Last Updated**: 2025-09-30
**Terminal.Gui Version**: 2.0.0-alpha.*
**Project**: AzStore - Azure Blob Storage Terminal Client
