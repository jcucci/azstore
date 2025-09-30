using AzStore.Terminal.Theming;
using AzStore.Terminal.UI.Layout;
using Microsoft.Extensions.Logging;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public abstract class PaneViewBase : FrameView
{
    private readonly IThemeService _theme;
    private static ILogger? _logger;

    protected PaneViewBase(string title, IThemeService theme)
    {
        Title = title;
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;  // Let chrome handle tab stops
        _theme = theme;

        ShadowStyle = ShadowStyle.None;

        var border = Border;
        if (border is not null)
        {
            border.Visible = false;
            border.Thickness = new Thickness(0);
        }
    }

    protected Label CreatePlaceholder(string text)
    {
        var label = new Label
        {
            Text = text,
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = Dim.Fill(1),
            TextAlignment = Alignment.Center
        };

        label.ColorScheme = _theme.GetLabelColorScheme(ThemeToken.Title);
        return label;
    }

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    protected override bool OnKeyDown(Key keyEvent)
    {
        _logger?.LogDebug("PaneViewBase '{Title}' OnKeyDown: Key={Key}", Title, keyEvent);

        if (LayoutRootView.IsTraversalKey(keyEvent))
        {
            _logger?.LogDebug("PaneViewBase '{Title}': Detected traversal key", Title);
            if (HandleFocusTraversal(keyEvent))
            {
                _logger?.LogDebug("PaneViewBase '{Title}': Traversal handled", Title);
                return true;
            }
            _logger?.LogDebug("PaneViewBase '{Title}': Traversal not handled", Title);
        }

        return base.OnKeyDown(keyEvent);
    }

    private bool HandleFocusTraversal(Key keyEvent)
    {
        var layout = FindLayoutRoot();
        return layout != null && layout.HandleFocusTraversal(keyEvent);
    }

    private LayoutRootView? FindLayoutRoot()
    {
        View? current = this;

        while (current != null)
        {
            if (current is LayoutRootView layout)
            {
                return layout;
            }

            current = current.SuperView;
        }

        return null;
    }
}
