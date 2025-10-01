using AzStore.Terminal.Theming;
using AzStore.Terminal.UI.Layout;
using Microsoft.Extensions.Logging;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public abstract class PaneViewBase : FrameView
{
    private readonly IThemeService _theme;
    protected readonly ILogger? Logger;

    protected PaneViewBase(string title, IThemeService theme, ILogger? logger = null)
    {
        Title = title;
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;  // Let chrome handle tab stops
        _theme = theme;
        Logger = logger;

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

    protected override bool OnKeyDown(Key keyEvent)
    {
        Logger?.LogDebug("PaneViewBase '{Title}' OnKeyDown: Key={Key}", Title, keyEvent);

        if (LayoutRootView.IsTraversalKey(keyEvent))
        {
            Logger?.LogDebug("PaneViewBase '{Title}': Detected traversal key", Title);
            if (HandleFocusTraversal(keyEvent))
            {
                Logger?.LogDebug("PaneViewBase '{Title}': Traversal handled", Title);
                return true;
            }
            Logger?.LogDebug("PaneViewBase '{Title}': Traversal not handled", Title);
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
