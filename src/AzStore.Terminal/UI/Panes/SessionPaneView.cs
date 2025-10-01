using AzStore.Terminal.Theming;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public class SessionPaneView : PaneViewBase
{
    private readonly Label _placeholder;

    public SessionPaneView(IThemeService theme) : base("Sessions", theme)
    {
        _placeholder = CreatePlaceholder("No sessions loaded");
        Add(_placeholder);
    }

    public Label Placeholder => _placeholder;
}
