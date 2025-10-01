using AzStore.Terminal.Theming;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public class CommandHistoryPaneView : PaneViewBase
{
    private readonly Label _placeholder;

    public CommandHistoryPaneView(IThemeService theme) : base("History", theme)
    {
        _placeholder = CreatePlaceholder("No commands run yet");
        Add(_placeholder);
    }

    public Label Placeholder => _placeholder;
}
