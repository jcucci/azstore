using AzStore.Terminal.Theming;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public class ContainerPaneView : PaneViewBase
{
    private readonly Label _placeholder;

    public ContainerPaneView(IThemeService theme) : base("Container", theme)
    {
        _placeholder = CreatePlaceholder("No container selected");
        Add(_placeholder);
    }

    public Label Placeholder => _placeholder;
}
