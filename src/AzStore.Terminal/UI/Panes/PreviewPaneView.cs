using AzStore.Terminal.Theming;
using Terminal.Gui;

namespace AzStore.Terminal.UI.Panes;

public class PreviewPaneView : PaneViewBase
{
    private readonly Label _placeholder;

    public PreviewPaneView(IThemeService theme) : base("Preview", theme)
    {
        _placeholder = CreatePlaceholder("Select an item to preview");
        Add(_placeholder);
    }

    public Label Placeholder => _placeholder;
}
