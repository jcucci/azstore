using AzStore.Terminal.Theming;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public class CommandEditorPaneView : PaneViewBase
{
    private readonly Label _placeholder;

    public CommandEditorPaneView(IThemeService theme) : base("Command", theme)
    {
        _placeholder = CreatePlaceholder(":command editor placeholder");
        Add(_placeholder);
    }

    public Label Placeholder => _placeholder;
}
