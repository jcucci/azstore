using AzStore.Terminal.Theming;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public class StorageAccountPaneView : PaneViewBase
{
    private readonly Label _placeholder;

    public StorageAccountPaneView(IThemeService theme) : base("Storage Account", theme)
    {
        _placeholder = CreatePlaceholder("No account selected");
        Add(_placeholder);
    }

    public Label Placeholder => _placeholder;
}
