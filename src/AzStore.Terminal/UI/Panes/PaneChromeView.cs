using System;
using AzStore.Terminal.Theming;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Tui = global::Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public sealed class PaneChromeView : View
{
    private readonly PaneViewBase _content;
    private readonly ColorScheme _normalScheme;
    private readonly ColorScheme _focusScheme;
    private readonly ILogger<PaneChromeView>? _logger;

    public PaneChromeView(PaneViewBase content, IThemeService theme, ILogger<PaneChromeView>? logger = null)
    {
        _content = content;
        _logger = logger;

        _logger?.LogInformation("PaneChromeView created for pane: {Title}", content.Title);

        CanFocus = true;
        TabStop = TabBehavior.TabStop;
        ShadowStyle = ShadowStyle.None;
        Width = Dim.Fill();
        Height = Dim.Fill();

        var border = Border;
        if (border is not null)
        {
            border.Visible = true;
            border.LineStyle = LineStyle.Rounded;
            border.Thickness = new Thickness(1);
            border.Settings = BorderSettings.Title;
        }

        _normalScheme = CreateNormalScheme(theme);
        _focusScheme = CreateFocusScheme(theme);
        ApplyScheme(_normalScheme);

        Title = _content.Title;

        _content.X = 1;
        _content.Y = 1;
        _content.Width = Dim.Fill(2);
        _content.Height = Dim.Fill(2);

        base.Add(_content);

        _content.TitleChanged += OnContentTitleChanged;
        _content.HasFocusChanged += OnContentHasFocusChanged;
        HasFocusChanged += OnChromeFocusChanged;
    }

    public PaneViewBase Content => _content;

    public void SetActiveState(bool isActive)
    {
        _logger?.LogDebug("SetActiveState for '{Title}': isActive={IsActive}", Title, isActive);
        ApplyScheme(isActive ? _focusScheme : _normalScheme);
    }

    public override void OnAdded(SuperViewChangedEventArgs e)
    {
        _logger?.LogInformation("PaneChromeView '{Title}' added to view hierarchy", Title);
        base.OnAdded(e);
        UpdateChrome();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _content.TitleChanged -= OnContentTitleChanged;
            _content.HasFocusChanged -= OnContentHasFocusChanged;
            HasFocusChanged -= OnChromeFocusChanged;
        }

        base.Dispose(disposing);
    }

    private void OnContentTitleChanged(object? sender, Tui.EventArgs<string> e)
    {
        Title = e.CurrentValue;
        SetNeedsDraw();
    }

    private void OnContentHasFocusChanged(object? sender, HasFocusEventArgs e)
    {
        _logger?.LogDebug("PaneChromeView '{Title}': Content.HasFocusChanged event fired, NewValue={NewValue}", Title, e.NewValue);
        UpdateChrome();
    }

    private void OnChromeFocusChanged(object? sender, HasFocusEventArgs e)
    {
        _logger?.LogDebug("PaneChromeView '{Title}': Chrome.HasFocusChanged event fired, NewValue={NewValue}", Title, e.NewValue);
        UpdateChrome();
    }

    private void UpdateChrome()
    {
        var hasFocus = HasFocus;
        var contentHasFocus = _content.HasFocus;
        var mostFocused = _content.MostFocused;
        var isActive = hasFocus || contentHasFocus || mostFocused is not null;

        _logger?.LogDebug(
            "UpdateChrome for '{Title}': HasFocus={HasFocus}, Content.HasFocus={ContentHasFocus}, Content.MostFocused={MostFocused}, IsActive={IsActive}",
            Title, hasFocus, contentHasFocus, mostFocused?.GetType().Name ?? "null", isActive);

        ApplyScheme(isActive ? _focusScheme : _normalScheme);
    }

    private void ApplyScheme(ColorScheme scheme)
    {
        var schemeName = ReferenceEquals(scheme, _focusScheme) ? "Focus" : "Normal";
        _logger?.LogDebug(
            "ApplyScheme for '{Title}': Applying {Scheme} scheme (Normal={Normal})",
            Title, schemeName, scheme.Normal);

        ColorScheme = scheme;
        SetNeedsDraw();
    }

    private static ColorScheme CreateNormalScheme(IThemeService theme)
    {
        var title = theme.ResolveTui(ThemeToken.Title);
        var disabled = new Tui.Attribute(Color.DarkGray, theme.ResolveBackground());

        return new ColorScheme
        {
            Normal = title,
            Focus = title,
            HotNormal = title,
            HotFocus = title,
            Disabled = disabled
        };
    }

    private static ColorScheme CreateFocusScheme(IThemeService theme)
    {
        var highlight = theme.ResolveTui(ThemeToken.Selection);
        var disabled = new Tui.Attribute(Color.DarkGray, theme.ResolveBackground());

        return new ColorScheme
        {
            Normal = highlight,
            Focus = highlight,
            HotNormal = highlight,
            HotFocus = highlight,
            Disabled = disabled
        };
    }
}
