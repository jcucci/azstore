using System;
using AzStore.Terminal.Theming;
using Terminal.Gui;
using Tui = global::Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public sealed class PaneChromeView : View
{
    private readonly PaneViewBase _content;
    private readonly ColorScheme _normalScheme;
    private readonly ColorScheme _focusScheme;
    private bool _navigationSubscribed;

    public PaneChromeView(PaneViewBase content, IThemeService theme)
    {
        _content = content;

        CanFocus = false;
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

        SubscribeNavigation();
    }

    public PaneViewBase Content => _content;

    public override void OnAdded(SuperViewChangedEventArgs e)
    {
        base.OnAdded(e);
        SubscribeNavigation();
        UpdateChrome();
    }

    public override void OnRemoved(SuperViewChangedEventArgs e)
    {
        base.OnRemoved(e);
        UnsubscribeNavigation();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _content.TitleChanged -= OnContentTitleChanged;
            _content.HasFocusChanged -= OnContentHasFocusChanged;
            UnsubscribeNavigation();
        }

        base.Dispose(disposing);
    }

    private void OnContentTitleChanged(object? sender, Tui.EventArgs<string> e)
    {
        Title = e.CurrentValue;
        SetNeedsDraw();
    }

    private void OnContentHasFocusChanged(object? sender, HasFocusEventArgs e) => UpdateChrome();

    private void SubscribeNavigation()
    {
        if (_navigationSubscribed)
        {
            return;
        }

        var navigation = Application.Navigation;
        if (navigation is null)
        {
            return;
        }

        navigation.FocusedChanged += OnNavigationFocusedChanged;
        _navigationSubscribed = true;
        UpdateChrome();
    }

    private void UnsubscribeNavigation()
    {
        if (!_navigationSubscribed)
        {
            return;
        }

        var navigation = Application.Navigation;
        if (navigation is not null)
        {
            navigation.FocusedChanged -= OnNavigationFocusedChanged;
        }

        _navigationSubscribed = false;
    }

    private void OnNavigationFocusedChanged(object? sender, EventArgs e) => UpdateChrome();

    private void UpdateChrome()
    {
        var isActive = _content.HasFocus || _content.MostFocused is not null;
        ApplyScheme(isActive ? _focusScheme : _normalScheme);
    }

    private void ApplyScheme(ColorScheme scheme)
    {
        ColorScheme = scheme;
        if (Border is { } border)
        {
            border.ColorScheme = scheme;
        }

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
